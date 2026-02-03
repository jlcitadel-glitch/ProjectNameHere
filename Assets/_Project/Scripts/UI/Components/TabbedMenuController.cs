using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectName.UI
{
    /// <summary>
    /// Hollow Knight-style tabbed menu controller.
    /// Supports bumper/shoulder button navigation (LB/RB) for tab switching.
    /// </summary>
    public class TabbedMenuController : MonoBehaviour
    {
        [Header("Tabs")]
        [Tooltip("Tab button components in order")]
        [SerializeField] private TabButton[] tabs;

        [Tooltip("Content panels corresponding to each tab")]
        [SerializeField] private GameObject[] tabContents;

        [Tooltip("Default tab to show on open")]
        [SerializeField] private int defaultTabIndex = 0;

        [Header("Navigation Input")]
        [SerializeField] private InputActionReference tabLeftAction;
        [SerializeField] private InputActionReference tabRightAction;

        [Header("Animation")]
        [SerializeField] private float transitionDuration = 0.2f;

        [Header("Visual Settings")]
        [Tooltip("Underline or indicator that shows current tab")]
        [SerializeField] private RectTransform tabIndicator;

        [Tooltip("How fast the indicator moves between tabs")]
        [SerializeField] private float indicatorSpeed = 0.15f;

        private int currentTabIndex;
        private bool isTransitioning;

        public int CurrentTabIndex => currentTabIndex;
        public event Action<int> OnTabChanged;

        private void OnEnable()
        {
            if (tabLeftAction != null)
            {
                tabLeftAction.action.Enable();
                tabLeftAction.action.performed += OnTabLeft;
            }

            if (tabRightAction != null)
            {
                tabRightAction.action.Enable();
                tabRightAction.action.performed += OnTabRight;
            }

            // Initialize to default tab
            SwitchTab(defaultTabIndex, immediate: true);
        }

        private void OnDisable()
        {
            if (tabLeftAction != null)
            {
                tabLeftAction.action.performed -= OnTabLeft;
            }

            if (tabRightAction != null)
            {
                tabRightAction.action.performed -= OnTabRight;
            }
        }

        private void OnTabLeft(InputAction.CallbackContext context)
        {
            PreviousTab();
        }

        private void OnTabRight(InputAction.CallbackContext context)
        {
            NextTab();
        }

        /// <summary>
        /// Switches to the next tab (wraps around).
        /// </summary>
        public void NextTab()
        {
            if (tabs == null || tabs.Length == 0)
                return;

            int nextIndex = (currentTabIndex + 1) % tabs.Length;
            SwitchTab(nextIndex);
        }

        /// <summary>
        /// Switches to the previous tab (wraps around).
        /// </summary>
        public void PreviousTab()
        {
            if (tabs == null || tabs.Length == 0)
                return;

            int prevIndex = currentTabIndex - 1;
            if (prevIndex < 0)
                prevIndex = tabs.Length - 1;

            SwitchTab(prevIndex);
        }

        /// <summary>
        /// Switches to a specific tab by index.
        /// </summary>
        public void SwitchTab(int index, bool immediate = false)
        {
            if (tabs == null || tabs.Length == 0)
                return;

            if (index < 0 || index >= tabs.Length)
                return;

            if (index == currentTabIndex && !immediate)
                return;

            if (isTransitioning)
                return;

            // Deactivate current tab
            if (tabs[currentTabIndex] != null)
            {
                tabs[currentTabIndex].SetSelected(false);
            }

            if (tabContents != null && currentTabIndex < tabContents.Length && tabContents[currentTabIndex] != null)
            {
                if (immediate)
                {
                    tabContents[currentTabIndex].SetActive(false);
                }
                else
                {
                    StartCoroutine(FadeOutContent(tabContents[currentTabIndex]));
                }
            }

            // Update index
            int previousIndex = currentTabIndex;
            currentTabIndex = index;

            // Activate new tab
            if (tabs[currentTabIndex] != null)
            {
                tabs[currentTabIndex].SetSelected(true);
            }

            if (tabContents != null && currentTabIndex < tabContents.Length && tabContents[currentTabIndex] != null)
            {
                if (immediate)
                {
                    tabContents[currentTabIndex].SetActive(true);
                }
                else
                {
                    StartCoroutine(FadeInContent(tabContents[currentTabIndex]));
                }
            }

            // Move indicator
            if (tabIndicator != null && tabs[currentTabIndex] != null)
            {
                if (immediate)
                    MoveIndicatorImmediate(tabs[currentTabIndex].GetComponent<RectTransform>());
                else
                    StartCoroutine(MoveIndicatorAnimated(tabs[currentTabIndex].GetComponent<RectTransform>()));
            }

            // Play sound
            if (!immediate)
            {
                UIManager.Instance?.PlayTabSwitchSound();
            }

            OnTabChanged?.Invoke(currentTabIndex);
        }

        private IEnumerator FadeOutContent(GameObject content)
        {
            CanvasGroup group = content.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = content.AddComponent<CanvasGroup>();
            }

            float duration = transitionDuration / 2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                group.alpha = 1f - EaseOutQuad(t);
                yield return null;
            }

            group.alpha = 0f;
            content.SetActive(false);
        }

        private IEnumerator FadeInContent(GameObject content)
        {
            content.SetActive(true);

            CanvasGroup group = content.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = content.AddComponent<CanvasGroup>();
            }

            group.alpha = 0f;
            float duration = transitionDuration / 2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                group.alpha = EaseOutQuad(t);
                yield return null;
            }

            group.alpha = 1f;
        }

        private void MoveIndicatorImmediate(RectTransform targetTab)
        {
            if (tabIndicator == null || targetTab == null)
                return;

            Vector3 targetPosition = targetTab.position;
            targetPosition.y = tabIndicator.position.y;
            tabIndicator.position = targetPosition;

            Vector2 size = tabIndicator.sizeDelta;
            size.x = targetTab.sizeDelta.x;
            tabIndicator.sizeDelta = size;
        }

        private IEnumerator MoveIndicatorAnimated(RectTransform targetTab)
        {
            if (tabIndicator == null || targetTab == null)
                yield break;

            Vector3 startPosition = tabIndicator.position;
            Vector3 targetPosition = targetTab.position;
            targetPosition.y = startPosition.y;

            Vector2 startSize = tabIndicator.sizeDelta;
            Vector2 targetSize = new Vector2(targetTab.sizeDelta.x, startSize.y);

            float elapsed = 0f;

            while (elapsed < indicatorSpeed)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / indicatorSpeed);
                float eased = EaseOutQuad(t);

                tabIndicator.position = Vector3.Lerp(startPosition, targetPosition, eased);
                tabIndicator.sizeDelta = Vector2.Lerp(startSize, targetSize, eased);

                yield return null;
            }

            tabIndicator.position = targetPosition;
            tabIndicator.sizeDelta = targetSize;
        }

        /// <summary>
        /// Sets up tab click handlers for mouse/touch input.
        /// </summary>
        public void InitializeTabButtons()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                int tabIndex = i; // Capture for closure
                if (tabs[i] != null)
                {
                    tabs[i].OnTabClicked += () => SwitchTab(tabIndex);
                }
            }
        }

        private float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure arrays are same length
            if (tabs != null && tabContents != null && tabs.Length != tabContents.Length)
            {
                Debug.LogWarning("TabbedMenuController: tabs and tabContents arrays should have the same length!");
            }
        }
