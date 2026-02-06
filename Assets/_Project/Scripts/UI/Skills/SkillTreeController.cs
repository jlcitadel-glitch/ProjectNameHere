using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectName.UI
{
    /// <summary>
    /// Controller for opening/closing the skill tree UI.
    /// Handles input binding and integration with game state.
    /// </summary>
    public class SkillTreeController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas skillTreeCanvas;
        [SerializeField] private CanvasGroup skillTreeCanvasGroup;
        [SerializeField] private SkillTreePanel skillTreePanel;
        [SerializeField] private Button closeButton;

        [Header("Input")]
        [SerializeField] private InputActionReference openSkillTreeAction;

        [Header("Settings")]
        [SerializeField] private bool pauseGameWhenOpen = true;
        [SerializeField] private int canvasSortOrder = 150;

        private bool isOpen;
        private PlayerInput playerInput;
        private int lastToggleFrame = -1;

        // Fallback input actions created at runtime
        private InputAction fallbackOpenAction;
        private InputAction escapeAction;

        public bool IsOpen => isOpen;

        public event System.Action OnOpened;
        public event System.Action OnClosed;

        private void Awake()
        {
            // Auto-find references
            if (skillTreeCanvas == null)
            {
                skillTreeCanvas = GetComponent<Canvas>();
            }

            if (skillTreeCanvasGroup == null && skillTreeCanvas != null)
            {
                skillTreeCanvasGroup = skillTreeCanvas.GetComponent<CanvasGroup>();
                if (skillTreeCanvasGroup == null)
                {
                    skillTreeCanvasGroup = skillTreeCanvas.gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (skillTreePanel == null)
            {
                skillTreePanel = GetComponentInChildren<SkillTreePanel>();
            }

            // Configure canvas
            if (skillTreeCanvas != null)
            {
                skillTreeCanvas.sortingOrder = canvasSortOrder;
            }

            // Find player input
            playerInput = FindAnyObjectByType<PlayerInput>();

            // Create fallback input actions using new Input System
            fallbackOpenAction = new InputAction("OpenSkillTree", InputActionType.Button, "<Keyboard>/k");
            escapeAction = new InputAction("CloseSkillTree", InputActionType.Button, "<Keyboard>/escape");

            // Wire close button
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            // Start closed
            Close();
        }

        private void OnEnable()
        {
            if (openSkillTreeAction?.action != null)
            {
                openSkillTreeAction.action.Enable();
                openSkillTreeAction.action.performed += OnOpenSkillTreeInput;
            }

            // Always enable fallback K key — ensures input works even if
            // the InputActionReference is assigned but its action map is disabled
            if (fallbackOpenAction != null)
            {
                fallbackOpenAction.Enable();
                fallbackOpenAction.performed += OnOpenSkillTreeInput;
            }

            // Always enable escape to close
            if (escapeAction != null)
            {
                escapeAction.Enable();
                escapeAction.performed += OnEscapeInput;
            }

            // Subscribe to game state changes
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDisable()
        {
            if (openSkillTreeAction?.action != null)
            {
                openSkillTreeAction.action.performed -= OnOpenSkillTreeInput;
            }

            if (fallbackOpenAction != null)
            {
                fallbackOpenAction.performed -= OnOpenSkillTreeInput;
                fallbackOpenAction.Disable();
            }

            if (escapeAction != null)
            {
                escapeAction.performed -= OnEscapeInput;
                escapeAction.Disable();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            // Dispose dynamically created InputActions
            fallbackOpenAction?.Dispose();
            escapeAction?.Dispose();
        }

        private void OnOpenSkillTreeInput(InputAction.CallbackContext context)
        {
            // Prevent double-toggle when both InputActionReference and fallback fire on the same frame
            if (Time.frameCount == lastToggleFrame) return;
            lastToggleFrame = Time.frameCount;
            Toggle();
        }

        private void OnEscapeInput(InputAction.CallbackContext context)
        {
            if (isOpen)
            {
                Close();
            }
        }

        private void HandleGameStateChanged(GameManager.GameState previousState, GameManager.GameState newState)
        {
            // Close skill tree if game state changes to something incompatible
            if (isOpen && newState != GameManager.GameState.Paused && newState != GameManager.GameState.Playing)
            {
                Close();
            }
        }

        /// <summary>
        /// Toggles the skill tree open/closed.
        /// </summary>
        public void Toggle()
        {
            if (isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        /// <summary>
        /// Opens the skill tree UI.
        /// </summary>
        public void Open()
        {
            if (isOpen) return;

            // Don't open during certain game states
            if (GameManager.Instance != null)
            {
                var state = GameManager.Instance.CurrentState;
                if (state == GameManager.GameState.MainMenu ||
                    state == GameManager.GameState.Loading ||
                    state == GameManager.GameState.GameOver ||
                    state == GameManager.GameState.Cutscene)
                {
                    return;
                }
            }

            isOpen = true;

            // Show canvas using CanvasGroup (don't use SetActive - keeps controller running)
            if (skillTreeCanvasGroup != null)
            {
                skillTreeCanvasGroup.alpha = 1f;
                skillTreeCanvasGroup.interactable = true;
                skillTreeCanvasGroup.blocksRaycasts = true;
            }

            // Load current job's skill tree
            if (skillTreePanel != null && SkillManager.Instance?.CurrentJob?.skillTree != null)
            {
                skillTreePanel.LoadTree(SkillManager.Instance.CurrentJob.skillTree);
            }

            // Freeze time if configured (without triggering pause menu)
            if (pauseGameWhenOpen)
            {
                Time.timeScale = 0f;
            }

            // Switch to UI input
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SwitchToUIInput();
            }
            else if (playerInput != null)
            {
                playerInput.SwitchCurrentActionMap("UI");
            }

            UIManager.Instance?.PlaySelectSound();
            OnOpened?.Invoke();

            Debug.Log("[SkillTreeController] Skill tree opened");
        }

        /// <summary>
        /// Closes the skill tree UI.
        /// </summary>
        public void Close()
        {
            if (!isOpen && skillTreeCanvasGroup != null && skillTreeCanvasGroup.alpha == 0f)
            {
                // Already closed
                return;
            }

            isOpen = false;

            // Hide canvas using CanvasGroup (don't use SetActive - keeps controller running for input)
            if (skillTreeCanvasGroup != null)
            {
                skillTreeCanvasGroup.alpha = 0f;
                skillTreeCanvasGroup.interactable = false;
                skillTreeCanvasGroup.blocksRaycasts = false;
            }

            // Restore time if we froze it
            if (pauseGameWhenOpen)
            {
                Time.timeScale = 1f;
            }

            // Switch back to gameplay input
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SwitchToGameplayInput();
            }
            else if (playerInput != null)
            {
                playerInput.SwitchCurrentActionMap("Player");
            }

            UIManager.Instance?.PlayCancelSound();
            OnClosed?.Invoke();

            Debug.Log("[SkillTreeController] Skill tree closed");
        }
    }
}
