using UnityEngine;
using UnityEngine.InputSystem;

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

        private void Update()
        {
            // Pause toggle
            if (Keyboard.current != null && Keyboard.current[pauseKey].wasPressedThisFrame)
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.TogglePause();
                    Debug.Log($"[UITest] Pause toggled. IsPaused: {UIManager.Instance.IsPaused}");
                }
                else
                {
                    Debug.LogWarning("[UITest] UIManager.Instance is null. Make sure UIManager exists in scene.");
                }
            }

            // Show HUD
            if (Keyboard.current != null && Keyboard.current[showHUDKey].wasPressedThisFrame)
            {
                UIManager.Instance?.ShowHUD();
                Debug.Log("[UITest] HUD shown");
            }

            // Hide HUD
            if (Keyboard.current != null && Keyboard.current[hideHUDKey].wasPressedThisFrame)
            {
                UIManager.Instance?.HideHUD();
                Debug.Log("[UITest] HUD hidden");
            }
        }

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.BeginVertical("box");

            GUILayout.Label("<b>UI Test Controls</b>");
            GUILayout.Label($"{pauseKey}: Toggle Pause");
            GUILayout.Label($"{showHUDKey}: Show HUD");
            GUILayout.Label($"{hideHUDKey}: Hide HUD");

            GUILayout.Space(10);

            if (UIManager.Instance != null)
            {
                GUILayout.Label($"Paused: {UIManager.Instance.IsPaused}");
                GUILayout.Label($"In Menu: {UIManager.Instance.IsInMenu}");
            }
            else
            {
                GUILayout.Label("<color=red>UIManager not found!</color>");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
