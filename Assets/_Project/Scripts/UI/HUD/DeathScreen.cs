using System;
using UnityEngine;
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
        [SerializeField] private Button respawnButton;
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
        [SerializeField] private AudioClip respawnSound;

        private HealthSystem healthSystem;
        private AudioSource audioSource;
        private float fadeTimer;
        private float textFadeTimer;
        private bool isShowing;
        private bool textVisible;

        public event Action OnRespawnRequested;
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
            if (respawnButton != null)
            {
                respawnButton.onClick.AddListener(HandleRespawn);
            }

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
            if (respawnButton != null)
            {
                respawnButton.gameObject.SetActive(visible);
            }

            if (quitButton != null)
            {
                quitButton.gameObject.SetActive(visible);
            }
        }

        private void HandleRespawn()
        {
            PlaySound(respawnSound);
            Hide();
            OnRespawnRequested?.Invoke();

            // Revive player
            if (healthSystem != null)
            {
                healthSystem.Revive();
            }

            // Resume game
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartPlaying();
            }
        }

        private void HandleQuit()
        {
            OnQuitRequested?.Invoke();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMainMenu();
            }
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
    }
}
