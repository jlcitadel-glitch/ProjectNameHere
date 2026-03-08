using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Single-page options menu inspired by Hollow Knight.
    /// All settings on one screen — no tabs.
    /// </summary>
    public class OptionsMenuController : MonoBehaviour
    {
        // Palette
        private static readonly Color BtnNormal = new Color(0.10f, 0.10f, 0.18f, 1f);
        private static readonly Color BtnSelected = new Color(0.55f, 0f, 0f, 1f);
        private static readonly Color BtnPress = new Color(0.08f, 0.08f, 0.14f, 1f);
        private static readonly Color PanelBg = new Color(0.05f, 0.04f, 0.07f, 0.95f);
        private static readonly Color AgedGold = new Color(0.81f, 0.71f, 0.23f, 1f);
        private static readonly Color BoneWhite = new Color(0.93f, 0.89f, 0.82f, 1f);

        private const float ROW_HEIGHT = 55f;
        private const float ROW_SPACING = 8f;
        private const float FONT_SIZE = 24f;
        private const float TITLE_SIZE = 34f;

        // Runtime references
        private Button backButton;
        private TMP_Dropdown aspectRatioDropdown;
        private TMP_Dropdown resolutionDropdown;
        private Button fullscreenToggle;
        private TMP_Text fullscreenValueText;
        private Button vsyncToggle;
        private TMP_Text vsyncValueText;
        private Slider brightnessSlider;
        private TMP_Text brightnessText;
        private Slider musicSlider;
        private TMP_Text musicText;
        private Slider soundSlider;
        private TMP_Text soundText;
        private Button resetButton;

        private bool isFullscreen;
        private bool isVsync;
        private readonly List<Selectable> allSelectables = new List<Selectable>();

        private void Awake()
        {
            BuildUI();
        }

        private void OnEnable()
        {
            EnsureDisplaySettings();
            EnsureGraphicsSettings();

            RefreshAllValues();
            WireNavigation();

            // Set initial focus on first selectable
            if (allSelectables.Count > 0)
            {
                var es = EventSystem.current;
                if (es != null)
                    es.SetSelectedGameObject(allSelectables[0].gameObject);
            }
        }

        #region UI Construction

        private void BuildUI()
        {
            // Full-screen dark background
            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.color = PanelBg;

            // Destroy any existing children (prefab leftovers)
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            // Content column — centered, fixed width
            var column = CreateChild(transform, "Content");
            var colRt = column.GetComponent<RectTransform>();
            colRt.anchorMin = new Vector2(0.5f, 0.5f);
            colRt.anchorMax = new Vector2(0.5f, 0.5f);
            colRt.pivot = new Vector2(0.5f, 0.5f);
            colRt.anchoredPosition = Vector2.zero;
            colRt.sizeDelta = new Vector2(600f, 765f);

            float y = 0f;

            // Title
            CreateTitle(column.transform, "Options", ref y);
            y -= 20f;

            // --- Display ---
            CreateSectionHeader(column.transform, "Display", ref y);
            aspectRatioDropdown = CreateDropdownRow(column.transform, "Aspect Ratio", ref y);
            resolutionDropdown = CreateDropdownRow(column.transform, "Resolution", ref y);
            fullscreenToggle = CreateToggleRow(column.transform, "Fullscreen", out fullscreenValueText, ref y);
            vsyncToggle = CreateToggleRow(column.transform, "VSync", out vsyncValueText, ref y);
            y -= 15f;

            // --- Graphics ---
            CreateSectionHeader(column.transform, "Graphics", ref y);
            brightnessSlider = CreateSliderRow(column.transform, "Brightness", -1f, 1f, out brightnessText, ref y);
            y -= 15f;

            // --- Audio ---
            CreateSectionHeader(column.transform, "Audio", ref y);
            musicSlider = CreateSliderRow(column.transform, "Music", 0f, 1f, out musicText, ref y);
            soundSlider = CreateSliderRow(column.transform, "Sound", 0f, 1f, out soundText, ref y);
            y -= 20f;

            // Reset + Back buttons
            resetButton = CreateActionButton(column.transform, "Reset", 0f, ref y);
            backButton = CreateActionButton(column.transform, "Back", 0f, ref y);

            // Wire callbacks
            SetupCallbacks();

            // Style all text
            foreach (var tmp in GetComponentsInChildren<TMP_Text>(true))
            {
                FontManager.EnsureFont(tmp);
                if (tmp.fontSize != TITLE_SIZE && tmp.fontSize != 18f && tmp.fontSize != 20f)
                    tmp.fontSize = FONT_SIZE;
            }
        }

        private GameObject CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private void CreateTitle(Transform parent, string text, ref float y)
        {
            var go = CreateChild(parent, "Title");
            var tmp = go.AddComponent<TextMeshProUGUI>();
            FontManager.EnsureFont(tmp);
            tmp.text = text;
            tmp.fontSize = TITLE_SIZE;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = AgedGold;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(0, 50f);
            y -= 50f;
        }

        private void CreateSectionHeader(Transform parent, string text, ref float y)
        {
            var go = CreateChild(parent, text + "Header");
            var tmp = go.AddComponent<TextMeshProUGUI>();
            FontManager.EnsureFont(tmp);
            tmp.text = text;
            tmp.fontSize = 20f;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = new Color(AgedGold.r, AgedGold.g, AgedGold.b, 0.6f);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(0, 30f);
            y -= 35f;
        }

        private (RectTransform rowRt, TMP_Text label) CreateRowBase(Transform parent, string labelText, ref float y)
        {
            var row = CreateChild(parent, labelText + "Row");
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0, 1);
            rowRt.anchorMax = new Vector2(1, 1);
            rowRt.pivot = new Vector2(0.5f, 1);
            rowRt.anchoredPosition = new Vector2(0, y);
            rowRt.sizeDelta = new Vector2(0, ROW_HEIGHT);

            // Row background for focus highlight
            var rowImg = row.AddComponent<Image>();
            rowImg.color = Color.clear;
            row.AddComponent<SettingsRowHighlight>();

            // Label
            var labelGO = CreateChild(row.transform, "Label");
            var label = labelGO.AddComponent<TextMeshProUGUI>();
            FontManager.EnsureFont(label);
            label.text = labelText;
            label.fontSize = FONT_SIZE;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = BoneWhite;
            var labelRt = labelGO.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0, 0);
            labelRt.anchorMax = new Vector2(0.4f, 1);
            labelRt.offsetMin = new Vector2(10, 0);
            labelRt.offsetMax = Vector2.zero;

            y -= (ROW_HEIGHT + ROW_SPACING);
            return (rowRt, label);
        }

        private TMP_Dropdown CreateDropdownRow(Transform parent, string label, ref float y)
        {
            var (rowRt, _) = CreateRowBase(parent, label, ref y);

            var ddGO = CreateChild(rowRt.transform, "Dropdown");
            var ddImg = ddGO.AddComponent<Image>();
            ddImg.color = Color.white;

            var dd = ddGO.AddComponent<TMP_Dropdown>();
            dd.targetGraphic = ddImg;
            var colors = dd.colors;
            colors.normalColor = BtnNormal;
            colors.highlightedColor = BtnSelected;
            colors.pressedColor = BtnPress;
            colors.selectedColor = BtnSelected;
            colors.fadeDuration = 0.1f;
            dd.colors = colors;

            var ddRt = ddGO.GetComponent<RectTransform>();
            ddRt.anchorMin = new Vector2(0.42f, 0.1f);
            ddRt.anchorMax = new Vector2(1f, 0.9f);
            ddRt.offsetMin = Vector2.zero;
            ddRt.offsetMax = new Vector2(-10, 0);

            // Caption
            var capGO = CreateChild(ddGO.transform, "Label");
            var capTmp = capGO.AddComponent<TextMeshProUGUI>();
            FontManager.EnsureFont(capTmp);
            capTmp.fontSize = FONT_SIZE;
            capTmp.alignment = TextAlignmentOptions.MidlineLeft;
            capTmp.color = BoneWhite;
            var capRt = capGO.GetComponent<RectTransform>();
            capRt.anchorMin = Vector2.zero;
            capRt.anchorMax = Vector2.one;
            capRt.offsetMin = new Vector2(10, 0);
            capRt.offsetMax = new Vector2(-30, 0);
            dd.captionText = capTmp;

            CreateDropdownTemplate(dd);
            allSelectables.Add(dd);
            return dd;
        }

        private Button CreateToggleRow(Transform parent, string label, out TMP_Text valueText, ref float y)
        {
            var (rowRt, _) = CreateRowBase(parent, label, ref y);

            var btnGO = CreateChild(rowRt.transform, "Toggle");
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = Color.white;

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            var colors = btn.colors;
            colors.normalColor = BtnNormal;
            colors.highlightedColor = BtnSelected;
            colors.pressedColor = BtnPress;
            colors.selectedColor = BtnSelected;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;

            if (btn.GetComponent<SoulsButtonTextHighlight>() == null)
                btn.gameObject.AddComponent<SoulsButtonTextHighlight>();

            var btnRt = btnGO.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.42f, 0.1f);
            btnRt.anchorMax = new Vector2(0.65f, 0.9f);
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;

            var txtGO = CreateChild(btnGO.transform, "Text");
            valueText = txtGO.AddComponent<TextMeshProUGUI>();
            FontManager.EnsureFont(valueText);
            valueText.fontSize = FONT_SIZE;
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.color = BoneWhite;
            var txtRt = txtGO.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            allSelectables.Add(btn);
            return btn;
        }

        private Slider CreateSliderRow(Transform parent, string label, float min, float max, out TMP_Text valueText, ref float y)
        {
            var (rowRt, _) = CreateRowBase(parent, label, ref y);

            var sliderGO = CreateChild(rowRt.transform, "Slider");
            var slider = sliderGO.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = max;

            var sliderRt = sliderGO.GetComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(0.42f, 0.25f);
            sliderRt.anchorMax = new Vector2(0.85f, 0.75f);
            sliderRt.offsetMin = Vector2.zero;
            sliderRt.offsetMax = Vector2.zero;

            // Background
            var bgGO = CreateChild(sliderGO.transform, "Background");
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = BtnNormal;
            var bgRt = bgGO.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // Fill
            var fillArea = CreateChild(sliderGO.transform, "Fill Area");
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.offsetMin = Vector2.zero;
            fillAreaRt.offsetMax = Vector2.zero;

            var fill = CreateChild(fillArea.transform, "Fill");
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = BtnSelected;
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            slider.fillRect = fillRt;

            // Handle
            var handleArea = CreateChild(sliderGO.transform, "Handle Slide Area");
            var handleAreaRt = handleArea.GetComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = Vector2.zero;
            handleAreaRt.offsetMax = Vector2.zero;

            var handle = CreateChild(handleArea.transform, "Handle");
            var handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;
            var handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(20, 0);

            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;

            var sc = slider.colors;
            sc.normalColor = AgedGold;
            sc.highlightedColor = new Color(1f, 0.95f, 0.5f, 1f);
            sc.pressedColor = AgedGold;
            sc.selectedColor = new Color(1f, 0.95f, 0.5f, 1f);
            sc.fadeDuration = 0.1f;
            slider.colors = sc;

            // Value text
            var valGO = CreateChild(rowRt.transform, "ValueText");
            valueText = valGO.AddComponent<TextMeshProUGUI>();
            FontManager.EnsureFont(valueText);
            valueText.fontSize = FONT_SIZE;
            valueText.alignment = TextAlignmentOptions.MidlineRight;
            valueText.color = BoneWhite;
            var valRt = valGO.GetComponent<RectTransform>();
            valRt.anchorMin = new Vector2(0.87f, 0);
            valRt.anchorMax = new Vector2(1f, 1);
            valRt.offsetMin = Vector2.zero;
            valRt.offsetMax = new Vector2(-10, 0);

            allSelectables.Add(slider);
            return slider;
        }

        private Button CreateActionButton(Transform parent, string label, float xOffset, ref float y)
        {
            var go = CreateChild(parent, label + "Button");
            var img = go.AddComponent<Image>();
            img.color = Color.white;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = BtnNormal;
            colors.highlightedColor = BtnSelected;
            colors.pressedColor = BtnPress;
            colors.selectedColor = BtnSelected;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;

            if (btn.GetComponent<SoulsButtonTextHighlight>() == null)
                btn.gameObject.AddComponent<SoulsButtonTextHighlight>();

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(xOffset, y);
            rt.sizeDelta = new Vector2(200, 45);

            var txtGO = CreateChild(go.transform, "Text");
            var tmp = txtGO.AddComponent<TextMeshProUGUI>();
            FontManager.EnsureFont(tmp);
            tmp.text = label;
            tmp.fontSize = FONT_SIZE;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = BoneWhite;
            var txtRt = txtGO.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            y -= 55f;
            allSelectables.Add(btn);
            return btn;
        }

        private void CreateDropdownTemplate(TMP_Dropdown dropdown)
        {
            var template = CreateChild(dropdown.transform, "Template");
            var templateImg = template.AddComponent<Image>();
            templateImg.color = new Color(0.12f, 0.12f, 0.18f, 1f);
            var templateRt = template.GetComponent<RectTransform>();
            templateRt.anchorMin = new Vector2(0, 0);
            templateRt.anchorMax = new Vector2(1, 0);
            templateRt.pivot = new Vector2(0.5f, 1);
            templateRt.anchoredPosition = Vector2.zero;
            templateRt.sizeDelta = new Vector2(0, 150);

            var scrollRect = template.AddComponent<ScrollRect>();

            var viewport = CreateChild(template.transform, "Viewport");
            viewport.AddComponent<RectMask2D>();
            var vpImg = viewport.AddComponent<Image>();
            vpImg.color = new Color(0.12f, 0.12f, 0.18f, 1f);
            var vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;

            var content = CreateChild(viewport.transform, "Content");
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0, 28);

            var item = CreateChild(content.transform, "Item");
            var itemBg = item.AddComponent<Image>();
            itemBg.color = new Color(0.15f, 0.15f, 0.22f, 1f);
            var itemToggle = item.AddComponent<Toggle>();
            itemToggle.targetGraphic = itemBg;
            var tc = itemToggle.colors;
            tc.normalColor = new Color(0.15f, 0.15f, 0.22f, 1f);
            tc.highlightedColor = BtnSelected;
            tc.selectedColor = BtnSelected;
            itemToggle.colors = tc;
            var itemRt = item.GetComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0, 0.5f);
            itemRt.anchorMax = new Vector2(1, 0.5f);
            itemRt.pivot = new Vector2(0.5f, 0.5f);
            itemRt.sizeDelta = new Vector2(0, 28);

            var itemLabel = CreateChild(item.transform, "Item Label");
            var itemTmp = itemLabel.AddComponent<TextMeshProUGUI>();
            FontManager.EnsureFont(itemTmp);
            itemTmp.fontSize = 18;
            itemTmp.alignment = TextAlignmentOptions.MidlineLeft;
            itemTmp.color = BoneWhite;
            var itemLabelRt = itemLabel.GetComponent<RectTransform>();
            itemLabelRt.anchorMin = Vector2.zero;
            itemLabelRt.anchorMax = Vector2.one;
            itemLabelRt.offsetMin = new Vector2(10, 0);
            itemLabelRt.offsetMax = Vector2.zero;

            scrollRect.content = contentRt;
            scrollRect.viewport = vpRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            dropdown.template = templateRt;
            dropdown.itemText = itemTmp;
            template.SetActive(false);
        }

        #endregion

        #region Callbacks

        private void SetupCallbacks()
        {
            aspectRatioDropdown.onValueChanged.AddListener(OnAspectRatioChanged);
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            fullscreenToggle.onClick.AddListener(OnFullscreenToggled);
            vsyncToggle.onClick.AddListener(OnVsyncToggled);
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
            soundSlider.onValueChanged.AddListener(OnSoundChanged);
            resetButton.onClick.AddListener(OnResetClicked);
            backButton.onClick.AddListener(OnBackClicked);
        }

        private void OnAspectRatioChanged(int index)
        {
            var ds = DisplaySettings.Instance;
            if (ds == null) return;
            ds.SetAspectRatio(index);

            // Refresh resolution dropdown with filtered list
            if (resolutionDropdown != null)
            {
                resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
                resolutionDropdown.ClearOptions();
                resolutionDropdown.AddOptions(
                    new List<string>(ds.GetResolutionStrings()));
                resolutionDropdown.SetValueWithoutNotify(ds.CurrentResolutionIndex);
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            }

            ds.ApplyAndSave();
        }

        private void OnResolutionChanged(int index)
        {
            var ds = DisplaySettings.Instance;
            if (ds == null) return;
            ds.SetResolution(index);
            ds.ApplyAndSave();
        }

        private void OnFullscreenToggled()
        {
            isFullscreen = !isFullscreen;
            fullscreenValueText.text = isFullscreen ? "On" : "Off";

            var ds = DisplaySettings.Instance;
            if (ds == null) return;
            ds.SetWindowMode(isFullscreen
                ? DisplaySettings.WindowMode.FullscreenWindowed
                : DisplaySettings.WindowMode.Windowed);
            ds.ApplyAndSave();
            UIManager.Instance?.PlaySelectSound();
        }

        private void OnVsyncToggled()
        {
            isVsync = !isVsync;
            vsyncValueText.text = isVsync ? "On" : "Off";
            QualitySettings.vSyncCount = isVsync ? 1 : 0;
            PlayerPrefs.SetInt("Graphics_VSync", isVsync ? 1 : 0);
            UIManager.Instance?.PlaySelectSound();
        }

        private void OnBrightnessChanged(float value)
        {
            GraphicsSettings.Instance?.SetBrightness(value);
            UpdateBrightnessText();
        }

        private void OnMusicChanged(float value)
        {
            PlayerPrefs.SetFloat("Audio_Music", value);
            MusicManager.Instance?.SetVolume(value);
            musicText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }

        private void OnSoundChanged(float value)
        {
            PlayerPrefs.SetFloat("Audio_SFX", value);
            soundText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }

        private void OnResetClicked()
        {
            DisplaySettings.Instance?.ResetToDefaults();
            GraphicsSettings.Instance?.ResetToDefaults();
            QualitySettings.vSyncCount = 0;
            PlayerPrefs.SetInt("Graphics_VSync", 0);
            PlayerPrefs.SetFloat("Audio_Music", 1f);
            PlayerPrefs.SetFloat("Audio_SFX", 1f);
            MusicManager.Instance?.SetVolume(1f);
            AudioListener.volume = 1f;
            RefreshAllValues();
            UIManager.Instance?.PlayConfirmSound();
        }

        private void OnBackClicked()
        {
            UIManager.Instance?.PlayCancelSound();
            var mainMenu = FindFirstObjectByType<MainMenuController>();
            if (mainMenu != null)
                mainMenu.ShowMainMenu();
            else
                gameObject.SetActive(false);
        }

        #endregion

        #region Refresh

        private void RefreshAllValues()
        {
            var ds = DisplaySettings.Instance;

            // Aspect Ratio
            if (ds != null && aspectRatioDropdown != null)
            {
                aspectRatioDropdown.ClearOptions();
                aspectRatioDropdown.AddOptions(
                    new List<string>(ds.GetAspectRatioStrings()));
                aspectRatioDropdown.SetValueWithoutNotify((int)ds.CurrentAspectRatio);
            }

            // Resolution (filtered by aspect ratio)
            if (ds != null && resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();
                resolutionDropdown.AddOptions(
                    new List<string>(ds.GetResolutionStrings()));
                resolutionDropdown.SetValueWithoutNotify(ds.CurrentResolutionIndex);
            }

            // Fullscreen
            isFullscreen = Screen.fullScreenMode != FullScreenMode.Windowed;
            if (fullscreenValueText != null)
                fullscreenValueText.text = isFullscreen ? "On" : "Off";

            // VSync
            isVsync = PlayerPrefs.GetInt("Graphics_VSync", QualitySettings.vSyncCount > 0 ? 1 : 0) > 0;
            QualitySettings.vSyncCount = isVsync ? 1 : 0;
            if (vsyncValueText != null)
                vsyncValueText.text = isVsync ? "On" : "Off";

            // Brightness
            var gs = GraphicsSettings.Instance;
            if (gs != null && brightnessSlider != null)
                brightnessSlider.SetValueWithoutNotify(gs.Brightness);
            UpdateBrightnessText();

            // Audio
            float music = PlayerPrefs.GetFloat("Audio_Music", 1f);
            float sound = PlayerPrefs.GetFloat("Audio_SFX", 1f);
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(music);
            if (soundSlider != null) soundSlider.SetValueWithoutNotify(sound);
            if (musicText != null) musicText.text = $"{Mathf.RoundToInt(music * 100)}%";
            if (soundText != null) soundText.text = $"{Mathf.RoundToInt(sound * 100)}%";

            // Sync audio systems
            MusicManager.Instance?.SetVolume(music);
        }

        private void UpdateBrightnessText()
        {
            if (brightnessText != null && brightnessSlider != null)
            {
                int pct = Mathf.RoundToInt(brightnessSlider.value * 100f);
                brightnessText.text = pct > 0 ? $"+{pct}" : pct.ToString();
            }
        }

        #endregion

        #region Navigation

        private void WireNavigation()
        {
            for (int i = 0; i < allSelectables.Count; i++)
            {
                var sel = allSelectables[i];
                var nav = new Navigation { mode = Navigation.Mode.Explicit };

                nav.selectOnUp = i > 0 ? allSelectables[i - 1] : allSelectables[allSelectables.Count - 1];
                nav.selectOnDown = i < allSelectables.Count - 1 ? allSelectables[i + 1] : allSelectables[0];

                // Left/right for non-sliders mirrors up/down
                if (!(sel is Slider))
                {
                    nav.selectOnLeft = nav.selectOnUp;
                    nav.selectOnRight = nav.selectOnDown;
                }

                sel.navigation = nav;
            }
        }

        #endregion

        #region Helpers

        private void EnsureDisplaySettings()
        {
            if (DisplaySettings.Instance == null)
            {
                var go = new GameObject("DisplaySettings");
                go.AddComponent<DisplaySettings>();
            }
        }

        private void EnsureGraphicsSettings()
        {
            if (GraphicsSettings.Instance == null)
            {
                var go = new GameObject("GraphicsSettings");
                go.AddComponent<GraphicsSettings>();
            }
        }

        #endregion

        private void OnDestroy()
        {
            if (aspectRatioDropdown != null) aspectRatioDropdown.onValueChanged.RemoveAllListeners();
            if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveAllListeners();
            if (fullscreenToggle != null) fullscreenToggle.onClick.RemoveAllListeners();
            if (vsyncToggle != null) vsyncToggle.onClick.RemoveAllListeners();
            if (brightnessSlider != null) brightnessSlider.onValueChanged.RemoveAllListeners();
            if (musicSlider != null) musicSlider.onValueChanged.RemoveAllListeners();
            if (soundSlider != null) soundSlider.onValueChanged.RemoveAllListeners();
            if (resetButton != null) resetButton.onClick.RemoveAllListeners();
            if (backButton != null) backButton.onClick.RemoveAllListeners();
        }
    }
}
