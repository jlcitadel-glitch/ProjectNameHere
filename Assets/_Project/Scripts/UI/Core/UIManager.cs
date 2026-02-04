using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ProjectName.UI
{
    /// <summary>
    /// Core UI management singleton.
    /// Manages canvas states, transitions, pausing, and UI input switching.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Style Configuration")]
        [SerializeField] private UIStyleGuide styleGuide;
        [SerializeField] private UISoundBank soundBank;

        [Header("Canvas References")]
        [Tooltip("Main menu canvas (Screen Space - Overlay)")]
        [SerializeField] private Canvas mainMenuCanvas;

        [Tooltip("In-game HUD canvas (Screen Space - Overlay for 2D)")]
        [SerializeField] private Canvas hudCanvas;

        [Tooltip("Pause menu canvas (Screen Space - Overlay)")]
        [SerializeField] private Canvas pauseCanvas;

        [Tooltip("World space canvas for damage numbers, etc")]
        [SerializeField] private Canvas worldCanvas;

        [Header("Canvas Groups")]
        [SerializeField] private CanvasGroup mainMenuGroup;
        [SerializeField] private CanvasGroup hudGroup;
        [SerializeField] private CanvasGroup pauseGroup;

        [Header("Audio")]
        [SerializeField] private AudioSource uiAudioSource;

        [Header("Input")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private string gameplayActionMap = "Player";
        [SerializeField] private string uiActionMap = "UI";

        [Header("Settings")]
        [SerializeField] private bool pauseTimeOnMenu = true;

        public UIStyleGuide StyleGuide => styleGuide;
        public UISoundBank SoundBank => soundBank;
        public bool IsPaused { get; private set; }
        public bool IsInMenu { get; private set; }

        public event Action OnPause;
        public event Action OnResume;
        public event Action<bool> OnMenuStateChanged;

        private Coroutine currentTransition;
        private float previousTimeScale;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeCanvases();
            InitializeAudio();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (currentTransition != null)
                StopCoroutine(currentTransition);
        }

        private void InitializeCanvases()
        {
            // Auto-find canvases if not assigned
            AutoFindCanvases();

            // Main menu - always on top
            if (mainMenuCanvas != null)
            {
                mainMenuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                mainMenuCanvas.sortingOrder = 100;
            }

            // HUD - Screen Space Overlay for 2D games
            if (hudCanvas != null)
            {
                hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                hudCanvas.sortingOrder = 10;
            }

            // Pause menu - overlay on top of everything
            if (pauseCanvas != null)
            {
                pauseCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                pauseCanvas.sortingOrder = 200;
                pauseCanvas.gameObject.SetActive(false);
            }

            // World UI - for in-world elements
            if (worldCanvas != null)
            {
                worldCanvas.renderMode = RenderMode.WorldSpace;
                worldCanvas.sortingOrder = 5;
            }
        }

        private void AutoFindCanvases()
        {
            // Find all canvases including inactive ones
            var allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();

            foreach (var canvas in allCanvases)
            {
                // Skip prefabs (only process scene objects)
                if (canvas.gameObject.scene.name == null) continue;

                if (hudCanvas == null && canvas.name == "HUD_Canvas")
                {
                    hudCanvas = canvas;
                    hudGroup = canvas.GetComponent<CanvasGroup>();
                    Debug.Log("[UIManager] Auto-found HUD_Canvas");
                }
                else if (pauseCanvas == null && canvas.name == "PauseMenu_Canvas")
                {
                    pauseCanvas = canvas;
                    pauseGroup = canvas.GetComponent<CanvasGroup>();
                    if (pauseGroup == null)
                    {
                        pauseGroup = canvas.gameObject.AddComponent<CanvasGroup>();
                        Debug.Log("[UIManager] Added missing CanvasGroup to PauseMenu_Canvas");
                    }
                    Debug.Log("[UIManager] Auto-found PauseMenu_Canvas");
                }
                else if (mainMenuCanvas == null && canvas.name == "MainMenu_Canvas")
                {
                    mainMenuCanvas = canvas;
                    mainMenuGroup = canvas.GetComponent<CanvasGroup>();
                    Debug.Log("[UIManager] Auto-found MainMenu_Canvas");
                }
            }

            // Warn if critical canvases are still missing
            if (pauseCanvas == null)
            {
                Debug.LogWarning("[UIManager] PauseMenu_Canvas not found. Pause menu will not work. Run Tools > ProjectName > UI Setup Wizard to create it.");
            }
        }

        private void InitializeAudio()
        {
            if (uiAudioSource == null)
            {
                uiAudioSource = gameObject.AddComponent<AudioSource>();
                uiAudioSource.playOnAwake = false;
                uiAudioSource.spatialBlend = 0f; // 2D sound
            }
        }

        /// <summary>
        /// Toggles pause state.
        /// </summary>
        public void TogglePause()
        {
            if (IsPaused)
                Resume();
            else
                Pause();
        }

        /// <summary>
        /// Pauses the game and shows pause menu.
        /// </summary>
        public void Pause()
        {
            if (IsPaused)
                return;

            IsPaused = true;
            IsInMenu = true;

            if (pauseTimeOnMenu)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            SwitchToUIInput();

            if (pauseCanvas != null && pauseGroup != null)
            {
                Debug.Log($"[UIManager] Showing pause canvas. Canvas: {pauseCanvas.name}, Group alpha: {pauseGroup.alpha}");
                ShowCanvas(pauseCanvas, pauseGroup);
            }
            else
            {
                Debug.LogWarning($"[UIManager] Cannot show pause menu - pauseCanvas: {(pauseCanvas != null ? "OK" : "NULL")}, pauseGroup: {(pauseGroup != null ? "OK" : "NULL")}");
            }

            PlaySound(soundBank?.pause);
            OnPause?.Invoke();
            OnMenuStateChanged?.Invoke(true);
        }

        /// <summary>
        /// Resumes the game and hides pause menu.
        /// </summary>
        public void Resume()
        {
            Debug.Log("[UIManager] Resume() called");

            if (!IsPaused)
            {
                Debug.Log("[UIManager] Not paused, skipping resume");
                return;
            }

            IsPaused = false;
            IsInMenu = false;

            if (pauseTimeOnMenu)
            {
                Time.timeScale = previousTimeScale;
                Debug.Log($"[UIManager] Time.timeScale restored to {previousTimeScale}");
            }

            SwitchToGameplayInput();

            if (pauseCanvas != null && pauseGroup != null)
            {
                HideCanvas(pauseCanvas, pauseGroup);
            }

            PlaySound(soundBank?.resume);
            OnResume?.Invoke();
            OnMenuStateChanged?.Invoke(false);
            Debug.Log("[UIManager] Game resumed");
        }

        /// <summary>
        /// Shows a canvas with fade-in animation.
        /// </summary>
        public void ShowCanvas(Canvas canvas, CanvasGroup group, Action onComplete = null)
        {
            if (canvas == null || group == null)
            {
                Debug.LogWarning("[UIManager] ShowCanvas called with null canvas or group");
                return;
            }

            if (currentTransition != null)
                StopCoroutine(currentTransition);

            canvas.gameObject.SetActive(true);
            Debug.Log($"[UIManager] Canvas {canvas.name} activated. Starting fade-in.");

            // Set initial state for fade
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            float duration = styleGuide != null ? styleGuide.transitionDuration : 0.3f;

            currentTransition = StartCoroutine(FadeCanvasGroup(group, 0f, 1f, duration, () =>
            {
                group.interactable = true;
                group.blocksRaycasts = true;
                Debug.Log($"[UIManager] Canvas {canvas.name} fade-in complete. Alpha: {group.alpha}");
                onComplete?.Invoke();
            }));
        }

        /// <summary>
        /// Hides a canvas with fade-out animation.
        /// </summary>
        public void HideCanvas(Canvas canvas, CanvasGroup group, Action onComplete = null)
        {
            if (canvas == null || group == null)
                return;

            if (currentTransition != null)
                StopCoroutine(currentTransition);

            group.interactable = false;
            group.blocksRaycasts = false;

            float duration = styleGuide != null ? styleGuide.transitionDuration : 0.3f;

            currentTransition = StartCoroutine(FadeCanvasGroup(group, group.alpha, 0f, duration, () =>
            {
                canvas.gameObject.SetActive(false);
                onComplete?.Invoke();
            }));
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration, Action onComplete)
        {
            float elapsed = 0f;
            group.alpha = from;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease out quad
                t = 1f - (1f - t) * (1f - t);
                group.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            group.alpha = to;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Shows the HUD.
        /// </summary>
        public void ShowHUD()
        {
            if (hudCanvas != null)
            {
                hudCanvas.gameObject.SetActive(true);
                if (hudGroup != null)
                {
                    hudGroup.alpha = 1f;
                }
            }
        }

        /// <summary>
        /// Hides the HUD.
        /// </summary>
        public void HideHUD()
        {
            if (hudCanvas != null && hudGroup != null)
            {
                HideCanvas(hudCanvas, hudGroup);
            }
        }

        /// <summary>
        /// Fades the HUD to a specific alpha.
        /// </summary>
        public void FadeHUD(float targetAlpha, float duration = -1f)
        {
            if (hudGroup == null)
                return;

            if (duration < 0f)
                duration = styleGuide != null ? styleGuide.transitionDuration : 0.3f;

            StartCoroutine(FadeCanvasGroup(hudGroup, hudGroup.alpha, targetAlpha, duration, null));
        }

        /// <summary>
        /// Switches input to UI action map.
        /// </summary>
        public void SwitchToUIInput()
        {
            if (playerInput != null)
            {
                playerInput.SwitchCurrentActionMap(uiActionMap);
            }
        }

        /// <summary>
        /// Switches input to gameplay action map.
        /// </summary>
        public void SwitchToGameplayInput()
        {
            if (playerInput != null)
            {
                playerInput.SwitchCurrentActionMap(gameplayActionMap);
            }
        }

        /// <summary>
        /// Plays a UI sound effect.
        /// </summary>
        public void PlaySound(AudioClip clip, float volumeMultiplier = 1f)
        {
            if (clip == null || uiAudioSource == null)
                return;

            uiAudioSource.PlayOneShot(clip, volumeMultiplier);
        }

        /// <summary>
        /// Plays navigation sound.
        /// </summary>
        public void PlayNavigateSound()
        {
            soundBank?.PlayNavigate(uiAudioSource);
        }

        /// <summary>
        /// Plays selection sound.
        /// </summary>
        public void PlaySelectSound()
        {
            soundBank?.PlaySelect(uiAudioSource);
        }

        /// <summary>
        /// Plays cancel sound.
        /// </summary>
        public void PlayCancelSound()
        {
            soundBank?.PlayCancel(uiAudioSource);
        }

        /// <summary>
        /// Plays confirm sound.
        /// </summary>
        public void PlayConfirmSound()
        {
            soundBank?.PlayConfirm(uiAudioSource);
        }

        /// <summary>
        /// Plays error sound.
        /// </summary>
        public void PlayErrorSound()
        {
            soundBank?.PlayError(uiAudioSource);
        }

        /// <summary>
        /// Plays tab switch sound.
        /// </summary>
        public void PlayTabSwitchSound()
        {
            soundBank?.PlayTabSwitch(uiAudioSource);
        }

        /// <summary>
        /// Sets the selected UI element.
        /// </summary>
        public void SetSelectedGameObject(GameObject go)
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(go);
            }
        }

        /// <summary>
        /// Gets the currently selected UI element.
        /// </summary>
        public GameObject GetSelectedGameObject()
        {
            return EventSystem.current?.currentSelectedGameObject;
        }

        /// <summary>
        /// Ensures a valid selection exists (for gamepad users).
        /// </summary>
        public void EnsureSelection(GameObject defaultSelection)
        {
            if (EventSystem.current == null)
                return;

            if (EventSystem.current.currentSelectedGameObject == null ||
                !EventSystem.current.currentSelectedGameObject.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(defaultSelection);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-find components if not assigned
            if (uiAudioSource == null)
            {
                uiAudioSource = GetComponent<AudioSource>();
            }

            if (playerInput == null)
            {
                playerInput = FindAnyObjectByType<PlayerInput>();
            }
        }
#endif
    }
}
