using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DG.Tweening;

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
        [SerializeField] private Ease tabEase = Ease.OutQuad;

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
                    FadeOutContent(tabContents[currentTabIndex]);
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
                    FadeInContent(tabContents[currentTabIndex]);
                }
            }

            // Move indicator
            if (tabIndicator != null && tabs[currentTabIndex] != null)
            {
                MoveIndicator(tabs[currentTabIndex].GetComponent<RectTransform>(), immediate);
            }

            // Play sound
            if (!immediate)
            {
                UIManager.Instance?.PlayTabSwitchSound();
            }

            OnTabChanged?.Invoke(currentTabIndex);
        }

        private void FadeOutContent(GameObject content)
        {
            CanvasGroup group = content.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = content.AddComponent<CanvasGroup>();
            }

            group.DOFade(0f, transitionDuration / 2f)
                .SetEase(tabEase)
                .OnComplete(() => content.SetActive(false))
                .SetUpdate(true);
        }

        private void FadeInContent(GameObject content)
        {
            content.SetActive(true);

            CanvasGroup group = content.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = content.AddComponent<CanvasGroup>();
            }

            group.alpha = 0f;
            group.DOFade(1f, transitionDuration / 2f)
                .SetEase(tabEase)
                .SetUpdate(true);
        }

        private void MoveIndicator(RectTransform targetTab, bool immediate)
        {
            if (tabIndicator == null || targetTab == null)
                return;

            Vector3 targetPosition = targetTab.position;
            targetPosition.y = tabIndicator.position.y; // Keep same Y

            if (immediate)
            {
                tabIndicator.position = targetPosition;

                // Match width to tab
                Vector2 size = tabIndicator.sizeDelta;
                size.x = targetTab.sizeDelta.x;
                tabIndicator.sizeDelta = size;
            }
            else
            {
                tabIndicator.DOMove(targetPosition, indicatorSpeed)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true);

                tabIndicator.DOSizeDelta(
                    new Vector2(targetTab.sizeDelta.x, tabIndicator.sizeDelta.y),
                    indicatorSpeed
                ).SetEase(Ease.OutQuad).SetUpdate(true);
            }
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

            Color targetBgColor = selected ? selectedColor : normalColor;
            Color targetTextColor = selected ? selectedTextColor : normalTextColor;

            if (background != null)
            {
                background.DOColor(targetBgColor, transitionDuration).SetUpdate(true);
            }

            if (label != null)
            {
                label.DOColor(targetTextColor, transitionDuration).SetUpdate(true);
            }

            if (icon != null)
            {
                icon.DOColor(targetTextColor, transitionDuration).SetUpdate(true);
            }
        }
    }
}
