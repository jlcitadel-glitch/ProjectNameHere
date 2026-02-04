using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Controls the pause menu and its sub-menus (options, etc.)
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainPausePanel;
        [SerializeField] private GameObject optionsPanel;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button quitButton;

        [Header("Options Navigation")]
        [SerializeField] private Button optionsBackButton;

        [Header("First Selected")]
        [SerializeField] private GameObject mainMenuFirstSelected;
        [SerializeField] private GameObject optionsFirstSelected;

        private GameObject lastSelected;
        private MenuState currentState = MenuState.Closed;

        public enum MenuState
        {
            Closed,
            MainPause,
            Options
        }

        public MenuState CurrentState => currentState;
        public event Action<MenuState> OnStateChanged;

        private void Awake()
        {
            AutoFindReferences();
            SetupButtons();

            // Ensure correct initial state - options hidden, main panel ready
            if (optionsPanel != null)
                optionsPanel.SetActive(false);
        }

        private void AutoFindReferences()
        {
            // Auto-find panels if not assigned
            if (mainPausePanel == null)
            {
                var found = transform.Find("SafeArea/MainPausePanel");
                if (found != null)
                {
                    mainPausePanel = found.gameObject;
                    Debug.Log("[PauseMenu] Auto-found MainPausePanel");
                }
            }

            if (optionsPanel == null)
            {
                var found = transform.Find("SafeArea/OptionsPanel");
                if (found != null)
                {
                    optionsPanel = found.gameObject;
                    Debug.Log("[PauseMenu] Auto-found OptionsPanel");
                }
            }

            // Auto-find buttons if not assigned
            if (resumeButton == null && mainPausePanel != null)
            {
                var found = mainPausePanel.transform.Find("ResumeButton");
                if (found != null)
                {
                    resumeButton = found.GetComponent<Button>();
                    Debug.Log("[PauseMenu] Auto-found ResumeButton");
                }
            }

            if (optionsButton == null && mainPausePanel != null)
            {
                var found = mainPausePanel.transform.Find("OptionsButton");
                if (found != null)
                {
                    optionsButton = found.GetComponent<Button>();
                    Debug.Log("[PauseMenu] Auto-found OptionsButton");
                }
            }

            if (quitButton == null && mainPausePanel != null)
            {
                var found = mainPausePanel.transform.Find("QuitButton");
                if (found != null)
                {
                    quitButton = found.GetComponent<Button>();
                    Debug.Log("[PauseMenu] Auto-found QuitButton");
                }
            }

            if (optionsBackButton == null && optionsPanel != null)
            {
                var found = optionsPanel.transform.Find("Header/BackButton");
                if (found != null)
                {
                    optionsBackButton = found.GetComponent<Button>();
                    Debug.Log("[PauseMenu] Auto-found BackButton");
                }

                // Fix: Disable raycastTarget on the Title text so it doesn't block back button clicks
                var titleText = optionsPanel.transform.Find("Header/Title");
                if (titleText != null)
                {
                    var tmp = titleText.GetComponent<TMPro.TMP_Text>();
                    if (tmp != null && tmp.raycastTarget)
                    {
                        tmp.raycastTarget = false;
                        Debug.Log("[PauseMenu] Fixed Title raycastTarget blocking BackButton");
                    }
                }
            }

            // Set first selected defaults
            if (mainMenuFirstSelected == null && resumeButton != null)
            {
                mainMenuFirstSelected = resumeButton.gameObject;
            }

            if (optionsFirstSelected == null && optionsBackButton != null)
            {
                optionsFirstSelected = optionsBackButton.gameObject;
            }
        }

        private void OnEnable()
        {
            // Subscribe to GameManager (primary)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPause += OnGamePaused;
                GameManager.Instance.OnResume += OnGameResumed;

                // If we're enabled while the game is already paused, show the main pause menu
                if (GameManager.Instance.IsPaused)
                {
                    ShowMainPause();
                }
            }
            else
            {
                Debug.LogWarning("[PauseMenu] GameManager not found. Pause menu may not function correctly.");
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPause -= OnGamePaused;
                GameManager.Instance.OnResume -= OnGameResumed;
            }
        }

        private void SetupButtons()
        {
            Debug.Log($"[PauseMenu] SetupButtons - Resume: {(resumeButton != null ? "OK" : "NULL")}, Options: {(optionsButton != null ? "OK" : "NULL")}, Quit: {(quitButton != null ? "OK" : "NULL")}");

            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeClicked);

            if (optionsButton != null)
                optionsButton.onClick.AddListener(OnOptionsClicked);
            else
                Debug.LogWarning("[PauseMenu] optionsButton is NULL - Options button won't work!");

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            if (optionsBackButton != null)
            {
                optionsBackButton.onClick.AddListener(OnOptionsBackClicked);
                Debug.Log($"[PauseMenu] BackButton listener added. Interactable: {optionsBackButton.interactable}");
            }
            else
            {
                Debug.LogWarning("[PauseMenu] optionsBackButton is NULL - Back button won't work!");
            }
        }

        private void OnGamePaused()
        {
            ShowMainPause();
        }

        private void OnGameResumed()
        {
            CloseAll();
        }

        #region State Management

        public void ShowMainPause()
        {
            currentState = MenuState.MainPause;
            Debug.Log($"[PauseMenu] ShowMainPause called. mainPausePanel: {(mainPausePanel != null ? mainPausePanel.name : "NULL")}");

            if (mainPausePanel != null)
            {
                mainPausePanel.SetActive(true);
                Debug.Log($"[PauseMenu] MainPausePanel activated: {mainPausePanel.activeSelf}");
            }
            else
            {
                Debug.LogWarning("[PauseMenu] mainPausePanel is NULL! The pause menu won't show anything.");
            }

            if (optionsPanel != null)
                optionsPanel.SetActive(false);

            SetSelected(mainMenuFirstSelected ?? resumeButton?.gameObject);
            OnStateChanged?.Invoke(currentState);
        }

        public void ShowOptions()
        {
            Debug.Log($"[PauseMenu] ShowOptions called. optionsPanel: {(optionsPanel != null ? optionsPanel.name : "NULL")}");
            currentState = MenuState.Options;

            // Store last selected for when we come back
            lastSelected = EventSystem.current?.currentSelectedGameObject;

            if (mainPausePanel != null)
                mainPausePanel.SetActive(false);

            if (optionsPanel != null)
            {
                optionsPanel.SetActive(true);
                Debug.Log($"[PauseMenu] OptionsPanel activated: {optionsPanel.activeSelf}");

                // Debug back button state
                if (optionsBackButton != null)
                {
                    Debug.Log($"[PauseMenu] BackButton state - Active: {optionsBackButton.gameObject.activeSelf}, Interactable: {optionsBackButton.interactable}, Image raycast: {optionsBackButton.GetComponent<UnityEngine.UI.Image>()?.raycastTarget}");
                }
            }
            else
            {
                Debug.LogWarning("[PauseMenu] optionsPanel is NULL! Cannot show options.");
            }

            SetSelected(optionsFirstSelected ?? optionsBackButton?.gameObject);
            OnStateChanged?.Invoke(currentState);
        }

        public void CloseOptions()
        {
            Debug.Log("[PauseMenu] CloseOptions called");
            ShowMainPause();

            // Restore selection
            if (lastSelected != null)
                SetSelected(lastSelected);
        }

        public void CloseAll()
        {
            currentState = MenuState.Closed;

            if (mainPausePanel != null)
                mainPausePanel.SetActive(false);

            if (optionsPanel != null)
                optionsPanel.SetActive(false);

            OnStateChanged?.Invoke(currentState);
        }

        private void SetSelected(GameObject go)
        {
            if (go != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(go);
            }
        }

        #endregion

        #region Button Callbacks

        private void OnResumeClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Resume();
            }
            else
            {
                UIManager.Instance?.Resume();
            }
        }

        private void OnOptionsClicked()
        {
            Debug.Log("[PauseMenu] Options button clicked!");
            ShowOptions();
            UIManager.Instance?.PlaySelectSound();
        }

        private void OnOptionsBackClicked()
        {
            Debug.Log("[PauseMenu] Back button clicked!");
            CloseOptions();
            UIManager.Instance?.PlayCancelSound();
        }

        private void OnQuitClicked()
        {
            // For now, just quit to desktop
            // TODO: Add quit confirmation dialog
            // TODO: Add "Quit to Main Menu" option
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Input Handling

        /// <summary>
        /// Call this from your input system when Cancel/Back is pressed.
        /// </summary>
        public void OnCancelInput()
        {
            Debug.Log($"[PauseMenu] OnCancelInput called. CurrentState: {currentState}");
            switch (currentState)
            {
                case MenuState.Options:
                    CloseOptions();
                    break;
                case MenuState.MainPause:
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.Resume();
                    }
                    else
                    {
                        UIManager.Instance?.Resume();
                    }
                    break;
            }
        }

        #endregion
    }
}
