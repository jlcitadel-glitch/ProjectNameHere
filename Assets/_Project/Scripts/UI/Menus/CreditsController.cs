using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Scrolling credits screen with configurable text and speed.
    /// </summary>
    public class CreditsController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform creditsContent;
        [SerializeField] private TMP_Text creditsText;
        [SerializeField] private Button backButton;

        [Header("Settings")]
        [SerializeField] private float scrollSpeed = 50f;
        [SerializeField] private bool autoScroll = true;

        [Header("Credits Content")]
        [TextArea(10, 30)]
        [SerializeField] private string creditsString =
            "ProjectNameHere\n\n" +
            "--- Design & Programming ---\n\n" +
            "[Your Name]\n\n" +
            "--- Art ---\n\n" +
            "Hero Knight - Pixel Art\n\n" +
            "--- Music ---\n\n" +
            "[Music Credits]\n\n" +
            "--- Tools ---\n\n" +
            "Unity 6\nDOTween\nTextMesh Pro\n\n" +
            "--- Special Thanks ---\n\n" +
            "Thank you for playing!";

        private float scrollPosition;
        private bool isScrolling;

        public event System.Action OnBackPressed;

        private void Awake()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(HandleBack);
            }

            if (creditsText != null)
            {
                creditsText.text = creditsString;
                FontManager.EnsureFont(creditsText);
            }
        }

        private void OnEnable()
        {
            scrollPosition = 0f;
            isScrolling = autoScroll;
            if (creditsContent != null)
            {
                creditsContent.anchoredPosition = Vector2.zero;
            }
        }

        private void Update()
        {
            if (!isScrolling || creditsContent == null)
                return;

            scrollPosition += scrollSpeed * Time.unscaledDeltaTime;
            creditsContent.anchoredPosition = new Vector2(0, scrollPosition);

            // Stop scrolling when content has scrolled past
            float contentHeight = creditsContent.rect.height;
            float viewportHeight = creditsContent.parent != null
                ? ((RectTransform)creditsContent.parent).rect.height
                : 600f;

            if (scrollPosition > contentHeight + viewportHeight)
            {
                isScrolling = false;
            }
        }

        private void HandleBack()
        {
            UIManager.Instance?.PlayCancelSound();
            OnBackPressed?.Invoke();
        }

        private void OnDestroy()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(HandleBack);
            }
        }
    }
}
