using UnityEngine;
using UnityEngine.UI;

namespace ProjectName.UI
{
    /// <summary>
    /// Enhanced canvas scaler that adapts to different aspect ratios.
    /// Use instead of manual CanvasScaler configuration.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    [ExecuteAlways]
    public class AdaptiveCanvasScaler : MonoBehaviour
    {
        [Header("Reference Resolution")]
        [Tooltip("The resolution the UI is designed for")]
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920, 1080);

        [Header("Scaling Mode")]
        [Tooltip("How to handle different aspect ratios")]
        [SerializeField] private ScalingMode scalingMode = ScalingMode.MatchHeight;

        [Header("Aspect Ratio Handling")]
        [Tooltip("Minimum supported aspect ratio")]
        [SerializeField] private float minAspectRatio = 1.33f; // 4:3

        [Tooltip("Maximum supported aspect ratio")]
        [SerializeField] private float maxAspectRatio = 2.4f; // ~21:9

        [Header("Advanced")]
        [SerializeField] private bool usePhysicalSize = false;
        [SerializeField] private float fallbackDPI = 96f;

        public enum ScalingMode
        {
            MatchHeight,    // Best for platformers - keeps vertical space consistent
            MatchWidth,     // Keeps horizontal space consistent
            Expand,         // Expands UI to fill screen (may show more content)
            Shrink,         // Shrinks UI to fit (may show less content)
            MatchAspect     // Dynamically chooses based on current vs reference aspect
        }

        private CanvasScaler canvasScaler;
        private float lastAspect;

        private void Awake()
        {
            canvasScaler = GetComponent<CanvasScaler>();
            UpdateScaler();
        }

        private void Update()
        {
            float currentAspect = (float)Screen.width / Screen.height;
            if (!Mathf.Approximately(currentAspect, lastAspect))
            {
                UpdateScaler();
            }
        }

        public void UpdateScaler()
        {
            if (canvasScaler == null)
            {
                canvasScaler = GetComponent<CanvasScaler>();
                if (canvasScaler == null) return;
            }

            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = referenceResolution;
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            float currentAspect = (float)Screen.width / Screen.height;
            float referenceAspect = referenceResolution.x / referenceResolution.y;

            lastAspect = currentAspect;

            // Calculate match value based on scaling mode
            float matchValue = scalingMode switch
            {
                ScalingMode.MatchHeight => 1f,
                ScalingMode.MatchWidth => 0f,
                ScalingMode.Expand => currentAspect > referenceAspect ? 1f : 0f,
                ScalingMode.Shrink => currentAspect > referenceAspect ? 0f : 1f,
                ScalingMode.MatchAspect => CalculateMatchValue(currentAspect, referenceAspect),
                _ => 0.5f
            };

            canvasScaler.matchWidthOrHeight = matchValue;

            // Handle physical size if needed
            if (usePhysicalSize)
            {
                canvasScaler.referencePixelsPerUnit = Screen.dpi > 0 ? Screen.dpi : fallbackDPI;
            }
        }

        private float CalculateMatchValue(float currentAspect, float referenceAspect)
        {
            // If current is wider than reference, match height (show more width)
            // If current is taller than reference, match width (show more height)
            if (currentAspect > referenceAspect)
            {
                // Wider screen - match height to keep vertical space
                float widthRatio = currentAspect / maxAspectRatio;
                return Mathf.Clamp01(widthRatio);
            }
            else
            {
                // Taller screen - match width to keep horizontal space
                float heightRatio = minAspectRatio / currentAspect;
                return 1f - Mathf.Clamp01(heightRatio);
            }
        }

        /// <summary>
        /// Gets the effective canvas size accounting for scaling.
        /// </summary>
        public Vector2 GetEffectiveCanvasSize()
        {
            if (canvasScaler == null) return referenceResolution;

            float currentAspect = (float)Screen.width / Screen.height;
            float referenceAspect = referenceResolution.x / referenceResolution.y;
            float match = canvasScaler.matchWidthOrHeight;

            float logWidth = Mathf.Log(Screen.width / referenceResolution.x, 2);
            float logHeight = Mathf.Log(Screen.height / referenceResolution.y, 2);
            float logWeighted = Mathf.Lerp(logWidth, logHeight, match);
            float scaleFactor = Mathf.Pow(2, logWeighted);

            return new Vector2(Screen.width / scaleFactor, Screen.height / scaleFactor);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UpdateScaler();
        }

        private void Reset()
        {
            referenceResolution = new Vector2(1920, 1080);
            scalingMode = ScalingMode.MatchHeight;
            minAspectRatio = 1.33f;
            maxAspectRatio = 2.4f;
        }
#endif
    }
}
