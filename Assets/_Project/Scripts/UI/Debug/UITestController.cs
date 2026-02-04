using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectName.UI
{
    /// <summary>
    /// Test controller for UI system.
    /// Provides keyboard shortcuts to test UI functionality during development.
    /// Remove or disable in production builds.
    /// </summary>
    public class UITestController : MonoBehaviour
    {
        [Header("Test Keybinds")]
        [SerializeField] private Key pauseKey = Key.Escape;
        [SerializeField] private Key showHUDKey = Key.F1;
        [SerializeField] private Key hideHUDKey = Key.F2;

        [Header("Debug Display")]
        [SerializeField] private bool showDebugInfo = true;

        [Header("Fallback References (Auto-found if empty)")]
        [SerializeField] private Canvas pauseCanvas;
        [SerializeField] private CanvasGroup pauseGroup;

        private bool isPausedFallback = false;

        private void Start()
        {
            // Auto-find pause canvas if not assigned
            if (pauseCanvas == null)
            {
                var pauseCanvasGO = GameObject.Find("PauseMenu_Canvas");
                if (pauseCanvasGO != null)
                {
                    pauseCanvas = pauseCanvasGO.GetComponent<Canvas>();
                    pauseGroup = pauseCanvasGO.GetComponent<CanvasGroup>();
                    Debug.Log("[UITest] Auto-found PauseMenu_Canvas");
                }
                else
                {
                    // Try to find inactive
                    var allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();
                    foreach (var canvas in allCanvases)
                    {
                        if (canvas.gameObject.scene.name != null && canvas.name == "PauseMenu_Canvas")
                        {
                            pauseCanvas = canvas;
                            pauseGroup = canvas.GetComponent<CanvasGroup>();
                            Debug.Log("[UITest] Auto-found inactive PauseMenu_Canvas");
                            break;
                        }
                    }
                }
            }
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            // Pause toggle
            if (Keyboard.current[pauseKey].wasPressedThisFrame)
            {
                TogglePause();
            }

            // Show HUD
            if (Keyboard.current[showHUDKey].wasPressedThisFrame)
            {
                UIManager.Instance?.ShowHUD();
                Debug.Log("[UITest] HUD shown");
            }

            // Hide HUD
            if (Keyboard.current[hideHUDKey].wasPressedThisFrame)
            {
                UIManager.Instance?.HideHUD();
                Debug.Log("[UITest] HUD hidden");
            }
        }

        private void TogglePause()
        {
            // Check if we're currently paused (GameManager is primary source of truth)
            bool currentlyPaused = false;

            if (GameManager.Instance != null)
            {
                currentlyPaused = GameManager.Instance.IsPaused;
            }
            else if (UIManager.Instance != null)
            {
                currentlyPaused = UIManager.Instance.IsPaused;
            }
            else
            {
                currentlyPaused = isPausedFallback;
            }

            if (currentlyPaused)
            {
                // When paused, ESC should go back through menu hierarchy
                // If in options -> go to main pause
                // If in main pause -> resume game
                var pauseController = pauseCanvas?.GetComponent<PauseMenuController>();
                if (pauseController != null)
                {
                    pauseController.OnCancelInput();
                }
                else
                {
                    // No controller, just resume
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.Resume();
                    }
                    else if (UIManager.Instance != null)
                    {
                        UIManager.Instance.Resume();
                    }
                    if (isPausedFallback)
                    {
                        HidePauseCanvasFallback();
                    }
                }
            }
            else
            {
                // Pause
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TogglePause();
                }
                else if (UIManager.Instance != null)
                {
                    UIManager.Instance.TogglePause();

                    // Check if UIManager actually showed the canvas
                    if (UIManager.Instance.IsPaused && pauseCanvas != null && !pauseCanvas.gameObject.activeSelf)
                    {
                        ShowPauseCanvasFallback();
                    }
                }
                else if (pauseCanvas != null)
                {
                    // Fallback: directly show the pause canvas
                    ShowPauseCanvasFallback();
                }
            }
        }

        private void ShowPauseCanvasFallback()
        {
            if (pauseCanvas == null) return;

            isPausedFallback = true;
            Time.timeScale = 0f;
            pauseCanvas.gameObject.SetActive(true);

            if (pauseGroup != null)
            {
                pauseGroup.alpha = 1f;
                pauseGroup.interactable = true;
                pauseGroup.blocksRaycasts = true;
            }

            // Try to show the main pause panel
            var pauseController = pauseCanvas.GetComponent<PauseMenuController>();
            if (pauseController != null)
            {
                pauseController.ShowMainPause();
            }
        }

        private void HidePauseCanvasFallback()
        {
            if (pauseCanvas == null) return;

            isPausedFallback = false;
            Time.timeScale = 1f;
            pauseCanvas.gameObject.SetActive(false);
        }

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            // Compact debug info panel
            GUILayout.BeginArea(new Rect(10, 10, 280, 140));
            GUILayout.BeginVertical("box");

            GUILayout.Label("<b>UI Debug</b>");
            GUILayout.Label($"{pauseKey}: Pause | {showHUDKey}/{hideHUDKey}: HUD");

            if (GameManager.Instance != null)
            {
                GUILayout.Label($"GameState: {GameManager.Instance.CurrentState}");
                GUILayout.Label($"Paused: {GameManager.Instance.IsPaused}");
            }
            else if (UIManager.Instance != null)
            {
                GUILayout.Label($"Paused: {UIManager.Instance.IsPaused}");
                GUILayout.Label("<color=yellow>GameManager not found</color>");
            }

            GUILayout.Label($"Screen: {Screen.width}x{Screen.height}");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
