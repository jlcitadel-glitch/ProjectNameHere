using UnityEngine;
using UnityEngine.UI;

namespace ProjectName.UI
{
    /// <summary>
    /// Visual connection line between skill nodes in the tree.
    /// Uses UI.Image with a line texture or generates a procedural line.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class SkillConnectionLine : MonoBehaviour
    {
        [Header("Style")]
        [SerializeField] private float lineWidth = 4f;
        [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private Color availableColor = new Color(0.6f, 0.5f, 0.2f, 0.8f);
        [SerializeField] private Color connectedColor = new Color(0.4f, 0.6f, 0.9f, 1f);

        [Header("Animation")]
        [SerializeField] private bool animateOnConnect = true;
        [SerializeField] private float animationDuration = 0.3f;

        // Runtime
        private Image lineImage;
        private RectTransform rectTransform;
        private SkillNodeUI fromNode;
        private SkillNodeUI toNode;
        private bool isConnected;

        private void Awake()
        {
            lineImage = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// Initializes the connection between two nodes.
        /// </summary>
        public void Initialize(SkillNodeUI from, SkillNodeUI to)
        {
            fromNode = from;
            toNode = to;

            UpdateLinePosition();
            RefreshState();
        }

        /// <summary>
        /// Updates the line position based on node positions.
        /// </summary>
        public void UpdateLinePosition()
        {
            if (fromNode == null || toNode == null) return;

            Vector2 fromPos = fromNode.GetCenterPosition();
            Vector2 toPos = toNode.GetCenterPosition();

            // Calculate line properties
            Vector2 direction = toPos - fromPos;
            float distance = direction.magnitude;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Position at midpoint
            rectTransform.anchoredPosition = (fromPos + toPos) / 2f;

            // Set size (width = distance, height = line width)
            rectTransform.sizeDelta = new Vector2(distance, lineWidth);

            // Set rotation
            rectTransform.rotation = Quaternion.Euler(0, 0, angle);
        }

        /// <summary>
        /// Refreshes the line's visual state based on connected nodes.
        /// </summary>
        public void RefreshState()
        {
            if (fromNode == null || toNode == null) return;

            bool fromLearned = fromNode.SkillInstance != null;
            bool toLearned = toNode.SkillInstance != null;

            Color targetColor;

            if (toLearned)
            {
                targetColor = connectedColor;
                if (!isConnected && animateOnConnect)
                {
                    StartCoroutine(AnimateConnection());
                }
                isConnected = true;
            }
            else if (fromLearned)
            {
                targetColor = availableColor;
                isConnected = false;
            }
            else
            {
                targetColor = lockedColor;
                isConnected = false;
            }

            if (lineImage != null)
            {
                lineImage.color = targetColor;
            }
        }

        private System.Collections.IEnumerator AnimateConnection()
        {
            if (lineImage == null) yield break;

            float elapsed = 0f;
            Color startColor = lineImage.color;

            // Flash effect
            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / animationDuration;

                // Brightness pulse
                float brightness = 1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.5f;
                lineImage.color = connectedColor * brightness;

                yield return null;
            }

            lineImage.color = connectedColor;
        }

        /// <summary>
        /// Static factory method to create a connection line.
        /// </summary>
        public static SkillConnectionLine Create(Transform parent, SkillNodeUI from, SkillNodeUI to)
        {
            var go = new GameObject($"Connection_{from.SkillData?.skillId}_to_{to.SkillData?.skillId}");
            go.transform.SetParent(parent, false);

            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            var image = go.AddComponent<Image>();
            image.raycastTarget = false;

            var line = go.AddComponent<SkillConnectionLine>();
            line.Initialize(from, to);

            return line;
        }
    }
}
