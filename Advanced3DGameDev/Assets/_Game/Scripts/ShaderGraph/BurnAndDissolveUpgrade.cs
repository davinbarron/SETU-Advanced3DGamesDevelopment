using UnityEngine;
using Fusion;
using Example;

namespace Semester2
{
    /// <summary>
    /// Universal networked controller for "Dissolve" shader effects.
    /// Handles visual animation, lighting, and gameplay logic for Orbs and Barrels.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class BurnAndDissolveA2 : NetworkBehaviour
    {
        [Header("Visual Configuration")]
        public float duration = 1f;
        public AnimationCurve dissolveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public float startDelay = 0f;

        [Header("Detection (Independent Mode)")]
        [Tooltip("If true, this script manages its own lifecycle (e.g. Barrels). If false, external scripts trigger it.")]
        public bool independentMode = true;
        public bool onlyDuringMatch = true;
        [SerializeField] private float triggerRadius = 1.0f;
        public float autoResetDelay = 5f;

        [Header("Lighting")]
        public Light motivatedLight;
        public float maxLightIntensity = 5f;
        public float minIdleIntensity = 1.0f;
        public AnimationCurve lightIntensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 0f);

        [Networked] private bool _isDissolving { get; set; }
        [Networked] private float _networkedStartTime { get; set; }
        [Networked] private PlayerRef _triggeringPlayer { get; set; }
        [Networked] private TickTimer _scoreDrainTimer { get; set; }
        [Networked] private int _totalScoreDeducted { get; set; }

        private bool _wantsToDissolve;
        private PlayerRef _pendingPlayer;
        private Renderer _meshRenderer;
        private MaterialPropertyBlock _propBlock;
        private Collider _collider;
        private ExplodeBarrel _legacyExplosion;
        private Light[] _allLights;

        private static readonly int DissolvePropID = Shader.PropertyToID("_Dissolution_Amount");

        #region Lifecycle

        public override void Spawned()
        {
            InitializeComponents();
            RefreshVisualState();
        }

        private void InitializeComponents()
        {
            _meshRenderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
            _legacyExplosion = GetComponent<ExplodeBarrel>();
            _collider = GetComponent<Collider>();
            _allLights = GetComponentsInChildren<Light>(true);
            
            if (motivatedLight == null && _allLights.Length > 0)
            {
                motivatedLight = _allLights[0];
            }

            if (_collider is SphereCollider sphere) triggerRadius = sphere.radius;
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority)
            {
                // Non-authoritative clients still check for players to initiate authority requests
                if (independentMode && !_isDissolving) CheckForPlayers();
                return;
            }

            HandleAuthorityPending();

