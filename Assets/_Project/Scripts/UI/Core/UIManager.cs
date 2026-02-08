using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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

        // Cached input action for pause
        private InputAction pauseAction;

        public UIStyleGuide StyleGuide => styleGuide;
        public UISoundBank SoundBank => soundBank;

        /// <summary>
        /// Returns true if game is paused. Delegates to GameManager.
        /// </summary>
        public bool IsPaused => GameManager.Instance?.IsPaused ?? false;

        /// <summary>
        /// Returns true if player is in a menu. Delegates to GameManager.
        /// </summary>
        public bool IsInMenu => GameManager.Instance?.IsInMenu ?? false;

        private Coroutine currentTransition;

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

            // Subscribe to scene loaded to re-find canvases in new scenes
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            SubscribeToGameManager();
            SetupPauseInput();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[UIManager] Scene loaded: {scene.name}, re-scanning for canvases");
            // Re-scan for canvases that may exist in the new scene
            AutoFindCanvases();

            // Ensure the GameFrameHUD exists on the HUD canvas for gameplay scenes
            EnsureGameFrameHUD();

            // Ensure a death screen exists for gameplay scenes
            EnsureDeathScreen();

            // Ensure the stat menu exists for gameplay scenes
            EnsureStatMenu();

            // Ensure the skill tree exists for gameplay scenes
            EnsureSkillTree();

            // Ensure damage number spawner exists for gameplay scenes
            EnsureDamageNumberSpawner();

            // Re-setup pause input after scene change in case PlayerInput was recreated
            SetupPauseInput();
        }

        private void SetupPauseInput()
        {
            // Find PlayerInput if not assigned
            if (playerInput == null)
            {
                playerInput = FindAnyObjectByType<PlayerInput>();
            }

            // Unsubscribe from old action if exists
            if (pauseAction != null)
            {
                pauseAction.performed -= OnPauseInput;
                pauseAction = null;
            }

            // Try to get pause action from PlayerInput
            if (playerInput != null)
            {
                // Try Player/Pause action first (should work in gameplay)
                pauseAction = playerInput.actions.FindAction("Player/Pause");
                if (pauseAction == null)
                {
                    // Try just "Pause" action
                    pauseAction = playerInput.actions.FindAction("Pause");
                }
                if (pauseAction == null)
                {
                    // Try UI/Cancel as last resort
                    pauseAction = playerInput.actions.FindAction("UI/Cancel");
                }

                if (pauseAction != null)
                {
                    pauseAction.performed += OnPauseInput;
                    // Enable the action independently of action maps so it always works
                    pauseAction.Enable();
                    Debug.Log($"[UIManager] Pause input bound to action: {pauseAction.name}");
                }
                else
                {
                    Debug.LogWarning("[UIManager] No pause/cancel action found in PlayerInput. Using keyboard fallback.");
                }
            }
            else if (!IsMainMenuScene())
            {
                Debug.LogWarning("[UIManager] PlayerInput not found. Using keyboard fallback for pause.");
            }
        }

        /// <summary>
        /// Ensures pause action stays enabled after action map switches.
        /// </summary>
        private void EnsurePauseActionEnabled()
        {
            if (pauseAction != null && !pauseAction.enabled)
            {
                pauseAction.Enable();
            }
        }

        private void OnPauseInput(InputAction.CallbackContext context)
        {
            // Only respond if game state allows pausing
            if (GameManager.Instance != null)
            {
                var state = GameManager.Instance.CurrentState;
                if (state == GameManager.GameState.Playing ||
                    state == GameManager.GameState.BossFight ||
                    state == GameManager.GameState.Paused)
                {
                    GameManager.Instance.TogglePause();
                }
            }
        }

        private void Update()
        {
            // Fallback keyboard input for pause if no InputAction is bound
            if (pauseAction == null && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (GameManager.Instance != null)
                {
                    var state = GameManager.Instance.CurrentState;
                    if (state == GameManager.GameState.Playing ||
                        state == GameManager.GameState.BossFight ||
                        state == GameManager.GameState.Paused)
                    {
                        GameManager.Instance.TogglePause();
                    }
                }
            }
        }

        private void SubscribeToGameManager()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                Debug.Log("[UIManager] Subscribed to GameManager");
            }
            else
            {
                Debug.LogWarning("[UIManager] GameManager not found. UI will not respond to game state changes automatically.");
            }
        }

        private void HandleGameStateChanged(GameManager.GameState previousState, GameManager.GameState newState)
        {
            // Handle pause state
            if (newState == GameManager.GameState.Paused)
            {
                SwitchToUIInput();

                if (pauseCanvas != null && pauseGroup != null)
                {
                    Debug.Log($"[UIManager] Showing pause canvas for state: {newState}");
                    ShowCanvas(pauseCanvas, pauseGroup);
                }

                PlaySound(soundBank?.pause);
            }
            else if (previousState == GameManager.GameState.Paused)
            {
                SwitchToGameplayInput();

                if (pauseCanvas != null && pauseGroup != null)
                {
                    HideCanvas(pauseCanvas, pauseGroup);
                }

                PlaySound(soundBank?.resume);
            }

            // Handle main menu state
            if (newState == GameManager.GameState.MainMenu)
            {
                SwitchToUIInput();
                if (mainMenuCanvas != null && mainMenuGroup != null)
                {
                    ShowCanvas(mainMenuCanvas, mainMenuGroup);
                }
                // Ensure pause menu is hidden when returning to main menu
                if (pauseCanvas != null)
                {
                    pauseCanvas.gameObject.SetActive(false);
                    if (pauseGroup != null)
                    {
                        pauseGroup.alpha = 0f;
                        pauseGroup.interactable = false;
                        pauseGroup.blocksRaycasts = false;
                    }
                }
            }
            else if (previousState == GameManager.GameState.MainMenu)
            {
                if (mainMenuCanvas != null && mainMenuGroup != null)
                {
                    HideCanvas(mainMenuCanvas, mainMenuGroup);
                }
            }

            // When entering gameplay from any non-gameplay state, ensure proper UI setup.
            // Covers Loading->Playing, MainMenu->Playing, and any other unexpected transitions.
            if (newState == GameManager.GameState.Playing &&
                previousState != GameManager.GameState.Paused &&
                previousState != GameManager.GameState.BossFight)
            {
                SwitchToGameplayInput();
                if (pauseCanvas != null)
                {
                    pauseCanvas.gameObject.SetActive(false);
                    if (pauseGroup != null)
                    {
                        pauseGroup.alpha = 0f;
                        pauseGroup.interactable = false;
                        pauseGroup.blocksRaycasts = false;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (currentTransition != null)
                StopCoroutine(currentTransition);

            // Unsubscribe from scene loaded event
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // Unsubscribe from pause action
            if (pauseAction != null)
            {
                pauseAction.performed -= OnPauseInput;
            }

            // Unsubscribe from GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
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

                    // Immediately hide pause canvas unless game is actually paused.
                    // The scene's PauseMenu_Canvas may start active with a visible dark overlay;
                    // it must be hidden until the game is genuinely paused.
                    bool isPaused = GameManager.Instance != null && GameManager.Instance.IsPaused;
                    if (!isPaused)
                    {
                        pauseCanvas.gameObject.SetActive(false);
                        pauseGroup.alpha = 0f;
                        pauseGroup.interactable = false;
                        pauseGroup.blocksRaycasts = false;
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

            // Only warn about missing pause canvas in gameplay scenes, not the main menu
            if (pauseCanvas == null && !IsMainMenuScene())
            {
                Debug.LogWarning("[UIManager] PauseMenu_Canvas not found. Pause menu will not work. Run Tools > ProjectName > UI Setup Wizard to create it.");
            }
        }

        private void EnsureGameFrameHUD()
        {
            if (IsMainMenuScene() || hudCanvas == null)
                return;

            // Remove old TopLeftGroup from previous wizard setup if it exists
            Transform safeArea = hudCanvas.transform.Find("SafeArea");
            Transform searchRoot = safeArea != null ? safeArea : hudCanvas.transform;
            var oldTopLeft = searchRoot.Find("TopLeftGroup");
            if (oldTopLeft != null)
                Destroy(oldTopLeft.gameObject);

            var frameHUD = hudCanvas.GetComponent<GameFrameHUD>();
            if (frameHUD != null)
            {
                // Check if the bottom bar content already exists
                var bottomBar = searchRoot.Find("BottomBar");
                if (bottomBar != null)
                {
                    // Re-bind to new scene's Player systems
                    frameHUD.RebindPlayerSystems();
                    return;
                }
            }

            GameFrameHUD.CreateRuntimeUI(hudCanvas);
        }

        private void EnsureDeathScreen()
        {
            if (IsMainMenuScene())
                return;

            // Check if a DeathScreen already exists (including inactive objects)
            var existing = FindObjectsByType<DeathScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (existing.Length > 0)
                return;

            DeathScreen.CreateRuntimeUI();
            Debug.Log("[UIManager] Created runtime DeathScreen");
        }

        private void EnsureStatMenu()
        {
            if (IsMainMenuScene())
                return;

            var existing = FindObjectsByType<StatMenuController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (existing.Length > 0)
                return;

            StatMenuController.CreateRuntimeUI();
            Debug.Log("[UIManager] Created runtime StatMenu");
        }

        private void EnsureSkillTree()
        {
            if (IsMainMenuScene())
                return;

            var existing = FindObjectsByType<SkillTreeController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (existing.Length > 0)
                return;

            SkillTreeController.CreateRuntimeUI();
            Debug.Log("[UIManager] Created runtime SkillTree");
        }

        private void EnsureDamageNumberSpawner()
        {
            if (IsMainMenuScene())
                return;

            if (DamageNumberSpawner.Instance != null)
                return;

            var go = new GameObject("DamageNumberSpawner");
            go.AddComponent<DamageNumberSpawner>();
            DontDestroyOnLoad(go);
            Debug.Log("[UIManager] Created runtime DamageNumberSpawner");
        }

        private bool IsMainMenuScene()
        {
            var state = GameManager.Instance?.CurrentState;
            if (state == GameManager.GameState.MainMenu)
                return true;

            // Also check by scene name as fallback (GameManager may not be ready yet)
            string sceneName = SceneManager.GetActiveScene().name.ToLower();
            return sceneName.Contains("menu") || sceneName.Contains("title");
        }

        private void InitializeAudio()
        {
            if (uiAudioSource == null)
            {
                uiAudioSource = gameObject.AddComponent<AudioSource>();
                uiAudioSource.playOnAwake = false;
                uiAudioSource.spatialBlend = 0f; // 2D sound
            }

            if (soundBank == null)
            {
                soundBank = Resources.Load<UISoundBank>("UISoundBank");
            }
        }

        /// <summary>
        /// Toggles pause state. Delegates to GameManager.
        /// </summary>
        public void TogglePause()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TogglePause();
            }
            else
            {
                Debug.LogWarning("[UIManager] GameManager not found. Cannot toggle pause.");
            }
        }

        /// <summary>
        /// Pauses the game. Delegates to GameManager.
        /// UI changes are handled via HandleGameStateChanged.
        /// </summary>
        public void Pause()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Pause();
            }
            else
            {
                Debug.LogWarning("[UIManager] GameManager not found. Cannot pause.");
            }
        }

        /// <summary>
        /// Resumes the game. Delegates to GameManager.
        /// UI changes are handled via HandleGameStateChanged.
        /// </summary>
        public void Resume()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Resume();
            }
            else
            {
                Debug.LogWarning("[UIManager] GameManager not found. Cannot resume.");
            }
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
                // Re-enable pause action after map switch (it may have been disabled)
                EnsurePauseActionEnabled();
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
                // Re-enable pause action after map switch (it may have been disabled)
                EnsurePauseActionEnabled();
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
