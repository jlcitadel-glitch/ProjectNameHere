using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Game over screen displayed when player dies.
    /// Subscribes to HealthSystem.OnDeath.
    /// </summary>
    public class DeathScreen : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private Button quitButton;
        [SerializeField] private Image backgroundOverlay;

        [Header("Style")]
        [SerializeField] private UIStyleGuide styleGuide;
        [SerializeField] private Color overlayColor = new Color(0.1f, 0f, 0f, 0.85f);
        [SerializeField] private string deathTitle = "YOU DIED";
        [SerializeField] private string deathSubtitle = "The darkness claims another soul...";

        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 1.5f;
        [SerializeField] private float fadeInDelay = 0.5f;
        [SerializeField] private float textFadeDelay = 1f;

        [Header("Audio")]
        [SerializeField] private AudioClip deathSound;

        private HealthSystem healthSystem;
        private AudioSource audioSource;
        private float fadeTimer;
        private float textFadeTimer;
        private bool isShowing;
        private bool textVisible;

        public event Action OnQuitRequested;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            FindHealthSystem();
            InitializeStyle();
            InitializeAudio();
            InitializeButtons();
            Hide();
        }

        private void OnDestroy()
        {
            if (healthSystem != null)
            {
                healthSystem.OnDeath -= HandleDeath;
            }
        }

        private void Update()
        {
            UpdateFadeIn();
        }

        private void FindHealthSystem()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                healthSystem = player.GetComponent<HealthSystem>();
                if (healthSystem != null)
                {
                    healthSystem.OnDeath += HandleDeath;
                    Debug.Log("[DeathScreen] Connected to HealthSystem");
                }
            }
        }

        private void InitializeStyle()
        {
            if (styleGuide == null && UIManager.Instance != null)
            {
                styleGuide = UIManager.Instance.StyleGuide;
            }

            if (styleGuide != null)
            {
                overlayColor = new Color(
                    styleGuide.bloodRed.r * 0.3f,
                    styleGuide.bloodRed.g * 0.1f,
                    styleGuide.bloodRed.b * 0.1f,
                    0.9f
                );

                if (titleText != null)
                {
                    titleText.color = styleGuide.bloodRed;
                }

                if (subtitleText != null)
                {
                    subtitleText.color = styleGuide.fadedParchment;
                }
            }

            if (backgroundOverlay != null)
            {
                backgroundOverlay.color = overlayColor;
            }

            if (titleText != null)
            {
                titleText.text = deathTitle;
            }

            if (subtitleText != null)
            {
                subtitleText.text = deathSubtitle;
            }
        }

        private void InitializeAudio()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }
        }

        private void InitializeButtons()
        {
            if (quitButton != null)
            {
                quitButton.onClick.AddListener(HandleQuit);
            }
        }

        private void HandleDeath()
        {
            Show();
        }

        private void Show()
        {
            isShowing = true;
            textVisible = false;
            fadeTimer = 0f;
            textFadeTimer = 0f;

            gameObject.SetActive(true);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            // Hide text initially
            SetTextAlpha(0f);
            SetButtonsVisible(false);

            PlaySound(deathSound);

            // Notify GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
        }

        private void Hide()
        {
            isShowing = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        private void UpdateFadeIn()
        {
            if (!isShowing)
                return;

            fadeTimer += Time.unscaledDeltaTime;

            // Delay before starting fade
            if (fadeTimer < fadeInDelay)
                return;

            // Fade in background
            float fadeProgress = (fadeTimer - fadeInDelay) / fadeInDuration;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Clamp01(fadeProgress);
            }

            // Fade in text after delay
            if (fadeTimer > fadeInDelay + textFadeDelay)
            {
                if (!textVisible)
                {
                    textVisible = true;
                    textFadeTimer = 0f;
                }

                textFadeTimer += Time.unscaledDeltaTime;
                float textAlpha = Mathf.Clamp01(textFadeTimer / 0.5f);
                SetTextAlpha(textAlpha);

                // Show buttons when text is visible
                if (textAlpha >= 1f)
                {
                    SetButtonsVisible(true);

                    if (canvasGroup != null)
                    {
                        canvasGroup.interactable = true;
                        canvasGroup.blocksRaycasts = true;
                    }
                }
            }
        }

        private void SetTextAlpha(float alpha)
        {
            if (titleText != null)
            {
                Color c = titleText.color;
                c.a = alpha;
                titleText.color = c;
            }

            if (subtitleText != null)
            {
                Color c = subtitleText.color;
                c.a = alpha;
                subtitleText.color = c;
            }
        }

        private void SetButtonsVisible(bool visible)
        {
            if (quitButton != null)
            {
                quitButton.gameObject.SetActive(visible);
            }
        }

        private void HandleQuit()
        {
            OnQuitRequested?.Invoke();

            Time.timeScale = 1f;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMainMenu();
            }

            // Load main menu scene (index 0 in build settings)
            SceneManager.LoadScene(0);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Manually sets the health system reference.
        /// </summary>
        public void SetHealthSystem(HealthSystem system)
        {
            if (healthSystem != null)
            {
                healthSystem.OnDeath -= HandleDeath;
            }

            healthSystem = system;

            if (healthSystem != null)
            {
                healthSystem.OnDeath += HandleDeath;
            }
        }

        #region Runtime UI Builder

        // Gothic color palette
        private static readonly Color PanelBg = new Color(0.08f, 0.02f, 0.02f, 0.9f);
        private static readonly Color BloodRed = new Color(0.8f, 0.1f, 0.1f, 1f);
        private static readonly Color FadedText = new Color(0.8f, 0.75f, 0.65f, 1f);
        private static readonly Color BtnNormal = new Color(0.15f, 0.05f, 0.05f, 1f);
        private static readonly Color BtnHover = new Color(0.25f, 0.08f, 0.08f, 1f);

        /// <summary>
        /// Builds the entire death screen UI at runtime. No scene setup required.
        /// Call from UIManager.EnsureDeathScreen() or similar.
        /// </summary>
        public static DeathScreen CreateRuntimeUI()
        {
            // --- Canvas ---
            var canvasGo = new GameObject("DeathScreen_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            var cg = canvasGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;

            // --- Background overlay (full screen dark red) ---
            var bgGo = MakeUIObject("Background", canvasGo.transform);
            Stretch(bgGo);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = PanelBg;

            // --- Centered content column ---
            var contentGo = MakeUIObject("Content", canvasGo.transform);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(600f, 400f);

            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 20f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // --- Title ---
            var titleGo = MakeUIObject("Title", contentGo.transform);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "YOU DIED";
            titleTmp.fontSize = 72;
            titleTmp.color = BloodRed;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.fontStyle = FontStyles.Bold;
            var titleLayout = titleGo.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 100f;

            // --- Subtitle ---
            var subGo = MakeUIObject("Subtitle", contentGo.transform);
            var subTmp = subGo.AddComponent<TextMeshProUGUI>();
            subTmp.text = "The darkness claims another soul...";
            subTmp.fontSize = 24;
            subTmp.color = FadedText;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.fontStyle = FontStyles.Italic;
            var subLayout = subGo.AddComponent<LayoutElement>();
            subLayout.preferredHeight = 40f;

            // --- Spacer ---
            var spacerGo = MakeUIObject("Spacer", contentGo.transform);
            var spacerLayout = spacerGo.AddComponent<LayoutElement>();
            spacerLayout.preferredHeight = 50f;

            // --- Buttons ---
            var quitBtn = MakeButton("Quit to Menu", contentGo.transform);

            // --- DeathScreen component (can access private fields from within the class) ---
            var ds = canvasGo.AddComponent<DeathScreen>();
            ds.canvasGroup = cg;
            ds.titleText = titleTmp;
            ds.subtitleText = subTmp;
            ds.quitButton = quitBtn;
            ds.backgroundOverlay = bgImg;

            return ds;
        }

        private static GameObject MakeUIObject(string name, Transform parent)
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
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private static Button MakeButton(string label, Transform parent)
        {
            var btnGo = MakeUIObject(label.Replace(" ", "") + "Button", parent);
            var btnLayout = btnGo.AddComponent<LayoutElement>();
            btnLayout.preferredHeight = 50f;
            btnLayout.preferredWidth = 300f;

            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = BtnNormal;

            var btn = btnGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = BtnNormal;
            colors.highlightedColor = BtnHover;
            colors.pressedColor = BloodRed;
            colors.selectedColor = BtnHover;
            btn.colors = colors;

            // Button text
            var textGo = MakeUIObject("Text", btnGo.transform);
            Stretch(textGo);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 28;
            tmp.color = FadedText;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;

            return btn;
        }

        #endregion
    }
}
