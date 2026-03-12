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
            Credits,
            Highscores
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
        [SerializeField] private SaveSlotUI saveSlotPrefab;
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

        [Header("Highscores")]
        [SerializeField] private Button highscoresButton;
        [SerializeField] private GameObject highscoresPanel;
        [SerializeField] private HighscoresController highscoresController;

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
        private Transform titleGroup;
        private Button newGameMainButton;

        // Stored delegates for unsubscription
        private System.Action creditsBackHandler;
        private System.Action highscoresBackHandler;

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
            EnsureGraphicsSettings();
            EnsureMusicManager();
            EnsureHighscoreManager();
            EnsureCharacterCreation();
            EnsureHighscoresPanel();
            AddButtonSounds();
            AdjustMainMenuLayout();

            // Wire credits back button
            if (creditsController != null)
            {
                creditsBackHandler = ShowMainMenu;
                creditsController.OnBackPressed += creditsBackHandler;
            }

            // Wire highscores back button
            if (highscoresController != null)
            {
                highscoresBackHandler = ShowMainMenu;
                highscoresController.OnBackPressed += highscoresBackHandler;
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

        private void EnsureGraphicsSettings()
        {
            if (GraphicsSettings.Instance != null)
                return;

            var go = new GameObject("GraphicsSettings");
            go.AddComponent<GraphicsSettings>();
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

        private void EnsureHighscoreManager()
        {
            if (HighscoreManager.Instance != null)
                return;

            var go = new GameObject("HighscoreManager");
            go.AddComponent<HighscoreManager>();
        }

        private void EnsureHighscoresPanel()
        {
            if (highscoresController != null)
                return;

            var safeArea = transform.Find("SafeArea");
            Transform uiParent = safeArea != null ? safeArea : transform;

            highscoresController = HighscoresController.CreateRuntimeUI(uiParent);
            highscoresPanel = highscoresController.gameObject;

            highscoresBackHandler = ShowMainMenu;
            highscoresController.OnBackPressed += highscoresBackHandler;
        }

        private void EnsureCharacterCreation()
        {
            if (characterCreation == null)
            {
                var safeArea = transform.Find("SafeArea");
                Transform uiParent = safeArea != null ? safeArea : transform;

                characterCreation = CharacterCreationController.CreateRuntimeUI(uiParent);
            }

            // Always ensure exactly one subscription (safe to call on reuse)
            characterCreation.OnCreationComplete -= OnCharacterCreationComplete;
            characterCreation.OnCreationCancelled -= OnCharacterCreationCancelled;
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

            // Find highscores elements
            if (highscoresPanel == null)
            {
                var found = parent.Find("HighscoresPanel");
                if (found != null) highscoresPanel = found.gameObject;
            }

            if (highscoresPanel != null && highscoresController == null)
            {
                highscoresController = highscoresPanel.GetComponent<HighscoresController>();
            }

            if (highscoresButton == null && mainMenuPanel != null)
            {
                var found = mainMenuPanel.transform.Find("HighscoresButton");
                if (found != null) highscoresButton = found.GetComponent<Button>();
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

            if (highscoresButton != null)
                highscoresButton.onClick.AddListener(OnHighscoresClicked);

            // Character creation events are wired in EnsureCharacterCreation() to avoid double-subscription
        }

        private void SetupSaveSlots()
        {
            if (saveSlotContainer == null)
                return;

            // Load prefab
            if (saveSlotPrefab == null)
                saveSlotPrefab = Resources.Load<SaveSlotUI>("UI/SaveSlotUI");

            // Always use prefab when available — destroy any scene-baked slots
            if (saveSlotPrefab != null)
            {
                for (int i = saveSlotContainer.childCount - 1; i >= 0; i--)
                    Destroy(saveSlotContainer.GetChild(i).gameObject);

                saveSlots = new SaveSlotUI[SAVE_SLOT_COUNT];
                for (int i = 0; i < SAVE_SLOT_COUNT; i++)
                {
                    var slot = Instantiate(saveSlotPrefab, saveSlotContainer);
                    slot.name = $"SaveSlot_{i}";
                    saveSlots[i] = slot;
                }
            }
            else if (saveSlots == null || saveSlots.Length == 0)
            {
                // Fallback: use whatever is in the scene
                saveSlots = saveSlotContainer.GetComponentsInChildren<SaveSlotUI>(true);
            }

            // Initialize and wire events
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
            if (highscoresPanel != null) highscoresPanel.SetActive(false);
            if (characterCreation != null) characterCreation.gameObject.SetActive(false);
            if (titleGroup != null) titleGroup.gameObject.SetActive(true);

            // Hide Load Game if no saves exist; select appropriate first button
            bool hasSaves = HasAnySavedGame();
            if (startGameButton != null)
                startGameButton.gameObject.SetActive(hasSaves);

            if (hasSaves)
                SetSelected(startGameButton?.gameObject);
            else
                SetSelected(newGameMainButton?.gameObject);
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
            if (highscoresPanel != null) highscoresPanel.SetActive(false);
            if (characterCreation != null) characterCreation.gameObject.SetActive(false);
            if (titleGroup != null) titleGroup.gameObject.SetActive(false);

            RefreshSaveSlots();

            // Focus first slot's button for keyboard/controller navigation
            GameObject firstSlotBtn = null;
            if (saveSlots?.Length > 0 && saveSlots[0] != null)
            {
                var btn = saveSlots[0].GetComponentInChildren<Button>();
                if (btn != null) firstSlotBtn = btn.gameObject;
            }
            SetSelected(saveSelectionFirstSelected ?? firstSlotBtn);
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
            if (highscoresPanel != null) highscoresPanel.SetActive(false);
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
            if (highscoresPanel != null) highscoresPanel.SetActive(false);
            if (characterCreation != null) characterCreation.gameObject.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(true);
        }

        /// <summary>
        /// Shows the highscores panel.
        /// </summary>
        public void ShowHighscores()
        {
            SetState(MainMenuState.Highscores);

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (saveSelectionPanel != null) saveSelectionPanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (overwriteConfirmPanel != null) overwriteConfirmPanel.SetActive(false);
            if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (characterCreation != null) characterCreation.gameObject.SetActive(false);
            if (highscoresPanel != null) highscoresPanel.SetActive(true);
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
                    string waveInfo = !string.IsNullOrEmpty(info.FormattedWave) ? $" \u00b7 {info.FormattedWave}" : "";
                    overwriteSlotInfoText.text = $"{info.characterName} \u00b7 Lv. {info.playerLevel}{waveInfo}\n{info.FormattedPlayTime} \u00b7 {info.FormattedDate}";
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
                    string waveInfo = !string.IsNullOrEmpty(info.FormattedWave) ? $" \u00b7 {info.FormattedWave}" : "";
                    deleteSlotInfoText.text = $"{info.characterName} \u00b7 Lv. {info.playerLevel}{waveInfo}\n{info.FormattedPlayTime} \u00b7 {info.FormattedDate}";
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

        private bool HasAnySavedGame()
        {
            if (SaveManager.Instance == null) return false;
            for (int i = 0; i < SAVE_SLOT_COUNT; i++)
            {
                if (SaveManager.Instance.HasSaveInSlot(i))
                    return true;
            }
            return false;
        }

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

        private void OnHighscoresClicked()
        {
            ShowHighscores();
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
                // Capture appearance data if the player customized it
                CharacterAppearanceSaveData appearanceData = null;
                if (characterCreation.BuiltAppearance != null)
                    appearanceData = CharacterAppearanceSaveData.FromConfig(characterCreation.BuiltAppearance);

                SaveManager.Instance.CreateNewGame(slotIndex, charName, startingClass, 0, appearanceData);
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
                    characterCreation.ResetState();
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
                    characterCreation.ResetState();
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
                case MainMenuState.Highscores:
                    ShowMainMenu();
                    break;
                case MainMenuState.CharacterCreation:
                    ShowSaveSelection();
                    break;
            }
        }

        #endregion

        // Gothic palette
        private static readonly Color AgedGold = new Color(0.81f, 0.71f, 0.23f, 1f);
        private static readonly Color BoneWhite = new Color(0.93f, 0.89f, 0.82f, 1f);
        private static readonly Color DimWhite = new Color(0.93f, 0.89f, 0.82f, 0.5f);
        private static readonly Color DividerGold = new Color(0.81f, 0.71f, 0.23f, 0.3f);
        private static readonly Color BtnNormal = new Color(0.10f, 0.10f, 0.18f, 1f);
        private static readonly Color BtnSelected = new Color(0.55f, 0f, 0f, 1f);
        private static readonly Color BtnPress = new Color(0.08f, 0.08f, 0.14f, 1f);

        /// <summary>
        /// Applies Souls-like styling to the title screen and submenus.
        /// Title: large gold text, high on screen.
        /// Buttons: text-only, no backgrounds, gold highlight on hover.
        /// </summary>
        private void AdjustMainMenuLayout()
        {
            var safeArea = transform.Find("SafeArea");
            Transform parent = safeArea != null ? safeArea : transform;

            // --- Cache title group (hidden during save selection) ---
            titleGroup = parent.Find("TitleGroup");

            // --- Build the main menu as a single vertical stack: title, divider, buttons ---
            if (mainMenuPanel != null)
            {
                var menuRect = mainMenuPanel.GetComponent<RectTransform>();
                if (menuRect != null)
                {
                    menuRect.anchorMin = new Vector2(0.5f, 0.5f);
                    menuRect.anchorMax = new Vector2(0.5f, 0.5f);
                    menuRect.pivot = new Vector2(0.5f, 0.5f);
                    menuRect.anchoredPosition = new Vector2(0, 0);
                    menuRect.sizeDelta = new Vector2(0, 0);
                }

                // Panel width via LayoutElement so parent layouts can negotiate
                var panelLe = mainMenuPanel.GetComponent<LayoutElement>();
                if (panelLe == null)
                    panelLe = mainMenuPanel.AddComponent<LayoutElement>();
                panelLe.preferredWidth = 400;

                // VerticalLayoutGroup for the whole stack
                var vlg = mainMenuPanel.GetComponent<VerticalLayoutGroup>();
                if (vlg == null)
                    vlg = mainMenuPanel.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 6f;
                vlg.childAlignment = TextAnchor.MiddleCenter;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                var csf = mainMenuPanel.GetComponent<ContentSizeFitter>();
                if (csf == null)
                    csf = mainMenuPanel.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                // Move TitleGroup into MainMenuPanel (added first so it's at the top)
                if (titleGroup != null)
                {
                    titleGroup.SetParent(mainMenuPanel.transform, false);
                    titleGroup.gameObject.SetActive(true);
                    ResetRectForLayout(titleGroup.GetComponent<RectTransform>());

                    // Let the layout group control height
                    var titleLe = titleGroup.GetComponent<LayoutElement>();
                    if (titleLe == null)
                        titleLe = titleGroup.gameObject.AddComponent<LayoutElement>();
                    titleLe.preferredHeight = 80;

                    // Also reset the inner TMP_Text RectTransform to fill parent
                    var titleTmp = titleGroup.GetComponentInChildren<TMP_Text>(true);
                    if (titleTmp != null)
                    {
                        ResetRectForLayout(titleTmp.GetComponent<RectTransform>());
                        titleTmp.text = "SCHADENFREUDE";
                        titleTmp.fontSize = 52;
                        titleTmp.color = AgedGold;
                        titleTmp.characterSpacing = 8f;
                        titleTmp.alignment = TextAlignmentOptions.Center;
                        titleTmp.textWrappingMode = TextWrappingModes.NoWrap;
                        titleTmp.overflowMode = TextOverflowModes.Overflow;
                        titleTmp.gameObject.SetActive(true);
                    }
                }

                // Gold divider after title
                if (mainMenuPanel.transform.Find("TitleDivider") == null)
                {
                    var divGo = new GameObject("TitleDivider", typeof(RectTransform));
                    divGo.transform.SetParent(mainMenuPanel.transform, false);
                    var divImg = divGo.AddComponent<Image>();
                    divImg.color = DividerGold;
                    var divLe = divGo.AddComponent<LayoutElement>();
                    divLe.preferredHeight = 1;
                    divLe.flexibleWidth = 1;
                }

                // Spacer between divider and buttons
                if (mainMenuPanel.transform.Find("MenuSpacer") == null)
                {
                    var spacer = new GameObject("MenuSpacer", typeof(RectTransform));
                    spacer.transform.SetParent(mainMenuPanel.transform, false);
                    var spacerLe = spacer.AddComponent<LayoutElement>();
                    spacerLe.preferredHeight = 30;
                }

                // Create New Game button before existing buttons
                CreateNewGameMainMenuButton(mainMenuPanel.transform);

                // Style buttons: text-only, no background
                StyleSoulsMenuButton(startGameButton, "Load Game");
                StyleSoulsMenuButton(newGameMainButton, "New Game");
                StyleSoulsMenuButton(optionsButton, "Options");
                StyleSoulsMenuButton(quitButton, "Quit Game");

                // Ensure button order: Load Game, New Game, Options, Quit
                // Re-parent each button so it lands at the end in sequence
                Button[] menuOrder = { startGameButton, newGameMainButton, optionsButton, quitButton };
                foreach (var btn in menuOrder)
                {
                    if (btn != null)
                        btn.transform.SetAsLastSibling();
                }
            }

            // Save selection panel layout
            if (saveSelectionPanel != null)
            {
                SetupSaveSelectionPanelChrome();
            }

            // Gothic confirmation dialogs
            StyleConfirmDialog(overwriteConfirmPanel, overwriteWarningText,
                "This chronicle will be overwritten.",
                confirmOverwriteButton, cancelOverwriteButton);
            StyleConfirmDialog(deleteConfirmPanel, deleteWarningText,
                "This chronicle will be lost forever.",
                confirmDeleteButton, cancelDeleteButton);
        }

        /// <summary>
        /// Styles a button as Souls-like text-only menu item:
        /// transparent background, bone white text, gold on hover/select.
        /// </summary>
        private static void StyleSoulsMenuButton(Button btn, string label)
        {
            if (btn == null) return;

            // Reset RectTransform so VLG can control positioning
            ResetRectForLayout(btn.GetComponent<RectTransform>());

            // Set button height via LayoutElement
            var le = btn.GetComponent<LayoutElement>();
            if (le == null)
                le = btn.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 50;

            // Dark base background, crimson on highlight/select
            var btnImg = btn.GetComponent<Image>();
            if (btnImg != null)
            {
                btnImg.color = Color.white;
                btn.targetGraphic = btnImg;
            }

            var colors = btn.colors;
            colors.normalColor = BtnNormal;
            colors.highlightedColor = BtnSelected;
            colors.pressedColor = BtnPress;
            colors.selectedColor = BtnSelected;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;

            // Style the text
            var tmp = btn.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
            {
                ResetRectForLayout(tmp.GetComponent<RectTransform>());
                tmp.text = label;
                tmp.fontSize = 26;
                tmp.color = BoneWhite;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontStyle = FontStyles.Normal;
            }

            // Add hover/select color switcher
            var switcher = btn.GetComponent<SoulsButtonTextHighlight>();
            if (switcher == null)
                switcher = btn.gameObject.AddComponent<SoulsButtonTextHighlight>();
        }

        /// <summary>
        /// Creates the "New Game" button on the main menu if it doesn't exist.
        /// </summary>
        private void CreateNewGameMainMenuButton(Transform menuParent)
        {
            var existing = menuParent.Find("NewGameMainButton");
            if (existing != null)
            {
                newGameMainButton = existing.GetComponent<Button>();
                return;
            }

            var go = new GameObject("NewGameMainButton", typeof(RectTransform));
            go.transform.SetParent(menuParent, false);
            var img = go.AddComponent<Image>();
            img.color = Color.clear;
            newGameMainButton = go.AddComponent<Button>();
            newGameMainButton.targetGraphic = img;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "New Game";
            tmp.fontSize = 26;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = DimWhite;

            newGameMainButton.onClick.AddListener(OnNewGameMainClicked);
        }

        /// <summary>
        /// New Game from main menu: find first empty slot, go straight to character creation.
        /// If no empty slot, go to save selection so the player can overwrite.
        /// </summary>
        private void OnNewGameMainClicked()
        {
            EnsureCharacterCreation();

            int targetSlot = -1;
            if (SaveManager.Instance != null)
            {
                var allSlots = SaveManager.Instance.GetAllSlotInfo();
                for (int i = 0; i < allSlots.Length; i++)
                {
                    if (allSlots[i].isEmpty)
                    {
                        targetSlot = i;
                        break;
                    }
                }
            }

            if (targetSlot < 0)
            {
                // No empty slots — send to save selection to pick one to overwrite
                ShowSaveSelection();
                return;
            }

            // Go straight to character creation with the auto-selected slot
            SetState(MainMenuState.CharacterCreation);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (saveSelectionPanel != null) saveSelectionPanel.SetActive(false);
            if (titleGroup != null) titleGroup.gameObject.SetActive(false);
            characterCreation.ResetState();
            characterCreation.gameObject.SetActive(true);
            characterCreation.ShowNameEntry(targetSlot);
            UIManager.Instance?.PlaySelectSound();
        }

        /// <summary>
        /// Applies gothic styling to the save selection panel.
        /// Hides legacy scene-baked elements and styles the container.
        /// </summary>
        private void SetupSaveSelectionPanelChrome()
        {
            // Full-screen stretch
            var panelRect = saveSelectionPanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
            }

            // Dark background matching other panels
            var panelImg = saveSelectionPanel.GetComponent<Image>();
            if (panelImg == null)
                panelImg = saveSelectionPanel.AddComponent<Image>();
            panelImg.color = new Color(0.05f, 0.04f, 0.07f, 0.95f);

            // Hide legacy scene-baked headers
            foreach (var name in new[] { "Header", "TitleText", "GothicTitle", "TitleDivider" })
            {
                var legacy = saveSelectionPanel.transform.Find(name);
                if (legacy != null)
                    legacy.gameObject.SetActive(false);
            }

            // Remove old VLG on the panel itself (we'll add our own content column)
            var oldVlg = saveSelectionPanel.GetComponent<VerticalLayoutGroup>();
            if (oldVlg != null) Destroy(oldVlg);
            var oldCsf = saveSelectionPanel.GetComponent<ContentSizeFitter>();
            if (oldCsf != null) Destroy(oldCsf);

            // Create centered content column (matches CharacterCreation style)
            var contentGo = saveSelectionPanel.transform.Find("SaveContent");
            if (contentGo == null)
            {
                var go = new GameObject("SaveContent", typeof(RectTransform));
                go.transform.SetParent(saveSelectionPanel.transform, false);
                go.transform.SetAsFirstSibling();
                contentGo = go.transform;

                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(600f, 0f);

                var vlg = go.AddComponent<VerticalLayoutGroup>();
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.spacing = 8f;
                vlg.padding = new RectOffset(20, 20, 20, 20);

                var csf = go.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // Gold title
                var titleGo = new GameObject("Title", typeof(RectTransform));
                titleGo.transform.SetParent(go.transform, false);
                var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
                titleTmp.text = "Choose Thy Chronicle";
                titleTmp.fontSize = 34f;
                titleTmp.color = AgedGold;
                titleTmp.alignment = TextAlignmentOptions.Center;
                var titleLe = titleGo.AddComponent<LayoutElement>();
                titleLe.preferredHeight = 50f;

                // Spacer
                var spacerGo = new GameObject("Spacer", typeof(RectTransform));
                spacerGo.transform.SetParent(go.transform, false);
                var spacerLe = spacerGo.AddComponent<LayoutElement>();
                spacerLe.preferredHeight = 10f;
                spacerLe.flexibleWidth = 1f;
            }

            // Reparent slot container into content column
            if (saveSlotContainer != null)
            {
                saveSlotContainer.SetParent(contentGo, false);
                ResetRectForLayout(saveSlotContainer.GetComponent<RectTransform>());

                var slotVlg = saveSlotContainer.GetComponent<VerticalLayoutGroup>();
                if (slotVlg == null)
                    slotVlg = saveSlotContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                slotVlg.spacing = 6f;
                slotVlg.padding = new RectOffset(0, 0, 0, 0);
                slotVlg.childAlignment = TextAnchor.UpperCenter;
                slotVlg.childControlWidth = true;
                slotVlg.childControlHeight = true;
                slotVlg.childForceExpandWidth = true;
                slotVlg.childForceExpandHeight = false;

                var slotCsf = saveSlotContainer.GetComponent<ContentSizeFitter>();
                if (slotCsf == null)
                    slotCsf = saveSlotContainer.gameObject.AddComponent<ContentSizeFitter>();
                slotCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // Reparent and style nav buttons into content column (guard against double-init)
            if (contentGo.Find("NavRow") != null) return;

            var navSpacer = new GameObject("NavSpacer", typeof(RectTransform));
            navSpacer.transform.SetParent(contentGo, false);
            var navSpacerLe = navSpacer.AddComponent<LayoutElement>();
            navSpacerLe.preferredHeight = 16f;
            navSpacerLe.flexibleWidth = 1f;

            var navRow = new GameObject("NavRow", typeof(RectTransform));
            navRow.transform.SetParent(contentGo, false);
            var hlg = navRow.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.spacing = 20f;
            var navLe = navRow.AddComponent<LayoutElement>();
            navLe.preferredHeight = 50f;

            if (backFromSaveButton != null)
            {
                backFromSaveButton.transform.SetParent(navRow.transform, false);
                StyleSaveNavButton(backFromSaveButton, "Back", 180f);
            }
            if (newGameButton != null)
            {
                newGameButton.transform.SetParent(navRow.transform, false);
                StyleSaveNavButton(newGameButton, "New Game", 200f);
            }

            // Style each save slot's button to match the dark/red scheme
            if (saveSlots != null)
            {
                foreach (var slot in saveSlots)
                    StyleSaveSlotButton(slot);
            }
        }

        private static void StyleSaveNavButton(Button btn, string label, float width)
        {
            if (btn == null) return;

            var le = btn.GetComponent<LayoutElement>();
            if (le == null) le = btn.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = 50f;

            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = Color.white;
                btn.targetGraphic = img;
            }

            var colors = btn.colors;
            colors.normalColor = BtnNormal;
            colors.highlightedColor = BtnSelected;
            colors.pressedColor = BtnPress;
            colors.selectedColor = BtnSelected;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;

            var tmp = btn.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = label;
                tmp.fontSize = 22f;
                tmp.color = BoneWhite;
                tmp.alignment = TextAlignmentOptions.Center;
            }
        }

        private static void StyleSaveSlotButton(SaveSlotUI slot)
        {
            if (slot == null) return;

            // Set height via LayoutElement
            var le = slot.GetComponent<LayoutElement>();
            if (le == null) le = slot.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 70f;

            // Style the main slot button with dark/red scheme
            var btn = slot.GetComponentInChildren<Button>();
            if (btn != null)
            {
                var img = btn.GetComponent<Image>();
                if (img != null)
                {
                    img.color = Color.white;
                    btn.targetGraphic = img;
                }

                var colors = btn.colors;
                colors.normalColor = BtnNormal;
                colors.highlightedColor = BtnSelected;
                colors.pressedColor = BtnPress;
                colors.selectedColor = BtnSelected;
                colors.fadeDuration = 0.1f;
                btn.colors = colors;
            }

            // Brighten text for readability against dark bg
            var texts = slot.GetComponentsInChildren<TMP_Text>(true);
            foreach (var tmp in texts)
            {
                if (tmp.color.a < 0.6f)
                    tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, 1f);
            }
        }

        /// <summary>
        /// Resets a RectTransform to stretch-fill its parent with zero offsets.
        /// This clears scene-baked anchors/positions so LayoutGroups can control placement.
        /// </summary>
        private static void ResetRectForLayout(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
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

        private static void StyleGothicButton(Button btn, float width, float height, float fontSize, bool isCrimson)
        {
            if (btn == null) return;
            var rt = btn.GetComponent<RectTransform>();
            if (rt != null)
                rt.sizeDelta = new Vector2(width, height);

            var btnColors = btn.colors;
            if (isCrimson)
            {
                btnColors.normalColor = new Color(0.55f, 0f, 0f, 1f);
                btnColors.highlightedColor = new Color(0.65f, 0.05f, 0.05f, 1f);
                btnColors.pressedColor = new Color(0.40f, 0f, 0f, 1f);
                btnColors.selectedColor = new Color(0.65f, 0.05f, 0.05f, 1f);
            }
            else
            {
                btnColors.normalColor = new Color(0.10f, 0.10f, 0.44f, 1f);
                btnColors.highlightedColor = new Color(0.15f, 0.15f, 0.55f, 1f);
                btnColors.pressedColor = new Color(0.08f, 0.08f, 0.35f, 1f);
                btnColors.selectedColor = new Color(0.15f, 0.15f, 0.55f, 1f);
            }
            btn.colors = btnColors;

            var tmp = btn.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
            {
                tmp.fontSize = fontSize;
                tmp.color = isCrimson
                    ? new Color(0.81f, 0.71f, 0.23f, 1f)
                    : new Color(0.93f, 0.89f, 0.82f, 1f);
            }
        }

        private static void StyleConfirmDialog(GameObject panel, TMP_Text warningText,
            string warningMessage, Button confirmBtn, Button cancelBtn)
        {
            if (panel == null) return;

            // Obsidian background
            var panelImg = panel.GetComponent<Image>();
            if (panelImg != null)
                panelImg.color = new Color(0.06f, 0.05f, 0.08f, 0.97f);

            // Warning text styling
            if (warningText != null)
            {
                warningText.text = warningMessage;
                warningText.color = new Color(0.93f, 0.89f, 0.82f, 1f);
                warningText.fontSize = 22;
            }

            // Slot info text styling
            var slotInfoText = panel.transform.Find("SlotInfoText")?.GetComponent<TMP_Text>();
            if (slotInfoText != null)
            {
                slotInfoText.color = new Color(0.65f, 0.60f, 0.52f, 1f);
                slotInfoText.fontSize = 18;
            }

            // Confirm button: crimson with gold text
            StyleGothicButton(confirmBtn, 140, 40, 17, true);
            // Cancel button: midnight blue with bone text
            StyleGothicButton(cancelBtn, 140, 40, 17, false);
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
            if (highscoresButton != null) highscoresButton.onClick.RemoveListener(OnHighscoresClicked);
            if (newGameMainButton != null) newGameMainButton.onClick.RemoveListener(OnNewGameMainClicked);

            if (characterCreation != null)
            {
                characterCreation.OnCreationComplete -= OnCharacterCreationComplete;
                characterCreation.OnCreationCancelled -= OnCharacterCreationCancelled;
            }

            if (creditsController != null && creditsBackHandler != null)
                creditsController.OnBackPressed -= creditsBackHandler;
            if (highscoresController != null && highscoresBackHandler != null)
                highscoresController.OnBackPressed -= highscoresBackHandler;

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
