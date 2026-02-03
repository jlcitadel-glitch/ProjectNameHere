using UnityEngine;

namespace ProjectName.UI
{
    /// <summary>
    /// Handles safe area and aspect ratio constraints for UI canvases.
    /// Applies letterboxing/pillarboxing for non-standard aspect ratios.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaHandler : MonoBehaviour
    {
        [Header("Safe Area Settings")]
        [SerializeField] private bool applySafeArea = true;
        [SerializeField] private bool constrainToAspectRatio = false;

        [Header("Aspect Ratio Constraints")]
        [Tooltip("Minimum aspect ratio (width/height). Set to 0 to disable.")]
        [SerializeField] private float minAspectRatio = 1.33f; // 4:3

        [Tooltip("Maximum aspect ratio (width/height). Set to 0 to disable.")]
        [SerializeField] private float maxAspectRatio = 2.4f; // ~21:9

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            ApplyConstraints();
        }

        private void Update()
        {
            // Check if screen size or safe area changed
            if (lastScreenSize.x != Screen.width ||
                lastScreenSize.y != Screen.height ||
                lastSafeArea != Screen.safeArea)
            {
                ApplyConstraints();
            }
        }

        public void ApplyConstraints()
        {
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            lastSafeArea = Screen.safeArea;

            Rect safeArea = Screen.safeArea;

            if (constrainToAspectRatio)
            {
                safeArea = ApplyAspectRatioConstraint(safeArea);
            }

            if (applySafeArea)
            {
                ApplySafeArea(safeArea);
            }
        }

        private Rect ApplyAspectRatioConstraint(Rect area)
        {
            float currentAspect = area.width / area.height;

            // Apply minimum aspect ratio (add letterboxing - black bars top/bottom)
            if (minAspectRatio > 0 && currentAspect < minAspectRatio)
            {
                float targetHeight = area.width / minAspectRatio;
                float heightDiff = area.height - targetHeight;
                area.y += heightDiff * 0.5f;
                area.height = targetHeight;
            }

            // Apply maximum aspect ratio (add pillarboxing - black bars left/right)
            if (maxAspectRatio > 0 && currentAspect > maxAspectRatio)
            {
                float targetWidth = area.height * maxAspectRatio;
                float widthDiff = area.width - targetWidth;
                area.x += widthDiff * 0.5f;
                area.width = targetWidth;
            }

            return area;
        }

        private void ApplySafeArea(Rect safeArea)
        {
            // Convert safe area to anchor values
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            if (showDebugInfo)
            {
                Debug.Log($"[SafeAreaHandler] Applied safe area: {safeArea}, Anchors: {anchorMin} - {anchorMax}");
            }
        }

        /// <summary>
        /// Forces a refresh of the safe area calculations.
        /// </summary>
        public void Refresh()
        {
            lastScreenSize = Vector2Int.zero;
            ApplyConstraints();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying && rectTransform != null)
            {
                ApplyConstraints();
            }
        }
#endif
    }
}
