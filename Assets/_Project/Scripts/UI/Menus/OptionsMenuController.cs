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

        [Header("Graphics Settings")]
        [SerializeField] private Slider brightnessSlider;
        [SerializeField] private Slider contrastSlider;
        [SerializeField] private Slider saturationSlider;
        [SerializeField] private TMP_Text brightnessText;
        [SerializeField] private TMP_Text contrastText;
        [SerializeField] private TMP_Text saturationText;
        [SerializeField] private Button resetGraphicsButton;

        [Header("Audio Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TMP_Text masterVolumeText;
        [SerializeField] private TMP_Text musicVolumeText;
        [SerializeField] private TMP_Text sfxVolumeText;

        [Header("Tab Colors")]
        [SerializeField] private Color normalTabColor = new Color(0.10f, 0.10f, 0.18f, 1f);
        [SerializeField] private Color selectedTabColor = new Color(0.55f, 0f, 0f, 1f);
        [SerializeField] private Color normalTextColor = new Color(0.93f, 0.89f, 0.82f, 1f);
        [SerializeField] private Color selectedTextColor = new Color(0.81f, 0.71f, 0.23f, 1f);

        // Shared palette
        private static readonly Color BtnNormal = new Color(0.10f, 0.10f, 0.18f, 1f);
        private static readonly Color BtnSelected = new Color(0.55f, 0f, 0f, 1f);
        private static readonly Color BtnPress = new Color(0.08f, 0.08f, 0.14f, 1f);
        private static readonly Color PanelBg = new Color(0.05f, 0.04f, 0.07f, 0.95f);
        private static readonly Color AgedGold = new Color(0.81f, 0.71f, 0.23f, 1f);
        private static readonly Color BoneWhite = new Color(0.93f, 0.89f, 0.82f, 1f);
        private static readonly Color RowBg = new Color(0.08f, 0.08f, 0.14f, 0.6f);

        private int currentTabIndex = 0;
        private Button[] tabButtons;
        private GameObject[] tabPanels;

        // Cached settings for revert
        private int cachedResolutionIndex;
        private int cachedWindowModeIndex;
        private int cachedAspectRatioIndex;

        private void ApplyDefaultFont(TMP_Text textComponent)
        {
            // Use FontManager for consistent font management across the project
            FontManager.EnsureFont(textComponent);
        }

        private void Awake()
        {
            AutoFindReferences();

            tabButtons = new Button[] { displayTabButton, audioTabButton, controlsTabButton };
            tabPanels = new GameObject[] { displayPanel, audioPanel, controlsPanel };

            SetupTabButtons();
            SetupDisplaySettings();
            SetupAudioSettings();
            SetupGraphicsSettings();
            StyleOptionsChrome();
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

            // Graphics sliders (row indices 4-6, gap after display dropdowns)
            var brightRow = displayPanel.transform.Find("BrightnessRow");
            if (brightRow == null)
            {
                CreateSettingsRow(displayPanel.transform, "BrightnessRow", "Brightness", 4, false);
                brightRow = displayPanel.transform.Find("BrightnessRow");
            }
            if (brightnessSlider == null && brightRow != null)
            {
                var slider = brightRow.Find("Slider");
                if (slider != null)
                {
                    brightnessSlider = slider.GetComponent<Slider>();
                    brightnessSlider.minValue = -2f;
                    brightnessSlider.maxValue = 2f;
                    brightnessSlider.value = 0f;
                }
                var valueText = brightRow.Find("ValueText");
                if (valueText != null)
                    brightnessText = valueText.GetComponent<TMP_Text>();
            }

            var contRow = displayPanel.transform.Find("ContrastRow");
            if (contRow == null)
            {
                CreateSettingsRow(displayPanel.transform, "ContrastRow", "Contrast", 5, false);
                contRow = displayPanel.transform.Find("ContrastRow");
            }
            if (contrastSlider == null && contRow != null)
            {
                var slider = contRow.Find("Slider");
                if (slider != null)
                {
                    contrastSlider = slider.GetComponent<Slider>();
                    contrastSlider.minValue = -100f;
                    contrastSlider.maxValue = 100f;
                    contrastSlider.value = 0f;
                }
                var valueText = contRow.Find("ValueText");
                if (valueText != null)
                    contrastText = valueText.GetComponent<TMP_Text>();
            }

            var satRow = displayPanel.transform.Find("SaturationRow");
            if (satRow == null)
            {
                CreateSettingsRow(displayPanel.transform, "SaturationRow", "Saturation", 6, false);
                satRow = displayPanel.transform.Find("SaturationRow");
            }
            if (saturationSlider == null && satRow != null)
            {
                var slider = satRow.Find("Slider");
                if (slider != null)
                {
                    saturationSlider = slider.GetComponent<Slider>();
                    saturationSlider.minValue = -100f;
                    saturationSlider.maxValue = 100f;
                    saturationSlider.value = 0f;
                }
                var valueText = satRow.Find("ValueText");
                if (valueText != null)
                    saturationText = valueText.GetComponent<TMP_Text>();
            }

            // Reset Graphics button
            if (displayPanel.transform.Find("ResetGraphicsButton") == null)
            {
                CreateResetGraphicsButton();
            }
            if (resetGraphicsButton == null)
            {
                var resetBtn = displayPanel.transform.Find("ResetGraphicsButton");
                if (resetBtn != null)
                    resetGraphicsButton = resetBtn.GetComponent<Button>();
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
            ApplyDefaultFont(placeholderText);
            placeholderText.text = "Control remapping coming soon...";
            placeholderText.fontSize = 24;
            placeholderText.alignment = TextAlignmentOptions.Center;
            placeholderText.color = new Color(0.93f, 0.89f, 0.82f, 0.5f);
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
            ApplyDefaultFont(labelText);
            labelText.text = label;
            labelText.fontSize = 24;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.color = BoneWhite;
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
            dropdownImage.color = BtnNormal;

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
            ApplyDefaultFont(ddLabelText);
            ddLabelText.text = "Select...";
            ddLabelText.fontSize = 20;
            ddLabelText.alignment = TextAlignmentOptions.MidlineLeft;
            ddLabelText.color = BoneWhite;
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
            bgImage.color = BtnNormal;
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
            fillImage.color = BtnSelected;
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
            handleImage.color = AgedGold;
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);

            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;

            // Value text
            var valueText = new GameObject("ValueText");
            valueText.transform.SetParent(parent, false);
            var valueTmp = valueText.AddComponent<TextMeshProUGUI>();
            ApplyDefaultFont(valueTmp);
            valueTmp.text = "100%";
            valueTmp.fontSize = 20;
            valueTmp.alignment = TextAlignmentOptions.MidlineRight;
            valueTmp.color = BoneWhite;
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
            buttonImage.color = Color.white;

            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            var colors = button.colors;
            colors.normalColor = BtnNormal;
            colors.highlightedColor = BtnSelected;
            colors.pressedColor = BtnPress;
            colors.selectedColor = BtnSelected;
            colors.fadeDuration = 0.1f;
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
            ApplyDefaultFont(tmp);
            tmp.text = "Apply";
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = BoneWhite;
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Debug.Log("[OptionsMenu] Created ApplyButton");
        }

        private void CreateResetGraphicsButton()
        {
            if (displayPanel == null) return;

            var buttonGO = new GameObject("ResetGraphicsButton");
            buttonGO.transform.SetParent(displayPanel.transform, false);

            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = Color.white;

            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            var colors = button.colors;
            colors.normalColor = BtnNormal;
            colors.highlightedColor = BtnSelected;
            colors.pressedColor = BtnPress;
            colors.selectedColor = BtnSelected;
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            var buttonRect = buttonGO.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0);
            buttonRect.anchorMax = new Vector2(0.5f, 0);
            buttonRect.pivot = new Vector2(0.5f, 0);
            buttonRect.anchoredPosition = new Vector2(-120, 20);
            buttonRect.sizeDelta = new Vector2(200, 45);

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            ApplyDefaultFont(tmp);
            tmp.text = "Reset Graphics";
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = BoneWhite;
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Debug.Log("[OptionsMenu] Created ResetGraphicsButton");
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
                    Destroy(templateTransform.gameObject);
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
            ApplyDefaultFont(itemLabelText);
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
            RefreshGraphicsSettings();

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

        private void StyleOptionsChrome()
        {
            // Full-screen dark background
            var rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            var panelImg = GetComponent<Image>();
            if (panelImg == null)
                panelImg = gameObject.AddComponent<Image>();
            panelImg.color = PanelBg;

            // Style header — find title text and set gold
            var header = transform.Find("Header");
            if (header != null)
            {
                // Remove header background
                var headerImg = header.GetComponent<Image>();
                if (headerImg != null) headerImg.color = Color.clear;

                var headerTitle = header.GetComponentInChildren<TMP_Text>();
                if (headerTitle != null)
                {
                    headerTitle.color = AgedGold;
                    headerTitle.fontSize = 34f;
                    headerTitle.text = "Options";
                }

                // Style back button
                var backBtn = header.GetComponentInChildren<Button>();
                if (backBtn != null)
                {
                    var backImg = backBtn.GetComponent<Image>();
                    if (backImg != null)
                    {
                        backImg.color = Color.white;
                        backBtn.targetGraphic = backImg;
                    }
                    var colors = backBtn.colors;
                    colors.normalColor = BtnNormal;
                    colors.highlightedColor = BtnSelected;
                    colors.pressedColor = BtnPress;
                    colors.selectedColor = BtnSelected;
                    colors.fadeDuration = 0.1f;
                    backBtn.colors = colors;

                    var backTmp = backBtn.GetComponentInChildren<TMP_Text>();
                    if (backTmp != null) backTmp.color = BoneWhite;
                }
            }

            // Style tab buttons
            foreach (var tabBtn in new[] { displayTabButton, audioTabButton, controlsTabButton })
            {
                if (tabBtn == null) continue;

                var tabImg = tabBtn.GetComponent<Image>();
                if (tabImg != null)
                {
                    tabImg.color = Color.white;
                    tabBtn.targetGraphic = tabImg;
                }

                var colors = tabBtn.colors;
                colors.normalColor = BtnNormal;
                colors.highlightedColor = BtnSelected;
                colors.pressedColor = BtnPress;
                colors.selectedColor = BtnSelected;
                colors.fadeDuration = 0.1f;
                tabBtn.colors = colors;
            }

            // Style tab bar background
            var tabBar = transform.Find("TabBar");
            if (tabBar != null)
            {
                var tabBarImg = tabBar.GetComponent<Image>();
                if (tabBarImg != null) tabBarImg.color = new Color(0.06f, 0.06f, 0.10f, 0.8f);
            }

            // Style settings rows and their controls
            StyleSettingsControls();
        }

        private void StyleSettingsControls()
        {
            // Style all dropdown backgrounds
            var dropdowns = GetComponentsInChildren<TMP_Dropdown>(true);
            foreach (var dd in dropdowns)
            {
                var ddImg = dd.GetComponent<Image>();
                if (ddImg != null)
                    ddImg.color = BtnNormal;
            }

            // Style all slider backgrounds
            var sliders = GetComponentsInChildren<Slider>(true);
            foreach (var s in sliders)
            {
                // Slider background
                var bgTransform = s.transform.Find("Background");
                if (bgTransform != null)
                {
                    var bgImg = bgTransform.GetComponent<Image>();
                    if (bgImg != null) bgImg.color = BtnNormal;
                }
            }

            // Style Apply and Reset buttons
            StyleActionButton(displayPanel, "ApplyButton");
            StyleActionButton(displayPanel, "ResetGraphicsButton");
        }

        private void StyleActionButton(GameObject panel, string buttonName)
        {
            if (panel == null) return;
            var btnTransform = panel.transform.Find(buttonName);
            if (btnTransform == null) return;

            var btn = btnTransform.GetComponent<Button>();
            if (btn == null) return;

            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = Color.white;
                btn.targetGraphic = img;
            }

            var colors = btn.colors;
            colors.normalColor = BtnNormal;
            colors.highlightedColor = BtnSelected;
            colors.pressedColor = BtnPress;
            colors.selectedColor = BtnSelected;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;

            var tmp = btn.GetComponentInChildren<TMP_Text>();
            if (tmp != null) tmp.color = BoneWhite;
        }

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
            // Ensure DisplaySettings exists
            if (DisplaySettings.Instance == null)
            {
                var go = new GameObject("DisplaySettings");
                go.AddComponent<DisplaySettings>(); // DisplaySettings handles DontDestroyOnLoad in Awake
                Debug.Log("[OptionsMenu] Created DisplaySettings instance");
            }

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
            MusicManager.Instance?.SetVolume(value);
            UpdateVolumeTexts();
        }

        private void OnSFXVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("Audio_SFX", value);
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

        #region Graphics Settings

        private void SetupGraphicsSettings()
        {
            if (brightnessSlider != null)
                brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
            if (contrastSlider != null)
                contrastSlider.onValueChanged.AddListener(OnContrastChanged);
            if (saturationSlider != null)
                saturationSlider.onValueChanged.AddListener(OnSaturationChanged);
            if (resetGraphicsButton != null)
                resetGraphicsButton.onClick.AddListener(OnResetGraphicsClicked);
        }

        private void RefreshGraphicsSettings()
        {
            if (GraphicsSettings.Instance == null)
            {
                var go = new GameObject("GraphicsSettings");
                go.AddComponent<GraphicsSettings>();
            }

            var gs = GraphicsSettings.Instance;
            if (gs == null) return;

            if (brightnessSlider != null)
                brightnessSlider.SetValueWithoutNotify(gs.Brightness);
            if (contrastSlider != null)
                contrastSlider.SetValueWithoutNotify(gs.Contrast);
            if (saturationSlider != null)
                saturationSlider.SetValueWithoutNotify(gs.Saturation);

            UpdateGraphicsTexts();
        }

        private void OnBrightnessChanged(float value)
        {
            GraphicsSettings.Instance?.SetBrightness(value);
            UpdateGraphicsTexts();
        }

        private void OnContrastChanged(float value)
        {
            GraphicsSettings.Instance?.SetContrast(value);
            UpdateGraphicsTexts();
        }

        private void OnSaturationChanged(float value)
        {
            GraphicsSettings.Instance?.SetSaturation(value);
            UpdateGraphicsTexts();
        }

        private void OnResetGraphicsClicked()
        {
            GraphicsSettings.Instance?.ResetToDefaults();
            RefreshGraphicsSettings();
        }

        private void UpdateGraphicsTexts()
        {
            if (brightnessText != null && brightnessSlider != null)
                brightnessText.text = brightnessSlider.value.ToString("F1");
            if (contrastText != null && contrastSlider != null)
                contrastText.text = Mathf.RoundToInt(contrastSlider.value).ToString();
            if (saturationText != null && saturationSlider != null)
                saturationText.text = Mathf.RoundToInt(saturationSlider.value).ToString();
        }

        #endregion

        private void OnDestroy()
        {
            if (displayTabButton != null) displayTabButton.onClick.RemoveAllListeners();
            if (audioTabButton != null) audioTabButton.onClick.RemoveAllListeners();
            if (controlsTabButton != null) controlsTabButton.onClick.RemoveAllListeners();
            if (applyDisplayButton != null) applyDisplayButton.onClick.RemoveAllListeners();

            if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
            if (windowModeDropdown != null) windowModeDropdown.onValueChanged.RemoveListener(OnWindowModeChanged);
            if (aspectRatioDropdown != null) aspectRatioDropdown.onValueChanged.RemoveListener(OnAspectRatioChanged);

            if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);

            if (brightnessSlider != null) brightnessSlider.onValueChanged.RemoveListener(OnBrightnessChanged);
            if (contrastSlider != null) contrastSlider.onValueChanged.RemoveListener(OnContrastChanged);
            if (saturationSlider != null) saturationSlider.onValueChanged.RemoveListener(OnSaturationChanged);
            if (resetGraphicsButton != null) resetGraphicsButton.onClick.RemoveAllListeners();
        }
    }
}
