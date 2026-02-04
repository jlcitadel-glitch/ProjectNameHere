using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Controls the options menu with tabbed navigation.
    /// Handles Display, Audio, and Controls settings.
    /// </summary>
    public class OptionsMenuController : MonoBehaviour
    {
        [Header("Tab Buttons")]
        [SerializeField] private Button displayTabButton;
        [SerializeField] private Button audioTabButton;
        [SerializeField] private Button controlsTabButton;

        [Header("Tab Panels")]
        [SerializeField] private GameObject displayPanel;
        [SerializeField] private GameObject audioPanel;
        [SerializeField] private GameObject controlsPanel;

        [Header("Tab Indicator")]
        [SerializeField] private RectTransform tabIndicator;
        [SerializeField] private float indicatorMoveSpeed = 0.15f;

        [Header("Tab Navigation Input")]
        [SerializeField] private InputActionReference tabLeftAction;
        [SerializeField] private InputActionReference tabRightAction;

        [Header("Display Settings")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown windowModeDropdown;
        [SerializeField] private TMP_Dropdown aspectRatioDropdown;
        [SerializeField] private Button applyDisplayButton;

        [Header("Audio Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TMP_Text masterVolumeText;
        [SerializeField] private TMP_Text musicVolumeText;
        [SerializeField] private TMP_Text sfxVolumeText;

        [Header("Tab Colors")]
        [SerializeField] private Color normalTabColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color selectedTabColor = new Color(0.545f, 0f, 0f, 1f);
        [SerializeField] private Color normalTextColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        [SerializeField] private Color selectedTextColor = new Color(0.961f, 0.961f, 0.863f, 1f);

        private int currentTabIndex = 0;
        private Button[] tabButtons;
        private GameObject[] tabPanels;

        // Cached settings for revert
        private int cachedResolutionIndex;
        private int cachedWindowModeIndex;
        private int cachedAspectRatioIndex;

        private void Awake()
        {
            AutoFindReferences();

            tabButtons = new Button[] { displayTabButton, audioTabButton, controlsTabButton };
            tabPanels = new GameObject[] { displayPanel, audioPanel, controlsPanel };

            SetupTabButtons();
            SetupDisplaySettings();
            SetupAudioSettings();
        }

        private void AutoFindReferences()
        {
            // Find TabBar and its buttons
            var tabBar = transform.Find("TabBar");
            if (tabBar != null)
            {
                if (displayTabButton == null)
                {
                    var found = tabBar.Find("DisplayTab");
                    if (found != null)
                    {
                        displayTabButton = found.GetComponent<Button>();
                        Debug.Log("[OptionsMenu] Auto-found DisplayTab");
                    }
                }

                if (audioTabButton == null)
                {
                    var found = tabBar.Find("AudioTab");
                    if (found != null)
                    {
                        audioTabButton = found.GetComponent<Button>();
                        Debug.Log("[OptionsMenu] Auto-found AudioTab");
                    }
                }

                if (controlsTabButton == null)
                {
                    var found = tabBar.Find("ControlsTab");
                    if (found != null)
                    {
                        controlsTabButton = found.GetComponent<Button>();
                        Debug.Log("[OptionsMenu] Auto-found ControlsTab");
                    }
                }
            }

            // Find ContentArea and its panels
            var contentArea = transform.Find("ContentArea");
            if (contentArea != null)
            {
                if (displayPanel == null)
                {
                    var found = contentArea.Find("DisplayPanel");
                    if (found != null)
                    {
                        displayPanel = found.gameObject;
                        Debug.Log("[OptionsMenu] Auto-found DisplayPanel");
                    }
                }

                if (audioPanel == null)
                {
                    var found = contentArea.Find("AudioPanel");
                    if (found != null)
                    {
                        audioPanel = found.gameObject;
                        Debug.Log("[OptionsMenu] Auto-found AudioPanel");
                    }
                }

                if (controlsPanel == null)
                {
                    var found = contentArea.Find("ControlsPanel");
                    if (found != null)
                    {
                        controlsPanel = found.gameObject;
                        Debug.Log("[OptionsMenu] Auto-found ControlsPanel");
                    }
                }
            }

            // Find Display settings within DisplayPanel
            if (displayPanel != null)
            {
                if (aspectRatioDropdown == null)
                {
                    var found = displayPanel.transform.Find("AspectRatioRow/Dropdown");
                    if (found != null)
                    {
                        aspectRatioDropdown = found.GetComponent<TMP_Dropdown>();
                        Debug.Log("[OptionsMenu] Auto-found AspectRatioDropdown");
                    }
                }

                if (resolutionDropdown == null)
                {
                    var found = displayPanel.transform.Find("ResolutionRow/Dropdown");
                    if (found != null)
                    {
                        resolutionDropdown = found.GetComponent<TMP_Dropdown>();
                        Debug.Log("[OptionsMenu] Auto-found ResolutionDropdown");
                    }
                }

                if (windowModeDropdown == null)
                {
                    var found = displayPanel.transform.Find("WindowModeRow/Dropdown");
                    if (found != null)
                    {
                        windowModeDropdown = found.GetComponent<TMP_Dropdown>();
                        Debug.Log("[OptionsMenu] Auto-found WindowModeDropdown");
                    }
                }

                if (applyDisplayButton == null)
                {
                    var found = displayPanel.transform.Find("ApplyButton");
                    if (found != null)
                    {
                        applyDisplayButton = found.GetComponent<Button>();
                        Debug.Log("[OptionsMenu] Auto-found ApplyButton");
                    }
                }
            }

            // Find Audio settings within AudioPanel
            if (audioPanel != null)
            {
                if (masterVolumeSlider == null)
                {
                    var found = audioPanel.transform.Find("MasterRow/Slider");
                    if (found != null)
                    {
                        masterVolumeSlider = found.GetComponent<Slider>();
                        Debug.Log("[OptionsMenu] Auto-found MasterVolumeSlider");
                    }
                }

                if (musicVolumeSlider == null)
                {
                    var found = audioPanel.transform.Find("MusicRow/Slider");
                    if (found != null)
                    {
                        musicVolumeSlider = found.GetComponent<Slider>();
                        Debug.Log("[OptionsMenu] Auto-found MusicVolumeSlider");
                    }
                }

                if (sfxVolumeSlider == null)
                {
                    var found = audioPanel.transform.Find("SFXRow/Slider");
                    if (found != null)
                    {
                        sfxVolumeSlider = found.GetComponent<Slider>();
                        Debug.Log("[OptionsMenu] Auto-found SFXVolumeSlider");
                    }
                }

                if (masterVolumeText == null)
                {
                    var found = audioPanel.transform.Find("MasterRow/ValueText");
                    if (found != null)
                    {
                        masterVolumeText = found.GetComponent<TMP_Text>();
                        Debug.Log("[OptionsMenu] Auto-found MasterVolumeText");
                    }
                }

                if (musicVolumeText == null)
                {
                    var found = audioPanel.transform.Find("MusicRow/ValueText");
                    if (found != null)
                    {
                        musicVolumeText = found.GetComponent<TMP_Text>();
                        Debug.Log("[OptionsMenu] Auto-found MusicVolumeText");
                    }
                }

                if (sfxVolumeText == null)
                {
                    var found = audioPanel.transform.Find("SFXRow/ValueText");
                    if (found != null)
                    {
                        sfxVolumeText = found.GetComponent<TMP_Text>();
                        Debug.Log("[OptionsMenu] Auto-found SFXVolumeText");
                    }
                }
            }

            // Create missing UI elements
            EnsureAllContentExists();

            // Fix dropdown templates
            FixDropdownTemplate(aspectRatioDropdown);
            FixDropdownTemplate(resolutionDropdown);
            FixDropdownTemplate(windowModeDropdown);
        }

        private void EnsureAllContentExists()
        {
            var contentArea = transform.Find("ContentArea");
            if (contentArea == null)
            {
                // Create ContentArea if missing
                var contentAreaGO = new GameObject("ContentArea");
                contentAreaGO.transform.SetParent(transform, false);
                var contentRect = contentAreaGO.AddComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0, 0);
                contentRect.anchorMax = new Vector2(1, 1);
                contentRect.offsetMin = new Vector2(20, 20);
                contentRect.offsetMax = new Vector2(-20, -130);
                contentArea = contentAreaGO.transform;
                Debug.Log("[OptionsMenu] Created ContentArea");
            }

            // Ensure DisplayPanel exists with all content
            if (displayPanel == null)
            {
                displayPanel = CreatePanel(contentArea, "DisplayPanel");
            }
            EnsureDisplayPanelContent();

            // Ensure AudioPanel exists with all content
            if (audioPanel == null)
            {
                audioPanel = CreatePanel(contentArea, "AudioPanel");
                audioPanel.SetActive(false);
            }
            EnsureAudioPanelContent();

            // Ensure ControlsPanel exists with placeholder
            if (controlsPanel == null)
            {
                controlsPanel = CreatePanel(contentArea, "ControlsPanel");
                controlsPanel.SetActive(false);
                CreateControlsPlaceholder();
            }
        }

        private GameObject CreatePanel(Transform parent, string name)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            Debug.Log($"[OptionsMenu] Created {name}");
            return panel;
        }

        private void EnsureDisplayPanelContent()
        {
            if (displayPanel == null) return;

            // Ensure AspectRatioRow exists
            if (displayPanel.transform.Find("AspectRatioRow") == null)
            {
                CreateSettingsRow(displayPanel.transform, "AspectRatioRow", "Aspect Ratio", 0, true);
            }

            // Ensure ResolutionRow exists
            var resRow = displayPanel.transform.Find("ResolutionRow");
            if (resRow == null)
            {
                CreateSettingsRow(displayPanel.transform, "ResolutionRow", "Resolution", 1, true);
                resRow = displayPanel.transform.Find("ResolutionRow");
            }
            if (resolutionDropdown == null && resRow != null)
            {
                var dropdown = resRow.Find("Dropdown");
                if (dropdown != null)
                    resolutionDropdown = dropdown.GetComponent<TMP_Dropdown>();
            }

            // Ensure WindowModeRow exists
            var modeRow = displayPanel.transform.Find("WindowModeRow");
            if (modeRow == null)
            {
                CreateSettingsRow(displayPanel.transform, "WindowModeRow", "Window Mode", 2, true);
                modeRow = displayPanel.transform.Find("WindowModeRow");
            }
            if (windowModeDropdown == null && modeRow != null)
            {
                var dropdown = modeRow.Find("Dropdown");
                if (dropdown != null)
                    windowModeDropdown = dropdown.GetComponent<TMP_Dropdown>();
            }

            // Ensure ApplyButton exists
            if (displayPanel.transform.Find("ApplyButton") == null)
            {
                CreateApplyButton();
            }
            if (applyDisplayButton == null)
            {
                var applyBtn = displayPanel.transform.Find("ApplyButton");
                if (applyBtn != null)
                    applyDisplayButton = applyBtn.GetComponent<Button>();
            }
        }

        private void EnsureAudioPanelContent()
        {
            if (audioPanel == null) return;

            // Ensure MasterRow exists
            var masterRow = audioPanel.transform.Find("MasterRow");
            if (masterRow == null)
            {
                CreateSettingsRow(audioPanel.transform, "MasterRow", "Master Volume", 0, false);
                masterRow = audioPanel.transform.Find("MasterRow");
            }
            if (masterVolumeSlider == null && masterRow != null)
            {
                var slider = masterRow.Find("Slider");
                if (slider != null)
                    masterVolumeSlider = slider.GetComponent<Slider>();
                var valueText = masterRow.Find("ValueText");
                if (valueText != null)
                    masterVolumeText = valueText.GetComponent<TMP_Text>();
            }

            // Ensure MusicRow exists
            var musicRow = audioPanel.transform.Find("MusicRow");
            if (musicRow == null)
            {
                CreateSettingsRow(audioPanel.transform, "MusicRow", "Music", 1, false);
                musicRow = audioPanel.transform.Find("MusicRow");
            }
            if (musicVolumeSlider == null && musicRow != null)
            {
                var slider = musicRow.Find("Slider");
                if (slider != null)
                    musicVolumeSlider = slider.GetComponent<Slider>();
                var valueText = musicRow.Find("ValueText");
                if (valueText != null)
                    musicVolumeText = valueText.GetComponent<TMP_Text>();
            }

            // Ensure SFXRow exists
            var sfxRow = audioPanel.transform.Find("SFXRow");
            if (sfxRow == null)
            {
                CreateSettingsRow(audioPanel.transform, "SFXRow", "Sound Effects", 2, false);
                sfxRow = audioPanel.transform.Find("SFXRow");
            }
            if (sfxVolumeSlider == null && sfxRow != null)
            {
                var slider = sfxRow.Find("Slider");
                if (slider != null)
                    sfxVolumeSlider = slider.GetComponent<Slider>();
                var valueText = sfxRow.Find("ValueText");
                if (valueText != null)
                    sfxVolumeText = valueText.GetComponent<TMP_Text>();
            }
        }

        private void CreateControlsPlaceholder()
        {
            if (controlsPanel == null) return;

            var placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(controlsPanel.transform, false);
            var placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
            placeholderText.text = "Control remapping coming soon...";
            placeholderText.fontSize = 24;
            placeholderText.alignment = TextAlignmentOptions.Center;
            placeholderText.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            var placeholderRect = placeholder.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;
        }

        private void CreateSettingsRow(Transform parent, string name, string label, int rowIndex, bool isDropdown)
        {
            var row = new GameObject(name);
            row.transform.SetParent(parent, false);
            var rowRect = row.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 1);
            rowRect.anchorMax = new Vector2(1, 1);
            rowRect.pivot = new Vector2(0.5f, 1);
            rowRect.anchoredPosition = new Vector2(0, -20 - (rowIndex * 70));
            rowRect.sizeDelta = new Vector2(0, 60);

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(row.transform, false);
            var labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 24;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.color = new Color(0.961f, 0.961f, 0.863f, 1f);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(0.4f, 1);
            labelRect.offsetMin = new Vector2(10, 0);
            labelRect.offsetMax = Vector2.zero;

            if (isDropdown)
            {
                CreateDropdown(row.transform);
            }
            else
            {
                CreateSlider(row.transform);
            }

            Debug.Log($"[OptionsMenu] Created {name}");
        }

        private void CreateDropdown(Transform parent)
        {
            var dropdownGO = new GameObject("Dropdown");
            dropdownGO.transform.SetParent(parent, false);

            var dropdownImage = dropdownGO.AddComponent<Image>();
            dropdownImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            var dropdown = dropdownGO.AddComponent<TMP_Dropdown>();

            var dropdownRect = dropdownGO.GetComponent<RectTransform>();
            dropdownRect.anchorMin = new Vector2(0.45f, 0.1f);
            dropdownRect.anchorMax = new Vector2(1f, 0.9f);
            dropdownRect.offsetMin = Vector2.zero;
            dropdownRect.offsetMax = new Vector2(-10, 0);

            // Caption label (shows current selection)
            var ddLabel = new GameObject("Label");
            ddLabel.transform.SetParent(dropdownGO.transform, false);
            var ddLabelText = ddLabel.AddComponent<TextMeshProUGUI>();
            ddLabelText.text = "Select...";
            ddLabelText.fontSize = 20;
            ddLabelText.alignment = TextAlignmentOptions.MidlineLeft;
            ddLabelText.color = new Color(0.961f, 0.961f, 0.863f, 1f);
            var ddLabelRect = ddLabel.GetComponent<RectTransform>();
            ddLabelRect.anchorMin = Vector2.zero;
            ddLabelRect.anchorMax = Vector2.one;
            ddLabelRect.offsetMin = new Vector2(10, 0);
            ddLabelRect.offsetMax = new Vector2(-30, 0);

            dropdown.captionText = ddLabelText;

            // Create proper template
            CreateDropdownTemplate(dropdown);
        }

        private void CreateSlider(Transform parent)
        {
            var sliderGO = new GameObject("Slider");
            sliderGO.transform.SetParent(parent, false);

            var slider = sliderGO.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            var sliderRect = sliderGO.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.45f, 0.3f);
            sliderRect.anchorMax = new Vector2(0.85f, 0.7f);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;

            // Background
            var bg = new GameObject("Background");
            bg.transform.SetParent(sliderGO.transform, false);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Fill area
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGO.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.545f, 0f, 0f, 1f);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            slider.fillRect = fillRect;

            // Handle area
            var handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderGO.transform, false);
            var handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;

            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.812f, 0.710f, 0.231f, 1f);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);

            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;

            // Value text
            var valueText = new GameObject("ValueText");
            valueText.transform.SetParent(parent, false);
            var valueTmp = valueText.AddComponent<TextMeshProUGUI>();
            valueTmp.text = "100%";
            valueTmp.fontSize = 20;
            valueTmp.alignment = TextAlignmentOptions.MidlineRight;
            valueTmp.color = new Color(0.961f, 0.961f, 0.863f, 1f);
            var valueRect = valueText.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.87f, 0);
            valueRect.anchorMax = new Vector2(1f, 1);
            valueRect.offsetMin = Vector2.zero;
            valueRect.offsetMax = new Vector2(-10, 0);
        }

        private void CreateApplyButton()
        {
            if (displayPanel == null) return;

            var buttonGO = new GameObject("ApplyButton");
            buttonGO.transform.SetParent(displayPanel.transform, false);

            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.545f, 0f, 0f, 0.8f);

            var button = buttonGO.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.545f, 0f, 0f, 0.8f);
            colors.highlightedColor = new Color(0.812f, 0.710f, 0.231f, 1f);
            colors.pressedColor = new Color(0.4f, 0f, 0f, 1f);
            colors.selectedColor = new Color(0.812f, 0.710f, 0.231f, 1f);
            button.colors = colors;

            var buttonRect = buttonGO.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0);
            buttonRect.anchorMax = new Vector2(0.5f, 0);
            buttonRect.pivot = new Vector2(0.5f, 0);
            buttonRect.anchoredPosition = new Vector2(0, 20);
            buttonRect.sizeDelta = new Vector2(200, 45);

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "Apply";
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.961f, 0.961f, 0.863f, 1f);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Debug.Log("[OptionsMenu] Created ApplyButton");
        }

        private void FixDropdownTemplate(TMP_Dropdown dropdown)
        {
            if (dropdown == null) return;

            // Find and assign caption text first
            if (dropdown.captionText == null)
            {
                var captionText = dropdown.transform.Find("Label");
                if (captionText != null)
                {
                    dropdown.captionText = captionText.GetComponent<TMP_Text>();
                }
            }

            // Check if template exists and is valid
            var templateTransform = dropdown.transform.Find("Template");
            bool needsNewTemplate = false;

            if (templateTransform == null)
            {
                needsNewTemplate = true;
            }
            else
            {
                // Check if template has proper structure (Toggle in hierarchy)
                var toggle = templateTransform.GetComponentInChildren<Toggle>(true);
                if (toggle == null)
                {
                    needsNewTemplate = true;
                    // Destroy the broken template
                    DestroyImmediate(templateTransform.gameObject);
                    Debug.Log($"[OptionsMenu] Destroyed broken template for {dropdown.name}");
                }
            }

            if (needsNewTemplate)
            {
                CreateDropdownTemplate(dropdown);
            }
            else
            {
                // Template exists and has Toggle - just ensure references are set
                dropdown.template = templateTransform.GetComponent<RectTransform>();

                if (dropdown.itemText == null)
                {
                    var itemText = templateTransform.GetComponentInChildren<TMP_Text>(true);
                    if (itemText != null && itemText.transform.parent.GetComponent<Toggle>() != null)
                    {
                        dropdown.itemText = itemText;
                    }
                }
            }

            Debug.Log($"[OptionsMenu] Fixed dropdown template for {dropdown.name}");
        }

        private void CreateDropdownTemplate(TMP_Dropdown dropdown)
        {
            // Create template structure for TMP_Dropdown
            var template = new GameObject("Template");
            template.transform.SetParent(dropdown.transform, false);
            var templateImage = template.AddComponent<Image>();
            templateImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            var templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.anchoredPosition = Vector2.zero;
            templateRect.sizeDelta = new Vector2(0, 150);

            // Add ScrollRect for scrolling
            var scrollRect = template.AddComponent<ScrollRect>();

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(template.transform, false);
            viewport.AddComponent<RectMask2D>();
            var viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 28);

            var item = new GameObject("Item");
            item.transform.SetParent(content.transform, false);
            var itemBg = item.AddComponent<Image>();
            itemBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            var itemToggle = item.AddComponent<Toggle>();
            itemToggle.targetGraphic = itemBg;
            var colors = itemToggle.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            colors.highlightedColor = new Color(0.545f, 0f, 0f, 1f);
            colors.selectedColor = new Color(0.545f, 0f, 0f, 1f);
            itemToggle.colors = colors;
            var itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.pivot = new Vector2(0.5f, 0.5f);
            itemRect.sizeDelta = new Vector2(0, 28);

            var itemLabel = new GameObject("Item Label");
            itemLabel.transform.SetParent(item.transform, false);
            var itemLabelText = itemLabel.AddComponent<TextMeshProUGUI>();
            itemLabelText.fontSize = 18;
            itemLabelText.alignment = TextAlignmentOptions.MidlineLeft;
            itemLabelText.color = new Color(0.961f, 0.961f, 0.863f, 1f);
            var itemLabelRect = itemLabel.GetComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(10, 0);
            itemLabelRect.offsetMax = Vector2.zero;

            // Setup ScrollRect
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Assign to dropdown
            dropdown.template = templateRect;
            dropdown.itemText = itemLabelText;

            // Ensure caption text is set
            if (dropdown.captionText == null)
            {
                var captionText = dropdown.transform.Find("Label");
                if (captionText != null)
                {
                    dropdown.captionText = captionText.GetComponent<TMP_Text>();
                }
            }

            template.SetActive(false);
            Debug.Log($"[OptionsMenu] Created dropdown template for {dropdown.name}");
        }

        private void DebugPanelContents()
        {
            Debug.Log($"[OptionsMenu] === Panel Debug ===");
            Debug.Log($"[OptionsMenu] DisplayPanel: {(displayPanel != null ? "EXISTS" : "NULL")}");
            if (displayPanel != null)
            {
                foreach (Transform child in displayPanel.transform)
                {
                    Debug.Log($"[OptionsMenu]   - {child.name} (active: {child.gameObject.activeSelf})");
                }
            }

            Debug.Log($"[OptionsMenu] AudioPanel: {(audioPanel != null ? "EXISTS" : "NULL")}");
            if (audioPanel != null)
            {
                foreach (Transform child in audioPanel.transform)
                {
                    Debug.Log($"[OptionsMenu]   - {child.name} (active: {child.gameObject.activeSelf})");
                }
            }

            Debug.Log($"[OptionsMenu] ControlsPanel: {(controlsPanel != null ? "EXISTS" : "NULL")}");
            if (controlsPanel != null)
            {
                foreach (Transform child in controlsPanel.transform)
                {
                    Debug.Log($"[OptionsMenu]   - {child.name} (active: {child.gameObject.activeSelf})");
                }
            }
        }

        private void OnEnable()
        {
            // Debug what's in each panel
            DebugPanelContents();

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

            // Refresh settings when opened
            RefreshDisplaySettings();
            RefreshAudioSettings();

            // Show first tab
            SwitchToTab(0, immediate: true);
        }

        private void OnDisable()
        {
            if (tabLeftAction != null)
                tabLeftAction.action.performed -= OnTabLeft;

            if (tabRightAction != null)
                tabRightAction.action.performed -= OnTabRight;
        }

        #region Tab Navigation

        private void SetupTabButtons()
        {
            if (displayTabButton != null)
                displayTabButton.onClick.AddListener(() => SwitchToTab(0));

            if (audioTabButton != null)
                audioTabButton.onClick.AddListener(() => SwitchToTab(1));

            if (controlsTabButton != null)
                controlsTabButton.onClick.AddListener(() => SwitchToTab(2));
        }

        private void OnTabLeft(InputAction.CallbackContext ctx)
        {
            int newIndex = currentTabIndex - 1;
            if (newIndex < 0) newIndex = tabButtons.Length - 1;
            SwitchToTab(newIndex);
            UIManager.Instance?.PlayTabSwitchSound();
        }

        private void OnTabRight(InputAction.CallbackContext ctx)
        {
            int newIndex = (currentTabIndex + 1) % tabButtons.Length;
            SwitchToTab(newIndex);
            UIManager.Instance?.PlayTabSwitchSound();
        }

        public void SwitchToTab(int index, bool immediate = false)
        {
            if (index < 0 || index >= tabButtons.Length) return;

            currentTabIndex = index;

            // Update panels
            for (int i = 0; i < tabPanels.Length; i++)
            {
                if (tabPanels[i] != null)
                    tabPanels[i].SetActive(i == index);
            }

            // Update tab button visuals
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] != null)
                {
                    var image = tabButtons[i].GetComponent<Image>();
                    var text = tabButtons[i].GetComponentInChildren<TMP_Text>();

                    if (image != null)
                        image.color = i == index ? selectedTabColor : normalTabColor;

                    if (text != null)
                        text.color = i == index ? selectedTextColor : normalTextColor;
                }
            }

            // Move indicator
            if (tabIndicator != null && tabButtons[index] != null)
            {
                var targetRect = tabButtons[index].GetComponent<RectTransform>();
                if (targetRect != null)
                {
                    if (immediate)
                    {
                        tabIndicator.position = new Vector3(targetRect.position.x, tabIndicator.position.y, tabIndicator.position.z);
                    }
                    else
                    {
                        StopAllCoroutines();
                        StartCoroutine(MoveIndicator(targetRect.position.x));
                    }
                }
            }
        }

        private IEnumerator MoveIndicator(float targetX)
        {
            Vector3 startPos = tabIndicator.position;
            Vector3 targetPos = new Vector3(targetX, startPos.y, startPos.z);
            float elapsed = 0f;

            while (elapsed < indicatorMoveSpeed)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / indicatorMoveSpeed);
                t = 1f - (1f - t) * (1f - t); // Ease out
                tabIndicator.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            tabIndicator.position = targetPos;
        }

        #endregion

        #region Display Settings

        private void SetupDisplaySettings()
        {
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

            if (windowModeDropdown != null)
                windowModeDropdown.onValueChanged.AddListener(OnWindowModeChanged);

            if (aspectRatioDropdown != null)
                aspectRatioDropdown.onValueChanged.AddListener(OnAspectRatioChanged);

            if (applyDisplayButton != null)
                applyDisplayButton.onClick.AddListener(OnApplyDisplayClicked);
        }

        private void RefreshDisplaySettings()
        {
            if (DisplaySettings.Instance == null) return;

            var ds = DisplaySettings.Instance;

            // Cache current settings for potential revert
            cachedResolutionIndex = ds.CurrentResolutionIndex;
            cachedWindowModeIndex = (int)ds.CurrentWindowMode;
            cachedAspectRatioIndex = (int)ds.CurrentAspectRatio;

            // Populate dropdowns
            if (aspectRatioDropdown != null)
            {
                aspectRatioDropdown.ClearOptions();
                aspectRatioDropdown.AddOptions(new System.Collections.Generic.List<string>(ds.GetAspectRatioStrings()));
                aspectRatioDropdown.SetValueWithoutNotify(cachedAspectRatioIndex);
            }

            if (windowModeDropdown != null)
            {
                windowModeDropdown.ClearOptions();
                windowModeDropdown.AddOptions(new System.Collections.Generic.List<string>(ds.GetWindowModeStrings()));
                windowModeDropdown.SetValueWithoutNotify(cachedWindowModeIndex);
            }

            RefreshResolutionDropdown();
        }

        private void RefreshResolutionDropdown()
        {
            if (resolutionDropdown == null || DisplaySettings.Instance == null) return;

            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(new System.Collections.Generic.List<string>(
                DisplaySettings.Instance.GetResolutionStrings()));
            resolutionDropdown.SetValueWithoutNotify(DisplaySettings.Instance.CurrentResolutionIndex);
        }

        private void OnResolutionChanged(int index)
        {
            DisplaySettings.Instance?.SetResolution(index);
        }

        private void OnWindowModeChanged(int index)
        {
            DisplaySettings.Instance?.SetWindowMode(index);
        }

        private void OnAspectRatioChanged(int index)
        {
            DisplaySettings.Instance?.SetAspectRatio(index);
            RefreshResolutionDropdown();
        }

        private void OnApplyDisplayClicked()
        {
            DisplaySettings.Instance?.ApplyAndSave();
            UIManager.Instance?.PlayConfirmSound();

            // Update cached values
            if (DisplaySettings.Instance != null)
            {
                cachedResolutionIndex = DisplaySettings.Instance.CurrentResolutionIndex;
                cachedWindowModeIndex = (int)DisplaySettings.Instance.CurrentWindowMode;
                cachedAspectRatioIndex = (int)DisplaySettings.Instance.CurrentAspectRatio;
            }
        }

        #endregion

        #region Audio Settings

        private void SetupAudioSettings()
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        private void RefreshAudioSettings()
        {
            // Load from PlayerPrefs or AudioManager
            float master = PlayerPrefs.GetFloat("Audio_Master", 1f);
            float music = PlayerPrefs.GetFloat("Audio_Music", 1f);
            float sfx = PlayerPrefs.GetFloat("Audio_SFX", 1f);

            if (masterVolumeSlider != null)
                masterVolumeSlider.SetValueWithoutNotify(master);

            if (musicVolumeSlider != null)
                musicVolumeSlider.SetValueWithoutNotify(music);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.SetValueWithoutNotify(sfx);

            UpdateVolumeTexts();
        }

        private void OnMasterVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("Audio_Master", value);
            AudioListener.volume = value;
            UpdateVolumeTexts();
        }

        private void OnMusicVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("Audio_Music", value);
            // TODO: Apply to music audio source/mixer
            UpdateVolumeTexts();
        }

        private void OnSFXVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("Audio_SFX", value);
            // TODO: Apply to SFX audio source/mixer
            UpdateVolumeTexts();
        }

        private void UpdateVolumeTexts()
        {
            if (masterVolumeText != null && masterVolumeSlider != null)
                masterVolumeText.text = $"{Mathf.RoundToInt(masterVolumeSlider.value * 100)}%";

            if (musicVolumeText != null && musicVolumeSlider != null)
                musicVolumeText.text = $"{Mathf.RoundToInt(musicVolumeSlider.value * 100)}%";

            if (sfxVolumeText != null && sfxVolumeSlider != null)
                sfxVolumeText.text = $"{Mathf.RoundToInt(sfxVolumeSlider.value * 100)}%";
        }

        #endregion
    }
}
