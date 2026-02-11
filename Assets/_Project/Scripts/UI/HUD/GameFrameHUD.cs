using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Main container that manages the fixed HUD frame layout.
    /// All stats (HP/MP/Level/XP) live in a single bottom bar.
    /// Uses GothicFrameStyle for consistent borders.
    /// </summary>
    public class GameFrameHUD : MonoBehaviour
    {
        [Header("Frame References")]
        [SerializeField] private RectTransform bottomBar;

        [Header("Bottom Bar Components")]
        [SerializeField] private LevelDisplay levelDisplay;
        [SerializeField] private ResourceBarDisplay healthBar;
        [SerializeField] private ResourceBarDisplay manaBar;
        [SerializeField] private ExpBarDisplay expBar;
        [SerializeField] private SkillHotbar skillHotbar;
        [SerializeField] private Image bottomFrame;

        [Header("Wave Tracker")]
        [SerializeField] private TMP_Text waveText;

        [Header("Style")]
        [SerializeField] private UIStyleGuide styleGuide;
        [SerializeField] private GothicFrameStyle frameStyle;

        [Header("Auto-Wire Settings")]
        [SerializeField] private bool autoFindPlayer = true;
        [SerializeField] private bool autoWireComponents = true;

        private HealthSystem healthSystem;
        private ManaSystem manaSystem;
        private LevelSystem levelSystem;
        private WaveManager waveManager;

        private void Start()
        {
            InitializeStyle();

            if (autoFindPlayer)
            {
                FindPlayerSystems();
            }

            if (autoWireComponents)
            {
                WireComponents();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeStyle()
        {
            if (styleGuide == null)
            {
                styleGuide = UIManager.Instance?.StyleGuide;
            }

            ApplyFrameStyle();
        }

        private void ApplyFrameStyle()
        {
            if (frameStyle == null)
                return;

            if (bottomFrame != null)
            {
                bottomFrame.sprite = frameStyle.frameSprite;
                bottomFrame.color = frameStyle.frameColor;
                bottomFrame.type = Image.Type.Sliced;
            }
        }

        private void FindPlayerSystems()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[GameFrameHUD] Player not found. HUD will not update.");
                return;
            }

            var newHealthSystem = player.GetComponent<HealthSystem>();

            // Already subscribed to this exact instance — skip
            if (newHealthSystem != null && newHealthSystem == healthSystem)
                return;

            // Unsubscribe from old references before re-binding
            UnsubscribeFromEvents();

            healthSystem = newHealthSystem;
            manaSystem = player.GetComponent<ManaSystem>();
            levelSystem = player.GetComponent<LevelSystem>();

            // Wave manager lives on its own GameObject, not on the player
            waveManager = FindAnyObjectByType<WaveManager>();

            SubscribeToEvents();
            InitializeValues();
        }

        /// <summary>
        /// Re-finds player systems after scene transitions.
        /// Called by UIManager when a new gameplay scene loads.
        /// </summary>
        public void RebindPlayerSystems()
        {
            FindPlayerSystems();
        }

        private void SubscribeToEvents()
        {
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged += HandleHealthChanged;
            }

            if (manaSystem != null)
            {
                manaSystem.OnManaChanged += HandleManaChanged;
            }

            if (waveManager != null)
            {
                waveManager.OnWaveStarted += HandleWaveStarted;
                waveManager.OnWaveCleared += HandleWaveCleared;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged -= HandleHealthChanged;
            }

            if (manaSystem != null)
            {
                manaSystem.OnManaChanged -= HandleManaChanged;
            }

            if (waveManager != null)
            {
                waveManager.OnWaveStarted -= HandleWaveStarted;
                waveManager.OnWaveCleared -= HandleWaveCleared;
            }
        }

        private void InitializeValues()
        {
            if (healthSystem != null && healthBar != null)
            {
                healthBar.SetValueImmediate(healthSystem.CurrentHealth, healthSystem.MaxHealth);
            }

            if (manaSystem != null && manaBar != null)
            {
                manaBar.SetValueImmediate(manaSystem.CurrentMana, manaSystem.MaxMana);
            }

            if (levelSystem != null && levelDisplay != null)
            {
                levelDisplay.SetLevelSystem(levelSystem);
            }

            if (levelSystem != null && expBar != null)
            {
                expBar.SetLevelSystem(levelSystem);
            }

            RefreshWaveDisplay();
        }

        private void WireComponents()
        {
            // All components live in the bottom bar
            if (bottomBar == null) return;

            if (levelDisplay == null)
                levelDisplay = bottomBar.GetComponentInChildren<LevelDisplay>();

            if (healthBar == null || manaBar == null)
            {
                var bars = bottomBar.GetComponentsInChildren<ResourceBarDisplay>();
                foreach (var bar in bars)
                {
                    if (healthBar == null && bar.name.Contains("Health"))
                        healthBar = bar;
                    else if (manaBar == null && bar.name.Contains("Mana"))
                        manaBar = bar;
                }
            }

            if (expBar == null)
                expBar = bottomBar.GetComponentInChildren<ExpBarDisplay>();

            if (skillHotbar == null)
                skillHotbar = bottomBar.GetComponentInChildren<SkillHotbar>();
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (healthBar != null)
            {
                healthBar.SetValue(current, max);
            }
        }

        private void HandleManaChanged(float current, float max)
        {
            if (manaBar != null)
            {
                manaBar.SetValue(current, max);
            }
        }

        private void HandleWaveStarted(int wave)
        {
            RefreshWaveDisplay();
        }

        private void HandleWaveCleared(int wave)
        {
            RefreshWaveDisplay();
        }

        private void RefreshWaveDisplay()
        {
            if (waveText == null) return;

            int wave = waveManager != null && waveManager.CurrentWave > 0 ? waveManager.CurrentWave : 1;
            waveText.text = $"Wave {wave}";
        }

        /// <summary>
        /// Manually connects to player systems.
        /// </summary>
        public void SetPlayerSystems(HealthSystem health, ManaSystem mana, LevelSystem level)
        {
            UnsubscribeFromEvents();

            healthSystem = health;
            manaSystem = mana;
            levelSystem = level;

            SubscribeToEvents();
            InitializeValues();
        }

        /// <summary>
        /// Shows or hides the bottom bar.
        /// </summary>
        public void SetBottomBarVisible(bool visible)
        {
            if (bottomBar != null)
            {
                bottomBar.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Shows or hides the entire HUD frame.
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        /// <summary>
        /// Gets the health bar component.
        /// </summary>
        public ResourceBarDisplay GetHealthBar() => healthBar;

        /// <summary>
        /// Gets the mana bar component.
        /// </summary>
        public ResourceBarDisplay GetManaBar() => manaBar;

        /// <summary>
        /// Gets the level display component.
        /// </summary>
        public LevelDisplay GetLevelDisplay() => levelDisplay;

        /// <summary>
        /// Gets the exp bar component.
        /// </summary>
        public ExpBarDisplay GetExpBar() => expBar;

        /// <summary>
        /// Gets the skill hotbar component.
        /// </summary>
        public SkillHotbar GetSkillHotbar() => skillHotbar;

        #region Runtime UI Builder

        // Gothic palette for runtime-built HUD
        private static readonly Color AgedGold = new Color(0.812f, 0.710f, 0.231f, 1f);
        private static readonly Color DeepCrimson = new Color(0.545f, 0f, 0f, 1f);
        private static readonly Color BlackBg = new Color(0f, 0f, 0f, 0.85f);
        private static readonly Color BoneWhite = new Color(0.961f, 0.961f, 0.863f, 1f);

        // MapleStory-style panel colors
        private static readonly Color PanelGrey = new Color(0.165f, 0.165f, 0.165f, 1f);
        private static readonly Color BorderGrey = new Color(0.29f, 0.29f, 0.29f, 1f);
        private static readonly Color RecessColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        private static readonly Color InnerBgColor = new Color(0.067f, 0.067f, 0.067f, 1f);
        private static readonly Color KeycapFace = new Color(0.176f, 0.176f, 0.176f, 1f);

        // Bar fill colors
        private static readonly Color MpBlue = new Color(0.1f, 0.227f, 0.502f, 1f);
        private static readonly Color ExpGold = new Color(0.812f, 0.710f, 0.231f, 1f);

        private static Sprite _whiteSprite;
        private static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite == null)
                {
                    var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
                }
                return _whiteSprite;
            }
        }

        /// <summary>
        /// Creates the full GameFrameHUD at runtime on the given canvas.
        /// All stats live in a single bottom bar.
        /// </summary>
        public static GameFrameHUD CreateRuntimeUI(Canvas hudCanvas)
        {
            if (hudCanvas == null) return null;

            // Check if one already exists with content
            var existing = hudCanvas.GetComponent<GameFrameHUD>();
            if (existing != null && existing.bottomBar != null)
                return existing;

            var hud = hudCanvas.gameObject.GetComponent<GameFrameHUD>();
            if (hud == null)
                hud = hudCanvas.gameObject.AddComponent<GameFrameHUD>();

            Transform parent = hudCanvas.transform.Find("SafeArea");
            if (parent == null)
                parent = hudCanvas.transform;

            // Remove old TopLeftGroup from previous wizard setup
            var oldTopLeft = parent.Find("TopLeftGroup");
            if (oldTopLeft != null)
                Destroy(oldTopLeft.gameObject);

            BuildBottomBar(hud, parent);
            BuildWaveTracker(hud, parent);

            hud.autoWireComponents = false;

            Debug.Log("[GameFrameHUD] Runtime UI created (bottom bar layout).");
            return hud;
        }

        private static void BuildWaveTracker(GameFrameHUD hud, Transform parent)
        {
            // Container anchored to top center
            var waveGo = MakeRect("WaveTracker", parent);
            var waveRect = waveGo.GetComponent<RectTransform>();
            waveRect.anchorMin = new Vector2(0.5f, 1f);
            waveRect.anchorMax = new Vector2(0.5f, 1f);
            waveRect.pivot = new Vector2(0.5f, 1f);
            waveRect.anchoredPosition = new Vector2(0, -8f);
            waveRect.sizeDelta = new Vector2(200f, 36f);

            // Background
            var waveBg = MakeRect("Background", waveGo.transform);
            Stretch(waveBg);
            var waveBgImg = waveBg.AddComponent<Image>();
            waveBgImg.sprite = WhiteSprite;
            waveBgImg.color = BlackBg;

            // Text
            var waveTextGo = MakeRect("WaveText", waveGo.transform);
            Stretch(waveTextGo);
            var waveTmp = waveTextGo.AddComponent<TextMeshProUGUI>();
            waveTmp.text = "Wave 1";
            waveTmp.fontSize = 20;
            waveTmp.alignment = TextAlignmentOptions.Center;
            waveTmp.color = AgedGold;
            waveTmp.fontStyle = FontStyles.Bold;
            FontManager.EnsureFont(waveTmp);

            hud.waveText = waveTmp;
        }

        private static void BuildBottomBar(GameFrameHUD hud, Transform parent)
        {
            // MapleStory-style bottom bar: 80px tall, solid dark grey panel
            var barGo = MakeRect("BottomBar", parent);
            var barRect = barGo.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 0);
            barRect.anchorMax = new Vector2(1, 0);
            barRect.pivot = new Vector2(0.5f, 0);
            barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(0, 80);
            hud.bottomBar = barRect;

            // Solid dark grey panel background
            var frameBg = MakeRect("PanelBackground", barGo.transform);
            Stretch(frameBg);
            var frameImg = frameBg.AddComponent<Image>();
            frameImg.sprite = WhiteSprite;
            frameImg.color = PanelGrey;
            hud.bottomFrame = frameImg;

            // Subtle top border (2px line for clean separation)
            var topBorder = MakeRect("TopBorder", barGo.transform);
            var topBorderRect = topBorder.GetComponent<RectTransform>();
            topBorderRect.anchorMin = new Vector2(0, 1);
            topBorderRect.anchorMax = new Vector2(1, 1);
            topBorderRect.pivot = new Vector2(0.5f, 1);
            topBorderRect.anchoredPosition = Vector2.zero;
            topBorderRect.sizeDelta = new Vector2(0, 2);
            var topBorderImg = topBorder.AddComponent<Image>();
            topBorderImg.sprite = WhiteSprite;
            topBorderImg.color = BorderGrey;

            // Stats row with padding (12px sides, 10px top/bottom)
            var row = MakeRect("StatsRow", barGo.transform);
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = Vector2.zero;
            rowRect.anchorMax = Vector2.one;
            rowRect.offsetMin = new Vector2(12, 10);
            rowRect.offsetMax = new Vector2(-12, -10);

            // --- Level display (left, 70px wide, recessed) ---
            var levelGo = MakeRect("LevelDisplay", row.transform);
            var levelComp = levelGo.AddComponent<LevelDisplay>();
            var levelRect = levelGo.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0, 0);
            levelRect.anchorMax = new Vector2(0, 1);
            levelRect.pivot = new Vector2(0, 0.5f);
            levelRect.anchoredPosition = Vector2.zero;
            levelRect.sizeDelta = new Vector2(70, 0);
            hud.levelDisplay = levelComp;

            // Level recess frame
            var levelRecess = MakeRect("RecessFrame", levelGo.transform);
            Stretch(levelRecess);
            var levelRecessImg = levelRecess.AddComponent<Image>();
            levelRecessImg.sprite = WhiteSprite;
            levelRecessImg.color = RecessColor;

            // Level inner background
            var levelBg = MakeRect("LevelBg", levelRecess.transform);
            var levelBgRect = levelBg.GetComponent<RectTransform>();
            levelBgRect.anchorMin = Vector2.zero;
            levelBgRect.anchorMax = Vector2.one;
            levelBgRect.offsetMin = new Vector2(2, 2);
            levelBgRect.offsetMax = new Vector2(-2, -2);
            var levelBgImg = levelBg.AddComponent<Image>();
            levelBgImg.sprite = WhiteSprite;
            levelBgImg.color = InnerBgColor;

            var levelTextGo = MakeRect("LevelText", levelRecess.transform);
            var levelTextRect = levelTextGo.GetComponent<RectTransform>();
            levelTextRect.anchorMin = Vector2.zero;
            levelTextRect.anchorMax = Vector2.one;
            levelTextRect.offsetMin = new Vector2(2, 2);
            levelTextRect.offsetMax = new Vector2(-2, -2);
            var levelTmp = levelTextGo.AddComponent<TextMeshProUGUI>();
            levelTmp.text = "Lv. 1";
            levelTmp.fontSize = 22;
            levelTmp.alignment = TextAlignmentOptions.Center;
            levelTmp.color = AgedGold;
            FontManager.EnsureFont(levelTmp);
            levelComp.SetReferences(levelTmp);

            // --- Three half-width recessed resource bars (left side) ---
            hud.healthBar = BuildHBar(row.transform, "HealthBarGroup",
                0.06f, 0.215f, DeepCrimson, "HP");

            hud.manaBar = BuildHBar(row.transform, "ManaBarGroup",
                0.22f, 0.375f, MpBlue, "MP");

            BuildExpBar(hud, row.transform, 0.38f, 0.535f);

            // --- Skill hotbar (right side) ---
            BuildSkillHotbar(hud, row.transform, 0.55f, 1.0f);
        }

        /// <summary>
        /// Builds a recessed horizontal resource bar within the stats row.
        /// </summary>
        private static ResourceBarDisplay BuildHBar(Transform parent, string name,
            float anchorMinX, float anchorMaxX, Color fillColor, string labelPrefix)
        {
            var barGo = MakeRect(name, parent);
            var barComp = barGo.AddComponent<ResourceBarDisplay>();
            var barRect = barGo.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(anchorMinX, 0);
            barRect.anchorMax = new Vector2(anchorMaxX, 1);
            barRect.offsetMin = new Vector2(4, 0);
            barRect.offsetMax = new Vector2(-4, 0);

            // Recess frame (dark outer border for sunken look)
            var recessGo = MakeRect("RecessFrame", barGo.transform);
            Stretch(recessGo);
            var recessImg = recessGo.AddComponent<Image>();
            recessImg.sprite = WhiteSprite;
            recessImg.color = RecessColor;

            // Inner background (near-black fill inside the recess)
            var bg = MakeRect("Background", recessGo.transform);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = new Vector2(2, 2);
            bgRect.offsetMax = new Vector2(-2, -2);
            var bgImg = bg.AddComponent<Image>();
            bgImg.sprite = WhiteSprite;
            bgImg.color = InnerBgColor;

            // Fill (colored bar inside the recess)
            var fill = MakeRect("Fill", recessGo.transform);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2, 2);
            fillRect.offsetMax = new Vector2(-2, -2);
            var fillImg = fill.AddComponent<Image>();
            fillImg.sprite = WhiteSprite;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;
            fillImg.color = fillColor;

            // Label (centered text on top)
            var label = MakeRect("Label", recessGo.transform);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(6, 2);
            labelRect.offsetMax = new Vector2(-6, -2);
            var labelTmp = label.AddComponent<TextMeshProUGUI>();
            labelTmp.text = $"{labelPrefix} 100/100";
            labelTmp.fontSize = 16;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.color = BoneWhite;
            FontManager.EnsureFont(labelTmp);

            barComp.SetReferences(fillImg, bgImg, labelTmp);
            barComp.ConfigureForRuntime(fillColor, InnerBgColor);
            barComp.SetLabelFormat($"{labelPrefix} {{0}}/{{1}}");
            return barComp;
        }

        /// <summary>
        /// Builds the EXP bar with recessed styling (uses ExpBarDisplay).
        /// </summary>
        private static void BuildExpBar(GameFrameHUD hud, Transform parent,
            float anchorMinX, float anchorMaxX)
        {
            var barGo = MakeRect("ExpBarGroup", parent);
            var expComp = barGo.AddComponent<ExpBarDisplay>();
            var barRect = barGo.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(anchorMinX, 0);
            barRect.anchorMax = new Vector2(anchorMaxX, 1);
            barRect.offsetMin = new Vector2(4, 0);
            barRect.offsetMax = new Vector2(-4, 0);
            hud.expBar = expComp;

            // Recess frame
            var recessGo = MakeRect("RecessFrame", barGo.transform);
            Stretch(recessGo);
            var recessImg = recessGo.AddComponent<Image>();
            recessImg.sprite = WhiteSprite;
            recessImg.color = RecessColor;

            // Inner background
            var bg = MakeRect("Background", recessGo.transform);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = new Vector2(2, 2);
            bgRect.offsetMax = new Vector2(-2, -2);
            var bgImg = bg.AddComponent<Image>();
            bgImg.sprite = WhiteSprite;
            bgImg.color = InnerBgColor;

            // Fill (gold/yellow)
            var fill = MakeRect("Fill", recessGo.transform);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2, 2);
            fillRect.offsetMax = new Vector2(-2, -2);
            var fillImg = fill.AddComponent<Image>();
            fillImg.sprite = WhiteSprite;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0f;
            fillImg.color = ExpGold;

            // Label
            var label = MakeRect("Label", recessGo.transform);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(6, 2);
            labelRect.offsetMax = new Vector2(-6, -2);
            var labelTmp = label.AddComponent<TextMeshProUGUI>();
            labelTmp.text = "0/0 XP";
            labelTmp.fontSize = 16;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.color = BoneWhite;
            FontManager.EnsureFont(labelTmp);

            expComp.SetReferences(fillImg, bgImg, labelTmp);
            expComp.SetProgressImmediate(0f);
        }

        /// <summary>
        /// Builds the skill hotbar with 6 keycap-style slots.
        /// </summary>
        private static void BuildSkillHotbar(GameFrameHUD hud, Transform parent,
            float anchorMinX, float anchorMaxX)
        {
            var containerGo = MakeRect("SkillHotbarGroup", parent);
            var containerRect = containerGo.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(anchorMinX, 0);
            containerRect.anchorMax = new Vector2(anchorMaxX, 1);
            containerRect.offsetMin = new Vector2(4, 0);
            containerRect.offsetMax = new Vector2(-4, 0);

            var layoutGroup = containerGo.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 6;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            var hotbarComp = containerGo.AddComponent<SkillHotbar>();
            string[] keybinds = { "1", "2", "3", "4", "5", "6" };
            var runtimeSlots = new SkillHotbar.HotbarSlot[6];

            for (int i = 0; i < 6; i++)
            {
                runtimeSlots[i] = BuildKeycapSlot(containerGo.transform, $"Slot{i + 1}", keybinds[i]);
            }

            hotbarComp.SetRuntimeSlots(runtimeSlots);
            hud.skillHotbar = hotbarComp;
        }

        /// <summary>
        /// Builds a single keycap-style skill slot.
        /// </summary>
        private static SkillHotbar.HotbarSlot BuildKeycapSlot(Transform parent, string name, string keybind)
        {
            var slot = new SkillHotbar.HotbarSlot();

            // Root slot object (56x56)
            var slotGo = MakeRect(name, parent);
            var slotRect = slotGo.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(56, 56);
            slot.slotRect = slotRect;

            var layoutElem = slotGo.AddComponent<LayoutElement>();
            layoutElem.preferredWidth = 56;
            layoutElem.preferredHeight = 56;

            var canvasGroup = slotGo.AddComponent<CanvasGroup>();
            slot.canvasGroup = canvasGroup;

            // Recess frame (sunken well, dark outer)
            var recessGo = MakeRect("RecessFrame", slotGo.transform);
            Stretch(recessGo);
            var recessImg = recessGo.AddComponent<Image>();
            recessImg.sprite = WhiteSprite;
            recessImg.color = new Color(0.102f, 0.102f, 0.102f, 1f); // #1A1A1A
            slot.frameImage = recessImg;

            // Keycap face (raised inner, 2px inset)
            var faceGo = MakeRect("KeycapFace", recessGo.transform);
            var faceRect = faceGo.GetComponent<RectTransform>();
            faceRect.anchorMin = Vector2.zero;
            faceRect.anchorMax = Vector2.one;
            faceRect.offsetMin = new Vector2(2, 2);
            faceRect.offsetMax = new Vector2(-2, -2);
            var faceImg = faceGo.AddComponent<Image>();
            faceImg.sprite = WhiteSprite;
            faceImg.color = KeycapFace;

            // Icon (dimmed placeholder)
            var iconGo = MakeRect("Icon", faceGo.transform);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(6, 6);
            iconRect.offsetMax = new Vector2(-6, -6);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = WhiteSprite;
            iconImg.color = new Color(1f, 1f, 1f, 0.15f);
            iconImg.enabled = false;
            slot.iconImage = iconImg;

            // "?" placeholder text (shown when no skill assigned)
            var placeholderGo = MakeRect("Placeholder", faceGo.transform);
            var placeholderRect = placeholderGo.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(2, 2);
            placeholderRect.offsetMax = new Vector2(-2, -2);
            var placeholderTmp = placeholderGo.AddComponent<TextMeshProUGUI>();
            placeholderTmp.text = "?";
            placeholderTmp.fontSize = 22;
            placeholderTmp.alignment = TextAlignmentOptions.Center;
            placeholderTmp.color = new Color(1f, 1f, 1f, 0.2f);
            FontManager.EnsureFont(placeholderTmp);

            // Cooldown overlay (Radial360, hidden initially)
            var cooldownGo = MakeRect("CooldownOverlay", faceGo.transform);
            var cooldownRect = cooldownGo.GetComponent<RectTransform>();
            cooldownRect.anchorMin = Vector2.zero;
            cooldownRect.anchorMax = Vector2.one;
            cooldownRect.offsetMin = new Vector2(2, 2);
            cooldownRect.offsetMax = new Vector2(-2, -2);
            var cooldownImg = cooldownGo.AddComponent<Image>();
            cooldownImg.sprite = WhiteSprite;
            cooldownImg.type = Image.Type.Filled;
            cooldownImg.fillMethod = Image.FillMethod.Radial360;
            cooldownImg.fillOrigin = (int)Image.Origin360.Top;
            cooldownImg.fillClockwise = true;
            cooldownImg.fillAmount = 0f;
            cooldownImg.color = new Color(0f, 0f, 0f, 0.6f);
            cooldownImg.raycastTarget = false;
            slot.cooldownOverlay = cooldownImg;

            // Cooldown text (hidden initially)
            var cdTextGo = MakeRect("CooldownText", faceGo.transform);
            var cdTextRect = cdTextGo.GetComponent<RectTransform>();
            cdTextRect.anchorMin = Vector2.zero;
            cdTextRect.anchorMax = Vector2.one;
            cdTextRect.offsetMin = new Vector2(2, 2);
            cdTextRect.offsetMax = new Vector2(-2, -2);
            var cdTmp = cdTextGo.AddComponent<TextMeshProUGUI>();
            cdTmp.text = "";
            cdTmp.fontSize = 16;
            cdTmp.alignment = TextAlignmentOptions.Center;
            cdTmp.color = BoneWhite;
            cdTmp.fontStyle = FontStyles.Bold;
            FontManager.EnsureFont(cdTmp);
            cdTextGo.SetActive(false);
            slot.cooldownText = cdTmp;

            // Key bind label (gold, bottom-right corner)
            var keyGo = MakeRect("KeyBind", recessGo.transform);
            var keyRect = keyGo.GetComponent<RectTransform>();
            keyRect.anchorMin = new Vector2(1, 0);
            keyRect.anchorMax = new Vector2(1, 0);
            keyRect.pivot = new Vector2(1, 0);
            keyRect.anchoredPosition = new Vector2(-2, 2);
            keyRect.sizeDelta = new Vector2(16, 14);
            var keyTmp = keyGo.AddComponent<TextMeshProUGUI>();
            keyTmp.text = keybind;
            keyTmp.fontSize = 11;
            keyTmp.alignment = TextAlignmentOptions.BottomRight;
            keyTmp.color = AgedGold;
            keyTmp.fontStyle = FontStyles.Bold;
            FontManager.EnsureFont(keyTmp);
            slot.keyBindText = keyTmp;

            return slot;
        }

        private static GameObject MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        #endregion
    }
}
