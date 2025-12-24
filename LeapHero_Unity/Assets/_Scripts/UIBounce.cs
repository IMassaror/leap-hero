using System.Collections;
using UnityEngine;

namespace UISquash
{
    public class UIBounce : MonoBehaviour
    {
        [System.Flags]
        public enum Axis
        {
            None = 0,
            X = 1,
            Y = 2
        }

        [Header("Target")]
        [SerializeField] private RectTransform rectTransform;

        [Header("Defaults")]
        public Axis axis = Axis.Y;
        public float duration = 0.2f;
        public float initialScale = 1f;
        public float maxScale = 1.3f;
        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Coroutine routine;
        private Vector3 originalScale;

        private bool affectX => (axis & Axis.X) != 0;
        private bool affectY => (axis & Axis.Y) != 0;

        private void Awake()
        {
            if (!rectTransform)
                rectTransform = GetComponent<RectTransform>();

            originalScale = rectTransform.localScale;
        }

        // =========================
        // PUBLIC API
        // =========================

        public void Play(
            Axis axis,
            float duration,
            float min,
            float max,
            AnimationCurve curve
        )
        {
            if (axis == Axis.None || duration <= 0f)
                return;

            this.axis = axis;
            this.duration = duration;
            this.initialScale = min;
            this.maxScale = max;
            this.curve = curve;

            Play();
        }

        public void Play()
        {
            if (routine != null)
                StopCoroutine(routine);

            rectTransform.localScale = originalScale;
            routine = StartCoroutine(SquashRoutine());
        }

        // =========================
        // CORE
        // =========================

        private IEnumerator SquashRoutine()
        {
            float t = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(t / duration);

                float curveValue = curve.Evaluate(normalized);
                float remappedValue = Mathf.Lerp(initialScale, maxScale, curveValue);

                // segurança numérica (igual ao script antigo)
                if (Mathf.Abs(remappedValue) < 0.0001f)
                    remappedValue = 0.0001f;

                Vector3 scale = originalScale;

                // === Eixo X ===
                if (affectX)
                    scale.x = originalScale.x * remappedValue;
                else
                    scale.x = originalScale.x / remappedValue;

                // === Eixo Y ===
                if (affectY)
                    scale.y = originalScale.y * remappedValue;
                else
                    scale.y = originalScale.y / remappedValue;

                rectTransform.localScale = scale;
                yield return null;
            }

            rectTransform.localScale = originalScale;
            routine = null;
        }
    }
}