#endif
    }

    /// <summary>
    /// Individual tab button component.
    /// </summary>
    public class TabButton : MonoBehaviour
    {
        [Header("Visual States")]
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private TMPro.TMP_Text label;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color selectedColor = new Color(0.545f, 0f, 0f, 1f);
        [SerializeField] private Color normalTextColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        [SerializeField] private Color selectedTextColor = new Color(0.961f, 0.961f, 0.863f, 1f);

        [Header("Animation")]
        [SerializeField] private float transitionDuration = 0.15f;

        private bool isSelected;
        private Button button;
        private Coroutine colorTransition;

        public event Action OnTabClicked;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnTabClicked?.Invoke());
            }
        }

        /// <summary>
        /// Sets the selected state of this tab.
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (colorTransition != null)
                StopCoroutine(colorTransition);

            colorTransition = StartCoroutine(AnimateColors(selected));
        }

        private IEnumerator AnimateColors(bool selected)
        {
            Color targetBgColor = selected ? selectedColor : normalColor;
            Color targetTextColor = selected ? selectedTextColor : normalTextColor;

            Color startBgColor = background != null ? background.color : normalColor;
            Color startTextColor = label != null ? label.color : normalTextColor;
            Color startIconColor = icon != null ? icon.color : normalTextColor;

            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);

                if (background != null)
                    background.color = Color.Lerp(startBgColor, targetBgColor, t);

                if (label != null)
                    label.color = Color.Lerp(startTextColor, targetTextColor, t);

                if (icon != null)
                    icon.color = Color.Lerp(startIconColor, targetTextColor, t);

                yield return null;
            }

            if (background != null)
                background.color = targetBgColor;

            if (label != null)
                label.color = targetTextColor;

            if (icon != null)
                icon.color = targetTextColor;
        }
    }
}