            if (!_isDissolving)
            {
                if (independentMode) CheckForPlayers();
            }
            else
            {
                ProcessScoreDrain();
                HandleAutoReset();
            }
        }

        public override void Render()
        {
            EnsureLightReference();

            float elapsed = _isDissolving ? (Runner.SimulationTime - _networkedStartTime) : -1f;

            if (_isDissolving && elapsed >= 0)
            {
                ApplyAnimationState(elapsed);
            }
            else
            {
                // Idle or Start Delay: Use base stationary glow
                float idleIntensity = Mathf.Max(minIdleIntensity, lightIntensityCurve.Evaluate(0) * maxLightIntensity);
                SetVisuals(0f, true, idleIntensity); 
            }
        }

        #endregion

        #region Gameplay Logic

        private void HandleAuthorityPending()
        {
            if (_wantsToDissolve)
            {
                StartDissolve(_pendingPlayer);
                _wantsToDissolve = false;
                _pendingPlayer = PlayerRef.None;
            }
        }

        private void CheckForPlayers()
        {
            if (onlyDuringMatch && !IsMatchPlaying()) return;

            float worldRadius = triggerRadius * GetMaxScale();
            // Using standard Physics.OverlapSphere for simplicity and ease of use
            Collider[] hits = Physics.OverlapSphere(transform.position, worldRadius);

            foreach (var hit in hits)
            {
                if (NetworkUtils.TryGetPlayer(hit, out Player p))
                {
                    RequestDissolve(p.Object.InputAuthority);
                    break;
                }
            }
        }

        private void RequestDissolve(PlayerRef player)
        {
            if (HasStateAuthority) 
            {
                StartDissolve(player);
            }
            else
            {
                Object.RequestStateAuthority();
                _wantsToDissolve = true;
                _pendingPlayer = player;
            }
        }

        private void ProcessScoreDrain()
        {
            if (_triggeringPlayer == PlayerRef.None || _totalScoreDeducted >= 10) return;

            if (_scoreDrainTimer.ExpiredOrNotRunning(Runner))
            {
                _scoreDrainTimer = TickTimer.CreateFromSeconds(Runner, 1.0f);
                _totalScoreDeducted++;
                Rpc_DeductScore(_triggeringPlayer);
            }
        }

        private void HandleAutoReset()
        {
            if (!independentMode) return;

            float elapsed = Runner.SimulationTime - _networkedStartTime;
            if (elapsed > duration + autoResetDelay)
            {
                ResetDissolve();
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_DeductScore(PlayerRef targetPlayer)
        {
            var playerObj = NetworkUtils.GetPlayerObject(Runner, targetPlayer);
            if (playerObj != null && playerObj.TryGetComponent(out Player p))
            {
                p.AddScore(-1);
            }
        }

        #endregion

        #region Public API

        public void StartDissolve(PlayerRef triggeringPlayer = default)
        {
            if (Object == null || !Object.HasStateAuthority || _isDissolving) return;

            _isDissolving = true;
            _networkedStartTime = Runner.SimulationTime + startDelay;
            _triggeringPlayer = triggeringPlayer;
            _totalScoreDeducted = 0;
            _scoreDrainTimer = TickTimer.CreateFromSeconds(Runner, 1.0f);

            if (_legacyExplosion != null) Rpc_Explode();
        }

        public void ResetDissolve()
        {
            if (Object == null || !Object.HasStateAuthority) return;

            _isDissolving = false;
            _triggeringPlayer = PlayerRef.None;
            _totalScoreDeducted = 0;
            _scoreDrainTimer = default;
            
            RefreshVisualState();
        }

        #endregion

        #region Visuals & Animation

        private void ApplyAnimationState(float elapsed)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float dissolveVal = dissolveCurve.Evaluate(t);
            float curveLight = lightIntensityCurve.Evaluate(t) * maxLightIntensity;

            bool isFinished = t >= 1f;
            float finalLight = isFinished ? 0f : Mathf.Max(minIdleIntensity * (1f - t), curveLight);
            
            SetVisuals(dissolveVal, !isFinished, finalLight);
        }

        private void SetVisuals(float dissolveAmount, bool isVisible, float lightVal)
        {
            UpdateShader(dissolveAmount, lightVal);
            
            if (_meshRenderer != null) _meshRenderer.enabled = isVisible;
            if (_collider != null) _collider.enabled = isVisible;
            
            UpdateLights(isVisible, lightVal);
        }

        private void UpdateLights(bool isVisible, float lightIntensity)
        {
            if (motivatedLight != null)
            {
                motivatedLight.intensity = lightIntensity;
                motivatedLight.enabled = isVisible || (lightIntensity > 0.001f);
            }

            if (_allLights == null) return;

            foreach (var l in _allLights)
            {
                if (l == null || l == motivatedLight) continue;
                l.enabled = isVisible;
            }
        }

        private void UpdateShader(float dissolveValue, float lightIntensity)
        {
            if (_meshRenderer == null) return;
            _meshRenderer.GetPropertyBlock(_propBlock);
            
            _propBlock.SetFloat(DissolvePropID, dissolveValue);
            _propBlock.SetFloat("_EmissionIntensity", lightIntensity);
            
            _meshRenderer.SetPropertyBlock(_propBlock);
        }

        private void RefreshVisualState()
        {
            if (_isDissolving)
            {
                SetVisuals(1f, false, 0f);
            }
            else
            {
                float idleIntensity = Mathf.Max(minIdleIntensity, lightIntensityCurve.Evaluate(0) * maxLightIntensity);
                SetVisuals(0f, true, idleIntensity);
            }
        }

        private void EnsureLightReference()
        {
            if (motivatedLight == null && _allLights != null && _allLights.Length > 0)
            {
                motivatedLight = _allLights[0];
            }
        }

        #endregion

        #region Helpers

        private bool IsMatchPlaying() => GameStateManager.Instance != null && GameStateManager.Instance.Phase == GamePhase.Playing;
        
        private float GetMaxScale() => Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_Explode() { if (_legacyExplosion != null) _legacyExplosion.Explode(); }

        #endregion
    }
}