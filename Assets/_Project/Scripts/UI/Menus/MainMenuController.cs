using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Controls the main menu navigation and state.
    /// Handles title screen, save selection, options, and overwrite confirmation.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        public enum MainMenuState
        {
            Title,
            SaveSelection,
            Options,
            ConfirmOverwrite,
            ConfirmDelete,
            CharacterCreation,
            Credits
        }

        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject saveSelectionPanel;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject overwriteConfirmPanel;
        [SerializeField] private GameObject deleteConfirmPanel;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button quitButton;

        [Header("Save Selection")]
        [SerializeField] private Transform saveSlotContainer;
        [SerializeField] private SaveSlotUI[] saveSlots;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button backFromSaveButton;

        [Header("Overwrite Confirmation")]
        [SerializeField] private TMP_Text overwriteWarningText;
        [SerializeField] private TMP_Text overwriteSlotInfoText;
        [SerializeField] private Button confirmOverwriteButton;
        [SerializeField] private Button cancelOverwriteButton;

        [Header("Delete Confirmation")]
        [SerializeField] private TMP_Text deleteWarningText;
        [SerializeField] private TMP_Text deleteSlotInfoText;
        [SerializeField] private Button confirmDeleteButton;
        [SerializeField] private Button cancelDeleteButton;

        [Header("Options Navigation")]
        [SerializeField] private Button optionsBackButton;

        [Header("Credits")]
        [SerializeField] private Button creditsButton;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private CreditsController creditsController;

        [Header("Character Creation")]
        [SerializeField] private CharacterCreationController characterCreation;

        [Header("Audio")]
        [SerializeField] private AudioClip menuMusic;

        [Header("First Selected")]
        [SerializeField] private GameObject mainMenuFirstSelected;
        [SerializeField] private GameObject saveSelectionFirstSelected;
        [SerializeField] private GameObject optionsFirstSelected;
        [SerializeField] private GameObject overwriteFirstSelected;

        private MainMenuState currentState = MainMenuState.Title;
        private int pendingSlotIndex = -1;
        private bool isSelectingSlotForNewGame;

        public MainMenuState CurrentState => currentState;
        public event Action<MainMenuState> OnStateChanged;

        private const int SAVE_SLOT_COUNT = 5;

        private void Awake()
        {
            AutoFindReferences();
            SetupButtons();
            SetupSaveSlots();
        }

        private void Start()
        {
            EnsureGameManager();
            EnsureUIManager();
            EnsureSaveManager();
            EnsureSceneLoader();
            EnsureDisplaySettings();
            EnsureMusicManager();
            EnsureCharacterCreation();
            AddButtonSounds();
            AdjustMainMenuLayout();

            // Wire credits back button
            if (creditsController != null)
            {
                creditsController.OnBackPressed += () => ShowMainMenu();
            }

            ShowMainMenu();

            // Set game state to MainMenu
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameManager.GameState.MainMenu);
            }

            MusicManager.Instance?.PlayTrack(menuMusic);
        }

        private void EnsureMusicManager()
        {
            if (MusicManager.Instance != null)
                return;

            var go = new GameObject("MusicManager");
            go.AddComponent<MusicManager>();
        }

        private void AddButtonSounds()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button.GetComponent<UIButtonSounds>() != null)
                    continue;

                var sounds = button.gameObject.AddComponent<UIButtonSounds>();
                string name = button.gameObject.name.ToLower();

                if (name.Contains("back") || name.Contains("cancel"))
                    sounds.SetClickSound(UIButtonSounds.ClickSoundType.Cancel);
                else if (name.Contains("confirm"))
                    sounds.SetClickSound(UIButtonSounds.ClickSoundType.Confirm);
            }
        }

        private void EnsureUIManager()
        {
            if (UIManager.Instance != null)
                return;

            var go = new GameObject("UIManager");
            go.AddComponent<UIManager>();
        }

        private void EnsureSceneLoader()
        {
            if (SceneLoader.Instance != null)
                return;

            var go = new GameObject("SceneLoader");
            go.AddComponent<SceneLoader>();
        }

        private void EnsureDisplaySettings()
        {
            if (DisplaySettings.Instance != null)
                return;

            var go = new GameObject("DisplaySettings");
            go.AddComponent<DisplaySettings>();
            // DisplaySettings handles DontDestroyOnLoad in its Awake
        }

        private void EnsureGameManager()
        {
            if (GameManager.Instance != null)
                return;

            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }

        private void EnsureSaveManager()
        {
            if (SaveManager.Instance != null)
                return;

            var go = new GameObject("SaveManager");
            go.AddComponent<SaveManager>();
        }

        private void EnsureCharacterCreation()
        {
            if (characterCreation != null)
                return;

            var safeArea = transform.Find("SafeArea");
            Transform uiParent = safeArea != null ? safeArea : transform;

            characterCreation = CharacterCreationController.CreateRuntimeUI(uiParent);

            // Wire events (Awake already ran, but characterCreation was null then)
            characterCreation.OnCreationComplete += OnCharacterCreationComplete;
            characterCreation.OnCreationCancelled += OnCharacterCreationCancelled;
        }

        private void AutoFindReferences()
        {
            var safeArea = transform.Find("SafeArea");
            Transform parent = safeArea != null ? safeArea : transform;

            // Find panels
            if (mainMenuPanel == null)
            {
                var found = parent.Find("MainMenuPanel");
                if (found != null) mainMenuPanel = found.gameObject;
            }

            if (saveSelectionPanel == null)
            {
                var found = parent.Find("SaveSelectionPanel");
                if (found != null) saveSelectionPanel = found.gameObject;
            }

            if (optionsPanel == null)
            {
                var found = parent.Find("OptionsPanel");
                if (found != null) optionsPanel = found.gameObject;
            }

            if (overwriteConfirmPanel == null)
            {
                var found = parent.Find("OverwriteConfirmPanel");
                if (found != null) overwriteConfirmPanel = found.gameObject;
            }

            if (deleteConfirmPanel == null)
            {
                var found = parent.Find("DeleteConfirmPanel");
                if (found != null) deleteConfirmPanel = found.gameObject;
            }

            // Find main menu buttons
            if (mainMenuPanel != null)
            {
                if (startGameButton == null)
                {
                    var found = mainMenuPanel.transform.Find("StartGameButton");
                    if (found != null) startGameButton = found.GetComponent<Button>();
                }

                if (optionsButton == null)
                {
                    var found = mainMenuPanel.transform.Find("OptionsButton");
                    if (found != null) optionsButton = found.GetComponent<Button>();
                }

                if (quitButton == null)
                {
                    var found = mainMenuPanel.transform.Find("QuitButton");
                    if (found != null) quitButton = found.GetComponent<Button>();
                }
            }

            // Find save selection elements
            if (saveSelectionPanel != null)
            {
                if (saveSlotContainer == null)
                {
                    var found = saveSelectionPanel.transform.Find("SaveSlotContainer");
                    if (found != null) saveSlotContainer = found;
                }

                if (newGameButton == null)
                {
                    var found = saveSelectionPanel.transform.Find("NewGameButton");
                    if (found != null) newGameButton = found.GetComponent<Button>();
                }

                if (backFromSaveButton == null)
                {
                    var found = saveSelectionPanel.transform.Find("BackButton");
                    if (found != null) backFromSaveButton = found.GetComponent<Button>();
                }
            }

            // Find overwrite confirmation elements
            if (overwriteConfirmPanel != null)
            {
                if (overwriteWarningText == null)
                {
                    var found = overwriteConfirmPanel.transform.Find("WarningText");
                    if (found != null) overwriteWarningText = found.GetComponent<TMP_Text>();
                }

                if (overwriteSlotInfoText == null)
                {
                    var found = overwriteConfirmPanel.transform.Find("SlotInfoText");
                    if (found != null) overwriteSlotInfoText = found.GetComponent<TMP_Text>();
                }

                if (confirmOverwriteButton == null)
                {
                    var found = overwriteConfirmPanel.transform.Find("ConfirmButton");
                    if (found != null) confirmOverwriteButton = found.GetComponent<Button>();
                }

                if (cancelOverwriteButton == null)
                {
                    var found = overwriteConfirmPanel.transform.Find("CancelButton");
                    if (found != null) cancelOverwriteButton = found.GetComponent<Button>();
                }
            }

            // Find delete confirmation elements
            if (deleteConfirmPanel != null)
            {
                if (deleteWarningText == null)
                {
                    var found = deleteConfirmPanel.transform.Find("WarningText");
                    if (found != null) deleteWarningText = found.GetComponent<TMP_Text>();
                }

                if (deleteSlotInfoText == null)
                {
                    var found = deleteConfirmPanel.transform.Find("SlotInfoText");
                    if (found != null) deleteSlotInfoText = found.GetComponent<TMP_Text>();
                }

                if (confirmDeleteButton == null)
                {
                    var found = deleteConfirmPanel.transform.Find("ConfirmButton");
                    if (found != null) confirmDeleteButton = found.GetComponent<Button>();
                }

                if (cancelDeleteButton == null)
                {
                    var found = deleteConfirmPanel.transform.Find("CancelButton");
                    if (found != null) cancelDeleteButton = found.GetComponent<Button>();
                }
            }

            // Find options back button
            if (optionsPanel != null && optionsBackButton == null)
            {
                var found = optionsPanel.transform.Find("Header/BackButton");
                if (found != null) optionsBackButton = found.GetComponent<Button>();
            }

            // Find character creation controller
            if (characterCreation == null)
            {
                var found = parent.Find("CharacterCreationPanel");
                if (found != null)
                    characterCreation = found.GetComponent<CharacterCreationController>();
            }
            // Also check as direct child component if panel name differs
            if (characterCreation == null)
            {
                characterCreation = GetComponentInChildren<CharacterCreationController>(true);
            }

            // Set default first selected
            if (mainMenuFirstSelected == null && startGameButton != null)
                mainMenuFirstSelected = startGameButton.gameObject;
        }

        private void SetupButtons()
        {
            if (startGameButton != null)
                startGameButton.onClick.AddListener(OnStartGameClicked);

            if (optionsButton != null)
                optionsButton.onClick.AddListener(OnOptionsClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            if (newGameButton != null)
                newGameButton.onClick.AddListener(OnNewGameClicked);

            if (backFromSaveButton != null)
                backFromSaveButton.onClick.AddListener(OnBackFromSaveClicked);

            if (confirmOverwriteButton != null)
                confirmOverwriteButton.onClick.AddListener(OnConfirmOverwriteClicked);

            if (cancelOverwriteButton != null)
                cancelOverwriteButton.onClick.AddListener(OnCancelOverwriteClicked);

            if (confirmDeleteButton != null)
                confirmDeleteButton.onClick.AddListener(OnConfirmDeleteClicked);

            if (cancelDeleteButton != null)
                cancelDeleteButton.onClick.AddListener(OnCancelDeleteClicked);

            if (optionsBackButton != null)
                optionsBackButton.onClick.AddListener(OnOptionsBackClicked);

            if (creditsButton != null)
                creditsButton.onClick.AddListener(OnCreditsClicked);

            // Character creation events
            if (characterCreation != null)
            {
                characterCreation.OnCreationComplete += OnCharacterCreationComplete;
                characterCreation.OnCreationCancelled += OnCharacterCreationCancelled;
            }
        }

        private void SetupSaveSlots()
        {
            // Find save slots in container if not assigned
            if ((saveSlots == null || saveSlots.Length == 0) && saveSlotContainer != null)
            {
                saveSlots = saveSlotContainer.GetComponentsInChildren<SaveSlotUI>(true);
            }

            // Initialize save slots
            if (saveSlots != null)
            {
                for (int i = 0; i < saveSlots.Length; i++)
                {
                    if (saveSlots[i] != null)
                    {
                        saveSlots[i].Initialize(i);
                        saveSlots[i].OnSlotClicked += OnSaveSlotClicked;
                        saveSlots[i].OnDeleteClicked += OnSaveSlotDeleteClicked;
                    }
                }
            }
        }

        #region State Management

        /// <summary>
        /// Shows the main menu (title + main buttons).
        /// </summary>
        public void ShowMainMenu()
        {
            SetState(MainMenuState.Title);

            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (saveSelectionPanel != null) saveSelectionPanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (overwriteConfirmPanel != null) overwriteConfirmPanel.SetActive(false);
            if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (characterCreation != null) characterCreation.gameObject.SetActive(false);

            SetSelected(mainMenuFirstSelected);
        }

        /// <summary>
        /// Shows the save selection panel.
        /// </summary>
        public void ShowSaveSelection()
        {
            SetState(MainMenuState.SaveSelection);
            isSelectingSlotForNewGame = false;

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (saveSelectionPanel != null) saveSelectionPanel.SetActive(true);
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (overwriteConfirmPanel != null) overwriteConfirmPanel.SetActive(false);
            if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (characterCreation != null) characterCreation.gameObject.SetActive(false);

            RefreshSaveSlots();

            SetSelected(saveSelectionFirstSelected ?? (saveSlots?.Length > 0 ? saveSlots[0]?.gameObject : null));
        }

        /// <summary>
        /// Shows the options panel.
        /// </summary>
        public void ShowOptions()
        {
            SetState(MainMenuState.Options);

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (saveSelectionPanel != null) saveSelectionPanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(true);
            if (overwriteConfirmPanel != null) overwriteConfirmPanel.SetActive(false);
            if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (characterCreation != null) characterCreation.gameObject.SetActive(false);

            SetSelected(optionsFirstSelected ?? optionsBackButton?.gameObject);
        }

        /// <summary>
        /// Shows the credits panel.
        /// </summary>
        public void ShowCredits()
        {
            SetState(MainMenuState.Credits);

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (saveSelectionPanel != null) saveSelectionPanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (overwriteConfirmPanel != null) overwriteConfirmPanel.SetActive(false);
            if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(false);
            if (characterCreation != null) characterCreation.gameObject.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(true);
        }

        /// <summary>
        /// Shows the overwrite confirmation panel.
        /// </summary>
        public void ShowOverwriteConfirm(int slotIndex)
        {
            SetState(MainMenuState.ConfirmOverwrite);
            pendingSlotIndex = slotIndex;

            if (overwriteConfirmPanel != null) overwriteConfirmPanel.SetActive(true);
            if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(false);

            // Update confirmation text
            if (SaveManager.Instance != null)
            {
                var info = SaveManager.Instance.GetSlotInfo(slotIndex);
                if (overwriteSlotInfoText != null)
                {
                    string waveInfo = !string.IsNullOrEmpty(info.FormattedWave) ? $" - {info.FormattedWave}" : "";
                    overwriteSlotInfoText.text = $"Slot {slotIndex + 1}: {info.characterName} - Lv. {info.playerLevel}{waveInfo}\n{info.FormattedPlayTime} playtime - {info.FormattedDate}";
                }
            }

            SetSelected(cancelOverwriteButton?.gameObject ?? overwriteFirstSelected);
        }

        /// <summary>
        /// Shows the delete confirmation panel.
        /// </summary>
        public void ShowDeleteConfirm(int slotIndex)
        {
            SetState(MainMenuState.ConfirmDelete);
            pendingSlotIndex = slotIndex;

            if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(true);
            if (overwriteConfirmPanel != null) overwriteConfirmPanel.SetActive(false);

            // Update confirmation text
            if (SaveManager.Instance != null)
            {
                var info = SaveManager.Instance.GetSlotInfo(slotIndex);
                if (deleteSlotInfoText != null)
                {
                    string waveInfo = !string.IsNullOrEmpty(info.FormattedWave) ? $" - {info.FormattedWave}" : "";
                    deleteSlotInfoText.text = $"Slot {slotIndex + 1}: {info.characterName} - Lv. {info.playerLevel}{waveInfo}\n{info.FormattedPlayTime} playtime - {info.FormattedDate}";
                }
            }

            SetSelected(cancelDeleteButton?.gameObject);
        }

        private void SetState(MainMenuState newState)
        {
            currentState = newState;
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

        #region Save Slot Management

        private void RefreshSaveSlots()
        {
            if (SaveManager.Instance == null || saveSlots == null)
                return;

            var allSlotInfo = SaveManager.Instance.GetAllSlotInfo();
            bool hasEmptySlot = false;

            for (int i = 0; i < saveSlots.Length && i < allSlotInfo.Length; i++)
            {
                if (saveSlots[i] != null)
                {
                    saveSlots[i].SetSlotData(allSlotInfo[i]);

                    if (allSlotInfo[i].isEmpty)
                        hasEmptySlot = true;
                }
            }

            // Show/hide New Game button based on whether all slots are full
            if (newGameButton != null)
            {
                // Only show New Game button if all slots are filled
                // (user needs to select which slot to overwrite)
                newGameButton.gameObject.SetActive(!hasEmptySlot);
            }
        }

        #endregion

        #region Button Callbacks

        private void OnStartGameClicked()
        {
            ShowSaveSelection();
            UIManager.Instance?.PlaySelectSound();
        }

        private void OnOptionsClicked()
        {
            ShowOptions();
            UIManager.Instance?.PlaySelectSound();
        }

        private void OnQuitClicked()
        {
            UIManager.Instance?.PlaySelectSound();
            QuitGame();
        }

        private void OnNewGameClicked()
        {
            // All slots are full, user needs to select which to overwrite
            isSelectingSlotForNewGame = true;
            UIManager.Instance?.PlaySelectSound();

            // Update visual hint that user is selecting slot to overwrite
            // The next slot click will trigger overwrite confirmation
        }

        private void OnBackFromSaveClicked()
        {
            ShowMainMenu();
            UIManager.Instance?.PlayCancelSound();
        }

        private void OnCreditsClicked()
        {
            ShowCredits();
            UIManager.Instance?.PlaySelectSound();
        }

        private void OnCharacterCreationComplete()
        {
            if (characterCreation == null)
                return;

            int slotIndex = characterCreation.TargetSlotIndex;
            string charName = characterCreation.CharacterName;
            string startingClass = characterCreation.SelectedClass != null
                ? characterCreation.SelectedClass.jobName
                : "";
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.CreateNewGame(slotIndex, charName, startingClass, 0);
            }

            StartNewGame(slotIndex);
        }

        private void OnCharacterCreationCancelled()
        {
            ShowSaveSelection();
        }

        private void OnSaveSlotClicked(SaveSlotUI slot)
        {
            if (slot.IsEmpty)
            {
                // Empty slot: show character creation
                if (characterCreation != null)
                {
                    SetState(MainMenuState.CharacterCreation);
                    if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
                    if (saveSelectionPanel != null) saveSelectionPanel.SetActive(false);
                    characterCreation.gameObject.SetActive(true);
                    characterCreation.ShowNameEntry(slot.SlotIndex);
                }
                else
                {
                    // Fallback: start new game directly
                    StartNewGame(slot.SlotIndex);
                }
            }
            else if (isSelectingSlotForNewGame)
            {
                // Filled slot while selecting for new game: show overwrite confirmation
                ShowOverwriteConfirm(slot.SlotIndex);
            }
            else
            {
                // Filled slot: load the save
                LoadGame(slot.SlotIndex);
            }
        }

        private void OnSaveSlotDeleteClicked(SaveSlotUI slot)
        {
            if (!slot.IsEmpty)
            {
                ShowDeleteConfirm(slot.SlotIndex);
            }
        }

        private void OnConfirmOverwriteClicked()
        {
            if (pendingSlotIndex >= 0)
            {
                isSelectingSlotForNewGame = false;

                if (characterCreation != null)
                {
                    // Route through character creation before starting new game
                    SetState(MainMenuState.CharacterCreation);
                    if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
                    if (saveSelectionPanel != null) saveSelectionPanel.SetActive(false);
                    if (overwriteConfirmPanel != null) overwriteConfirmPanel.SetActive(false);
                    characterCreation.gameObject.SetActive(true);
                    characterCreation.ShowNameEntry(pendingSlotIndex);
                }
                else
                {
                    // Fallback if character creation not available
                    StartNewGame(pendingSlotIndex);
                }
            }
            UIManager.Instance?.PlayConfirmSound();
        }

        private void OnCancelOverwriteClicked()
        {
            pendingSlotIndex = -1;
            isSelectingSlotForNewGame = false;

            if (overwriteConfirmPanel != null) overwriteConfirmPanel.SetActive(false);

            SetState(MainMenuState.SaveSelection);
            UIManager.Instance?.PlayCancelSound();
        }

        private void OnConfirmDeleteClicked()
        {
            if (pendingSlotIndex >= 0 && SaveManager.Instance != null)
            {
                SaveManager.Instance.DeleteSlot(pendingSlotIndex);
                pendingSlotIndex = -1;

                if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(false);

                SetState(MainMenuState.SaveSelection);
                RefreshSaveSlots();
            }
            UIManager.Instance?.PlayConfirmSound();
        }

        private void OnCancelDeleteClicked()
        {
            pendingSlotIndex = -1;

            if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(false);

            SetState(MainMenuState.SaveSelection);
            UIManager.Instance?.PlayCancelSound();
        }

        private void OnOptionsBackClicked()
        {
            ShowMainMenu();
            UIManager.Instance?.PlayCancelSound();
        }

        #endregion

        #region Game Actions

        /// <summary>
        /// Loads a saved game from the specified slot.
        /// </summary>
        public void LoadGame(int slotIndex)
        {
            Debug.Log($"[MainMenuController] Loading game from slot {slotIndex}");

            MusicManager.Instance?.Stop();
            SceneLoader.LoadGameplayWithSave(slotIndex);
        }

        /// <summary>
        /// Starts a new game in the specified slot.
        /// </summary>
        public void StartNewGame(int slotIndex)
        {
            Debug.Log($"[MainMenuController] Starting new game in slot {slotIndex}");

            MusicManager.Instance?.Stop();
            SceneLoader.LoadGameplayNewGame(slotIndex);
        }

        /// <summary>
        /// Quits the application.
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[MainMenuController] Quitting game");

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
            switch (currentState)
            {
                case MainMenuState.SaveSelection:
                    ShowMainMenu();
                    break;
                case MainMenuState.Options:
                    ShowMainMenu();
                    break;
                case MainMenuState.ConfirmOverwrite:
                    OnCancelOverwriteClicked();
                    break;
                case MainMenuState.ConfirmDelete:
                    OnCancelDeleteClicked();
                    break;
                case MainMenuState.Credits:
                    ShowMainMenu();
                    break;
                case MainMenuState.CharacterCreation:
                    ShowSaveSelection();
                    break;
            }
        }

        #endregion

        /// <summary>
        /// Repositions the title and menu buttons at runtime.
        /// Title gets double spacing from top, buttons move to the middle third.
        /// Also resizes the save selection panel to fit all slots properly.
        /// </summary>
        private void AdjustMainMenuLayout()
        {
            var safeArea = transform.Find("SafeArea");
            Transform parent = safeArea != null ? safeArea : transform;

            // Double the title spacing (move from -80 to -160 from top)
            var titleGroup = parent.Find("TitleGroup");
            if (titleGroup != null)
            {
                var titleRect = titleGroup.GetComponent<RectTransform>();
                if (titleRect != null)
                {
                    titleRect.anchoredPosition = new Vector2(0, -160);
                }
            }

            // Move menu buttons to the middle third
            if (mainMenuPanel != null)
            {
                var menuRect = mainMenuPanel.GetComponent<RectTransform>();
                if (menuRect != null)
                {
                    menuRect.anchorMin = new Vector2(0.5f, 0.4f);
                    menuRect.anchorMax = new Vector2(0.5f, 0.4f);
                    menuRect.pivot = new Vector2(0.5f, 0.5f);
                    menuRect.anchoredPosition = Vector2.zero;
                }
            }

            // Resize save selection panel to fit 5 slots comfortably
            if (saveSelectionPanel != null)
            {
                var panelRect = saveSelectionPanel.GetComponent<RectTransform>();
                if (panelRect != null)
                {
                    panelRect.sizeDelta = new Vector2(800, 700);
                }

                // Increase each slot's height
                var slots = saveSelectionPanel.GetComponentsInChildren<SaveSlotUI>(true);
                foreach (var slot in slots)
                {
                    var le = slot.GetComponent<LayoutElement>();
                    if (le != null)
                    {
                        le.preferredHeight = 100;
                    }
                }

                // Scale down navigation buttons (Back, New Game)
                ScaleButton(backFromSaveButton, 140, 36, 16);
                ScaleButton(newGameButton, 140, 36, 16);
            }

            // Scale down confirmation panel buttons
            ScaleButton(confirmOverwriteButton, 120, 36, 16);
            ScaleButton(cancelOverwriteButton, 120, 36, 16);
            ScaleButton(confirmDeleteButton, 120, 36, 16);
            ScaleButton(cancelDeleteButton, 120, 36, 16);
        }

        private static void ScaleButton(Button btn, float width, float height, float fontSize)
        {
            if (btn == null) return;
            var rt = btn.GetComponent<RectTransform>();
            if (rt != null)
                rt.sizeDelta = new Vector2(width, height);
            var tmp = btn.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
                tmp.fontSize = fontSize;
        }

        private void OnDestroy()
        {
            // Cleanup button listeners
            if (startGameButton != null) startGameButton.onClick.RemoveListener(OnStartGameClicked);
            if (optionsButton != null) optionsButton.onClick.RemoveListener(OnOptionsClicked);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuitClicked);
            if (newGameButton != null) newGameButton.onClick.RemoveListener(OnNewGameClicked);
            if (backFromSaveButton != null) backFromSaveButton.onClick.RemoveListener(OnBackFromSaveClicked);
            if (confirmOverwriteButton != null) confirmOverwriteButton.onClick.RemoveListener(OnConfirmOverwriteClicked);
            if (cancelOverwriteButton != null) cancelOverwriteButton.onClick.RemoveListener(OnCancelOverwriteClicked);
            if (confirmDeleteButton != null) confirmDeleteButton.onClick.RemoveListener(OnConfirmDeleteClicked);
            if (cancelDeleteButton != null) cancelDeleteButton.onClick.RemoveListener(OnCancelDeleteClicked);
            if (optionsBackButton != null) optionsBackButton.onClick.RemoveListener(OnOptionsBackClicked);
            if (creditsButton != null) creditsButton.onClick.RemoveListener(OnCreditsClicked);

            if (characterCreation != null)
            {
                characterCreation.OnCreationComplete -= OnCharacterCreationComplete;
                characterCreation.OnCreationCancelled -= OnCharacterCreationCancelled;
            }

            // Cleanup save slot listeners
            if (saveSlots != null)
            {
                foreach (var slot in saveSlots)
                {
                    if (slot != null)
                    {
                        slot.OnSlotClicked -= OnSaveSlotClicked;
                        slot.OnDeleteClicked -= OnSaveSlotDeleteClicked;
                    }
                }
            }
        }
    }
}
