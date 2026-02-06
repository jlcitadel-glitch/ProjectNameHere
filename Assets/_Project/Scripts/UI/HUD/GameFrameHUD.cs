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

        [Header("Style")]
        [SerializeField] private UIStyleGuide styleGuide;
        [SerializeField] private GothicFrameStyle frameStyle;

        [Header("Auto-Wire Settings")]
        [SerializeField] private bool autoFindPlayer = true;
        [SerializeField] private bool autoWireComponents = true;

        private HealthSystem healthSystem;
        private ManaSystem manaSystem;
        private LevelSystem levelSystem;

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

            healthSystem = player.GetComponent<HealthSystem>();
            manaSystem = player.GetComponent<ManaSystem>();
            levelSystem = player.GetComponent<LevelSystem>();

            SubscribeToEvents();
            InitializeValues();
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
        private static readonly Color FrameColor = new Color(0.812f, 0.710f, 0.231f, 0.3f);
        private static readonly Color AgedGold = new Color(0.812f, 0.710f, 0.231f, 1f);
        private static readonly Color DeepCrimson = new Color(0.545f, 0f, 0f, 1f);
        private static readonly Color DarkBlue = new Color(0.1f, 0.2f, 0.6f, 1f);
        private static readonly Color SpectralCyan = new Color(0f, 0.808f, 0.820f, 1f);
        private static readonly Color Charcoal = new Color(0.102f, 0.102f, 0.102f, 0.9f);
        private static readonly Color BlackBg = new Color(0f, 0f, 0f, 0.85f);
        private static readonly Color BoneWhite = new Color(0.961f, 0.961f, 0.863f, 1f);
        private static readonly Color ExpBg = new Color(1f, 1f, 1f, 0.15f);

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

            hud.autoWireComponents = false;

            Debug.Log("[GameFrameHUD] Runtime UI created (bottom bar layout).");
            return hud;
        }

        private static void BuildBottomBar(GameFrameHUD hud, Transform parent)
        {
            // Single-row bottom bar: Level | HP | Mana | EXP
            var barGo = MakeRect("BottomBar", parent);
            var barRect = barGo.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 0);
            barRect.anchorMax = new Vector2(1, 0);
            barRect.pivot = new Vector2(0.5f, 0);
            barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(0, 48);
            hud.bottomBar = barRect;

            // Frame background
            var frameBg = MakeRect("FrameBorder", barGo.transform);
            Stretch(frameBg);
            var frameImg = frameBg.AddComponent<Image>();
            frameImg.color = FrameColor;
            hud.bottomFrame = frameImg;

            // Single stats row filling the bar
            var row = MakeRect("StatsRow", barGo.transform);
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = Vector2.zero;
            rowRect.anchorMax = Vector2.one;
            rowRect.offsetMin = new Vector2(8, 4);
            rowRect.offsetMax = new Vector2(-8, -4);

            // --- Level label with black background (left side) ---
            var levelGo = MakeRect("LevelDisplay", row.transform);
            var levelComp = levelGo.AddComponent<LevelDisplay>();
            var levelRect = levelGo.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0, 0);
            levelRect.anchorMax = new Vector2(0, 1);
            levelRect.pivot = new Vector2(0, 0.5f);
            levelRect.anchoredPosition = Vector2.zero;
            levelRect.sizeDelta = new Vector2(70, 0);
            hud.levelDisplay = levelComp;

            // Black background behind level text
            var levelBg = MakeRect("LevelBg", levelGo.transform);
            Stretch(levelBg);
            var levelBgImg = levelBg.AddComponent<Image>();
            levelBgImg.sprite = WhiteSprite;
            levelBgImg.color = BlackBg;

            var levelTextGo = MakeRect("LevelText", levelGo.transform);
            Stretch(levelTextGo);
            var levelTmp = levelTextGo.AddComponent<TextMeshProUGUI>();
            levelTmp.text = "Lv. 1";
            levelTmp.fontSize = 18;
            levelTmp.alignment = TextAlignmentOptions.Center;
            levelTmp.color = AgedGold;
            FontManager.EnsureFont(levelTmp);
            levelComp.SetReferences(levelTmp);

            // --- HP bar ---
            hud.healthBar = BuildHBar(row.transform, "HealthBarGroup",
                0.08f, 0.19f, DeepCrimson, Charcoal, "HP");

            // --- Mana bar (darker blue) ---
            hud.manaBar = BuildHBar(row.transform, "ManaBarGroup",
                0.21f, 0.32f, DarkBlue, Charcoal, "MP");

            // --- EXP bar (spectral cyan, same size as HP/Mana) ---
            var expGo = MakeRect("ExpBarGroup", row.transform);
            var expComp = expGo.AddComponent<ExpBarDisplay>();
            var expRect = expGo.GetComponent<RectTransform>();
            expRect.anchorMin = new Vector2(0.34f, 0);
            expRect.anchorMax = new Vector2(0.45f, 1);
            expRect.offsetMin = new Vector2(2, 4);
            expRect.offsetMax = new Vector2(-2, -4);
            hud.expBar = expComp;

            // EXP Background
            var expBg = MakeRect("Background", expGo.transform);
            Stretch(expBg);
            var expBgImg = expBg.AddComponent<Image>();
            expBgImg.sprite = WhiteSprite;
            expBgImg.color = ExpBg;

            // EXP Fill — spectral cyan, starts empty
            var expFill = MakeRect("Fill", expGo.transform);
            var expFillRect = expFill.GetComponent<RectTransform>();
            expFillRect.anchorMin = Vector2.zero;
            expFillRect.anchorMax = Vector2.one;
            expFillRect.offsetMin = new Vector2(1, 1);
            expFillRect.offsetMax = new Vector2(-1, -1);
            var expFillImg = expFill.AddComponent<Image>();
            expFillImg.sprite = WhiteSprite;
            expFillImg.type = Image.Type.Filled;
            expFillImg.fillMethod = Image.FillMethod.Horizontal;
            expFillImg.fillAmount = 0f;
            expFillImg.color = SpectralCyan;

            // EXP Label
            var expLabel = MakeRect("Label", expGo.transform);
            Stretch(expLabel);
            var expLabelRect = expLabel.GetComponent<RectTransform>();
            expLabelRect.offsetMin = new Vector2(4, 0);
            expLabelRect.offsetMax = new Vector2(-4, 0);
            var expLabelTmp = expLabel.AddComponent<TextMeshProUGUI>();
            expLabelTmp.text = "0/0 XP";
            expLabelTmp.fontSize = 11;
            expLabelTmp.alignment = TextAlignmentOptions.Center;
            expLabelTmp.color = BoneWhite;
            FontManager.EnsureFont(expLabelTmp);

            expComp.SetReferences(expFillImg, expBgImg, expLabelTmp);
            expComp.SetProgressImmediate(0f);
        }

        /// <summary>
        /// Builds an anchored horizontal resource bar within a row.
        /// </summary>
        private static ResourceBarDisplay BuildHBar(Transform parent, string name,
            float anchorMinX, float anchorMaxX, Color fillColor, Color bgColor, string labelPrefix)
        {
            var barGo = MakeRect(name, parent);
            var barComp = barGo.AddComponent<ResourceBarDisplay>();
            var barRect = barGo.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(anchorMinX, 0);
            barRect.anchorMax = new Vector2(anchorMaxX, 1);
            barRect.offsetMin = new Vector2(2, 4);
            barRect.offsetMax = new Vector2(-2, -4);

            // Background
            var bg = MakeRect("Background", barGo.transform);
            Stretch(bg);
            var bgImg = bg.AddComponent<Image>();
            bgImg.sprite = WhiteSprite;
            bgImg.color = bgColor;

            // Fill
            var fill = MakeRect("Fill", barGo.transform);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(1, 1);
            fillRect.offsetMax = new Vector2(-1, -1);
            var fillImg = fill.AddComponent<Image>();
            fillImg.sprite = WhiteSprite;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;
            fillImg.color = fillColor;

            // Label
            var label = MakeRect("Label", barGo.transform);
            Stretch(label);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.offsetMin = new Vector2(4, 0);
            labelRect.offsetMax = new Vector2(-4, 0);
            var labelTmp = label.AddComponent<TextMeshProUGUI>();
            labelTmp.text = $"{labelPrefix} 100/100";
            labelTmp.fontSize = 11;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.color = BoneWhite;
            FontManager.EnsureFont(labelTmp);

            barComp.SetReferences(fillImg, bgImg, labelTmp);
            barComp.ConfigureForRuntime(fillColor, bgColor);
            return barComp;
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
