using UnityEngine;
using Fusion;
using Example;

namespace Semester2
{
    /// <summary>
    /// Universal controller for networked "Dissolve" shader effects.
    /// Decouples visual animation from game logic.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class BurnAndDissolveA2 : NetworkBehaviour
    {
        [Header("Visual Configuration")]
        public float duration = 1f;
        public AnimationCurve dissolveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public float startDelay = 0f;

        [Header("Detection (Independent Mode Only)")]
        [Tooltip("If true, this script manages its own lifecycle. If false, an external script (like ScoreOrb) must trigger it.")]
        public bool independentMode = true;
        public bool onlyDuringMatch = true;
        [SerializeField] private float triggerRadius = 1.0f;

        [Header("Lighting")]
        public Light motivatedLight;
        public float maxLightIntensity = 5f;
        public AnimationCurve lightIntensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 0f);

        [Networked] private bool _isDissolving { get; set; }
        [Networked] private float _networkedStartTime { get; set; }

        private bool _wantsToDissolve; // Local buffer for Shared Mode authority handover
        private Renderer _meshRenderer;
        private MaterialPropertyBlock _propBlock;
        private Collider _collider;
        private ExplodeBarrel _legacyExplosion;

        private static readonly int DissolvePropID = Shader.PropertyToID("_Dissolution_Amount");

        public override void Spawned()
        {
            _meshRenderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
            _legacyExplosion = GetComponent<ExplodeBarrel>();
            _collider = GetComponent<Collider>();

            // Auto-sync radius if using a SphereCollider
            if (_collider is SphereCollider sphere) triggerRadius = sphere.radius;

            RefreshVisualState();
        }

        public override void FixedUpdateNetwork()
        {
            HandleAuthorityRequests();

            if (Object.HasStateAuthority && independentMode && !_isDissolving)
            {
                CheckForPlayers();
            }
        }

        public override void Render()
        {
            if (_isDissolving)
            {
                ApplyAnimationState();
            }
            else
            {
                SetVisuals(0f, true, 0f); // Idle state
            }
        }

        #region Logic & Authority

        private void HandleAuthorityRequests()
        {
            if (_wantsToDissolve && Object.HasStateAuthority)
            {
                StartDissolve();
                _wantsToDissolve = false;
            }
        }

        private void CheckForPlayers()
        {
            if (onlyDuringMatch && !IsMatchPlaying()) return;

            float worldRadius = triggerRadius * GetMaxScale();
            var hits = Physics.OverlapSphere(transform.position, worldRadius);

            foreach (var hit in hits)
            {
                if (NetworkUtils.TryGetPlayer(hit, out _))
                {
                    RequestDissolve();
                    break;
                }
            }
        }

        private void RequestDissolve()
        {
            if (HasStateAuthority) StartDissolve();
            else
            {
                Object.RequestStateAuthority();
                _wantsToDissolve = true;
            }
        }

        public void StartDissolve()
        {
            if (Object == null || !Object.HasStateAuthority || _isDissolving) return;

            _isDissolving = true;
            _networkedStartTime = Runner.SimulationTime + startDelay;

            if (_legacyExplosion != null) Rpc_Explode();
        }

        public void ResetDissolve()
        {
            if (Object == null || !Object.HasStateAuthority) return;

            _isDissolving = false;
            _networkedStartTime = 0;
            RefreshVisualState();
        }

        #endregion

        #region Visuals & Animation

        private void ApplyAnimationState()
        {
            float elapsed = Runner.SimulationTime - _networkedStartTime;
            if (elapsed < 0) return;

            float t = Mathf.Clamp01(elapsed / duration);
            float dissolveVal = dissolveCurve.Evaluate(t);
            float lightVal = lightIntensityCurve.Evaluate(t) * maxLightIntensity;

            bool isFinished = t >= 1f;
            SetVisuals(dissolveVal, !isFinished, isFinished ? 0f : lightVal);
        }

        private void SetVisuals(float dissolveAmount, bool showObject, bool lightIntensity)
        {
            // Note: Overload for simple true/false state
        }

        private void SetVisuals(float dissolveAmount, bool physicsEnabled, float lightVal)
        {
            UpdateShader(dissolveAmount);
            
            _meshRenderer.enabled = physicsEnabled; // Hide renderer when "gone"
            if (_collider != null) _collider.enabled = physicsEnabled;
            if (motivatedLight != null) motivatedLight.intensity = lightVal;
        }

        private void RefreshVisualState()
        {
            if (_isDissolving) SetVisuals(1f, false, 0f);
            else SetVisuals(0f, true, 0f);
        }

        private void UpdateShader(float value)
        {
            if (_meshRenderer == null) return;
            _meshRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(DissolvePropID, value);
            _meshRenderer.SetPropertyBlock(_propBlock);
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