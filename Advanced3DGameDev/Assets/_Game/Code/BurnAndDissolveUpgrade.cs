using UnityEngine;

namespace Semester2
{
    [RequireComponent(typeof(Renderer))]
    public class BurnAndDissolveA2 : MonoBehaviour
    {
        [Header("Animation Settings")]
        public float duration = 3f;
        public AnimationCurve dissolveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public float startDelay = 0.5f;

        private Renderer _meshRenderer;
        private MaterialPropertyBlock _propBlock;
        private float _currentTime;
        private bool _isAnimating = false;

        private static readonly int DissolvePropID = Shader.PropertyToID("_Dissolution_Amount");

        private void Awake()
        {
            _meshRenderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            // Trigger and Reset support
            if (Input.GetKeyDown(KeyCode.Space)) StartDissolve();
            if (Input.GetKeyDown(KeyCode.R)) ResetDissolve();

            if (_isAnimating)
            {
                Animate();
            }
        }

        public void StartDissolve()
        {
            _currentTime = -startDelay;
            _isAnimating = true;
        }

        public void ResetDissolve()
        {
            _isAnimating = false;
            _currentTime = 0;
            UpdateShader(0f);
        }

        private void Animate()
        {
            _currentTime += Time.deltaTime;
            
            if (_currentTime < 0) return; // Wait for delay

            float t = Mathf.Clamp01(_currentTime / duration);
            float value = dissolveCurve.Evaluate(t);

            UpdateShader(value);

            if (t >= 1.0f) _isAnimating = false; // Stop at fully dissolved
        }

        private void UpdateShader(float value)
        {
            // MaterialPropertyBlock for efficiency
            _meshRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(DissolvePropID, value);
            _meshRenderer.SetPropertyBlock(_propBlock);
        }
    }
}