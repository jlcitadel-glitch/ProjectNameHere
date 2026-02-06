using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Handles the character creation flow: Name -> Class -> Appearance.
    /// Works with MainMenuController to manage state transitions.
    /// </summary>
    public class CharacterCreationController : MonoBehaviour
    {
        public enum CreationStep
        {
            NameEntry,
            ClassSelection,
            AppearanceSelection
        }

        [Header("Name Entry")]
        [SerializeField] private GameObject nameEntryPanel;
        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private Button nameConfirmButton;
        [SerializeField] private Button nameBackButton;
        [SerializeField] private TMP_Text nameErrorText;
        [SerializeField] private int maxNameLength = 16;

        [Header("Class Selection")]
        [SerializeField] private GameObject classSelectionPanel;
        [SerializeField] private Button warriorButton;
        [SerializeField] private Button mageButton;
        [SerializeField] private Button rogueButton;
        [SerializeField] private Button classBackButton;
        [SerializeField] private Button classConfirmButton;
        [SerializeField] private TMP_Text classDescriptionText;
        [SerializeField] private TMP_Text classStatsPreviewText;
        [SerializeField] private TMP_Text classNameText;

        [Header("Appearance Selection")]
        [SerializeField] private GameObject appearancePanel;
        [SerializeField] private Button appearanceLeftButton;
        [SerializeField] private Button appearanceRightButton;
        [SerializeField] private Button appearanceBackButton;
        [SerializeField] private Button appearanceConfirmButton;
        [SerializeField] private Image appearancePreview;
        [SerializeField] private Sprite[] appearanceSprites;

        [Header("Job Data References")]
        [SerializeField] private JobClassData warriorData;
        [SerializeField] private JobClassData mageData;
        [SerializeField] private JobClassData rogueData;

        // Creation data
        private string characterName = "";
        private JobClassData selectedClass;
        private int selectedAppearanceIndex;
        private int targetSlotIndex = -1;
        private CreationStep currentStep;

        // Results
        public string CharacterName => characterName;
        public JobClassData SelectedClass => selectedClass;
        public int SelectedAppearanceIndex => selectedAppearanceIndex;
        public int TargetSlotIndex => targetSlotIndex;
        public CreationStep CurrentStep => currentStep;

        public event Action OnCreationComplete;
        public event Action OnCreationCancelled;

        private void Awake()
        {
            SetupButtons();
        }

        private void SetupButtons()
        {
            // Name entry
            if (nameConfirmButton != null)
                nameConfirmButton.onClick.AddListener(OnNameConfirm);
            if (nameBackButton != null)
                nameBackButton.onClick.AddListener(OnNameBack);
            if (nameInputField != null)
            {
                nameInputField.characterLimit = maxNameLength;
                nameInputField.onSubmit.AddListener(_ => OnNameConfirm());
            }

            // Class selection
            if (warriorButton != null)
                warriorButton.onClick.AddListener(() => SelectClass(warriorData));
            if (mageButton != null)
                mageButton.onClick.AddListener(() => SelectClass(mageData));
            if (rogueButton != null)
                rogueButton.onClick.AddListener(() => SelectClass(rogueData));
            if (classBackButton != null)
                classBackButton.onClick.AddListener(OnClassBack);
            if (classConfirmButton != null)
                classConfirmButton.onClick.AddListener(OnClassConfirm);

            // Appearance selection
            if (appearanceLeftButton != null)
                appearanceLeftButton.onClick.AddListener(() => CycleAppearance(-1));
            if (appearanceRightButton != null)
                appearanceRightButton.onClick.AddListener(() => CycleAppearance(1));
            if (appearanceBackButton != null)
                appearanceBackButton.onClick.AddListener(OnAppearanceBack);
            if (appearanceConfirmButton != null)
                appearanceConfirmButton.onClick.AddListener(OnAppearanceConfirm);
        }

        /// <summary>
        /// Begins the character creation flow for the specified save slot.
        /// </summary>
        public void ShowNameEntry(int slotIndex)
        {
            targetSlotIndex = slotIndex;
            currentStep = CreationStep.NameEntry;
            characterName = "";
            selectedClass = null;
            selectedAppearanceIndex = 0;

            SetPanelActive(nameEntryPanel, true);
            SetPanelActive(classSelectionPanel, false);
            SetPanelActive(appearancePanel, false);

            if (nameInputField != null)
            {
                nameInputField.text = "";
                nameInputField.Select();
                nameInputField.ActivateInputField();
            }

            if (nameErrorText != null)
                nameErrorText.gameObject.SetActive(false);
        }

        #region Name Entry

        private void OnNameConfirm()
        {
            string input = nameInputField != null ? nameInputField.text.Trim() : "";

            if (string.IsNullOrEmpty(input))
            {
                ShowNameError("Please enter a name.");
                return;
            }

            if (input.Length > maxNameLength)
            {
                ShowNameError($"Name must be {maxNameLength} characters or less.");
                return;
            }

            characterName = input;
            UIManager.Instance?.PlaySelectSound();
            ShowClassSelection();
        }

        private void OnNameBack()
        {
            UIManager.Instance?.PlayCancelSound();
            HideAllPanels();
            OnCreationCancelled?.Invoke();
        }

        private void ShowNameError(string message)
        {
            if (nameErrorText != null)
            {
                nameErrorText.text = message;
                nameErrorText.gameObject.SetActive(true);
            }
        }

        #endregion

        #region Class Selection

        private void ShowClassSelection()
        {
            currentStep = CreationStep.ClassSelection;
            SetPanelActive(nameEntryPanel, false);
            SetPanelActive(classSelectionPanel, true);
            SetPanelActive(appearancePanel, false);

            // Disable confirm until a class is selected
            if (classConfirmButton != null)
                classConfirmButton.interactable = selectedClass != null;

            // Clear preview text when no class is selected
            if (selectedClass == null)
            {
                if (classNameText != null)
                    classNameText.text = "Select a Class";
                if (classDescriptionText != null)
                    classDescriptionText.text = "Choose your path.";
                if (classStatsPreviewText != null)
                    classStatsPreviewText.text = "";
            }
        }

        private void SelectClass(JobClassData classData)
        {
            if (classData == null)
                return;

            selectedClass = classData;
            UpdateClassPreview(classData);
            UIManager.Instance?.PlaySelectSound();

            if (classConfirmButton != null)
                classConfirmButton.interactable = true;
        }

        private void UpdateClassPreview(JobClassData classData)
        {
            if (classNameText != null)
                classNameText.text = classData.jobName;

            if (classDescriptionText != null)
                classDescriptionText.text = classData.description;

            if (classStatsPreviewText != null)
            {
                classStatsPreviewText.text =
                    $"Growth per Level:\n" +
                    $"  STR +{classData.strPerLevel}  INT +{classData.intPerLevel}  AGI +{classData.agiPerLevel}\n\n" +
                    $"Modifiers:\n" +
                    $"  ATK x{classData.attackModifier:F1}  MAG x{classData.magicModifier:F1}  DEF x{classData.defenseModifier:F1}\n\n" +
                    $"SP per Level: {classData.spPerLevel}";
            }
        }

        private void OnClassConfirm()
        {
            if (selectedClass == null)
                return;

            UIManager.Instance?.PlaySelectSound();
            ShowAppearanceSelection();
        }

        private void OnClassBack()
        {
            UIManager.Instance?.PlayCancelSound();
            ShowNameEntry(targetSlotIndex);
            if (nameInputField != null)
                nameInputField.text = characterName;
        }

        #endregion

        #region Appearance Selection

        private void ShowAppearanceSelection()
        {
            currentStep = CreationStep.AppearanceSelection;
            SetPanelActive(nameEntryPanel, false);
            SetPanelActive(classSelectionPanel, false);
            SetPanelActive(appearancePanel, true);

            selectedAppearanceIndex = 0;
            UpdateAppearancePreview();
        }

        private void CycleAppearance(int direction)
        {
            if (appearanceSprites == null || appearanceSprites.Length == 0)
                return;

            selectedAppearanceIndex += direction;

            if (selectedAppearanceIndex < 0)
                selectedAppearanceIndex = appearanceSprites.Length - 1;
            else if (selectedAppearanceIndex >= appearanceSprites.Length)
                selectedAppearanceIndex = 0;

            UpdateAppearancePreview();
            UIManager.Instance?.PlaySelectSound();
        }

        private void UpdateAppearancePreview()
        {
            if (appearancePreview != null && appearanceSprites != null
                && selectedAppearanceIndex < appearanceSprites.Length)
            {
                appearancePreview.sprite = appearanceSprites[selectedAppearanceIndex];
            }
        }

        private void OnAppearanceConfirm()
        {
            UIManager.Instance?.PlayConfirmSound();
            HideAllPanels();
            OnCreationComplete?.Invoke();
        }

        private void OnAppearanceBack()
        {
            UIManager.Instance?.PlayCancelSound();
            ShowClassSelection();
        }

        #endregion

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
                panel.SetActive(active);
        }

        private void HideAllPanels()
        {
            SetPanelActive(nameEntryPanel, false);
            SetPanelActive(classSelectionPanel, false);
            SetPanelActive(appearancePanel, false);
        }

        #region Runtime UI Builder

        // Colors for runtime-built UI
        private static readonly Color PanelBg = new Color(0.08f, 0.08f, 0.1f, 0.97f);
        private static readonly Color BtnNormal = new Color(0.2f, 0.2f, 0.25f, 1f);
        private static readonly Color BtnHover = new Color(0.3f, 0.3f, 0.35f, 1f);
        private static readonly Color BtnPress = new Color(0.15f, 0.15f, 0.2f, 1f);
        private static readonly Color TextCol = new Color(0.9f, 0.85f, 0.75f, 1f);
        private static readonly Color InputBg = new Color(0.12f, 0.12f, 0.15f, 1f);

        /// <summary>
        /// Creates the full character creation UI at runtime.
        /// Call when no CharacterCreationPanel exists in the scene.
        /// </summary>
        public static CharacterCreationController CreateRuntimeUI(Transform parent)
        {
            var rootGo = MakeUIObject("CharacterCreationPanel", parent);
            Stretch(rootGo);
            rootGo.SetActive(false);

            var ctrl = rootGo.AddComponent<CharacterCreationController>();

            BuildNamePanel(ctrl, rootGo.transform);
            BuildClassPanel(ctrl, rootGo.transform);
            BuildAppearancePanel(ctrl, rootGo.transform);
            FindJobData(ctrl);

            // Awake already ran with null refs (no-op), so bind listeners now
            ctrl.SetupButtons();

            return ctrl;
        }

        private static void BuildNamePanel(CharacterCreationController ctrl, Transform parent)
        {
            var panel = MakeDarkPanel(parent, "NameEntryPanel");
            ctrl.nameEntryPanel = panel;

            var content = MakeContentColumn(panel.transform);
            MakeLabel(content.transform, "Name Your Character", 36f);
            MakeSpacer(content.transform, 20f);

            ctrl.nameInputField = MakeInputField(content.transform);
            MakeSpacer(content.transform, 8f);

            var err = MakeLabel(content.transform, "", 18f);
            err.color = new Color(0.9f, 0.3f, 0.3f, 1f);
            err.gameObject.SetActive(false);
            ctrl.nameErrorText = err;
            MakeSpacer(content.transform, 20f);

            var row = MakeHRow(content.transform, 10f, 50f);
            ctrl.nameBackButton = MakeButton(row.transform, "Back", 150f);
            ctrl.nameConfirmButton = MakeButton(row.transform, "Next", 150f);
        }

        private static void BuildClassPanel(CharacterCreationController ctrl, Transform parent)
        {
            var panel = MakeDarkPanel(parent, "ClassSelectionPanel");
            panel.SetActive(false);
            ctrl.classSelectionPanel = panel;

            var content = MakeContentColumn(panel.transform);
            MakeLabel(content.transform, "Choose Your Class", 36f);
            MakeSpacer(content.transform, 20f);

            var classRow = MakeHRow(content.transform, 15f, 65f);
            ctrl.warriorButton = MakeButton(classRow.transform, "Warrior", 160f, 60f);
            ctrl.mageButton = MakeButton(classRow.transform, "Mage", 160f, 60f);
            ctrl.rogueButton = MakeButton(classRow.transform, "Rogue", 160f, 60f);
            MakeSpacer(content.transform, 20f);

            ctrl.classNameText = MakeLabel(content.transform, "Select a Class", 28f);
            MakeSpacer(content.transform, 8f);
            ctrl.classDescriptionText = MakeLabel(content.transform, "Choose your path.", 20f);
            MakeAutoHeight(ctrl.classDescriptionText.gameObject);
            MakeSpacer(content.transform, 8f);
            ctrl.classStatsPreviewText = MakeLabel(content.transform, "", 18f);
            MakeAutoHeight(ctrl.classStatsPreviewText.gameObject);
            MakeSpacer(content.transform, 20f);

            var navRow = MakeHRow(content.transform, 10f, 50f);
            ctrl.classBackButton = MakeButton(navRow.transform, "Back", 150f);
            ctrl.classConfirmButton = MakeButton(navRow.transform, "Next", 150f);
            ctrl.classConfirmButton.interactable = false;
        }

        private static void BuildAppearancePanel(CharacterCreationController ctrl, Transform parent)
        {
            var panel = MakeDarkPanel(parent, "AppearancePanel");
            panel.SetActive(false);
            ctrl.appearancePanel = panel;

            var content = MakeContentColumn(panel.transform);
            MakeLabel(content.transform, "Choose Appearance", 36f);
            MakeSpacer(content.transform, 20f);

            // Preview image placeholder
            var previewGo = MakeUIObject("Preview", content.transform);
            var previewImg = previewGo.AddComponent<Image>();
            previewImg.color = new Color(0.25f, 0.25f, 0.3f, 1f);
            previewImg.preserveAspect = true;
            var previewLayout = previewGo.AddComponent<LayoutElement>();
            previewLayout.preferredWidth = 128f;
            previewLayout.preferredHeight = 128f;
            ctrl.appearancePreview = previewImg;

            MakeLabel(content.transform, "Default Appearance", 18f);
            MakeSpacer(content.transform, 10f);

            var cycleRow = MakeHRow(content.transform, 10f, 50f);
            ctrl.appearanceLeftButton = MakeButton(cycleRow.transform, "<", 60f);
            ctrl.appearanceRightButton = MakeButton(cycleRow.transform, ">", 60f);
            MakeSpacer(content.transform, 20f);

            var navRow = MakeHRow(content.transform, 10f, 50f);
            ctrl.appearanceBackButton = MakeButton(navRow.transform, "Back", 150f);
            ctrl.appearanceConfirmButton = MakeButton(navRow.transform, "Start Game", 200f);
        }

        private static void FindJobData(CharacterCreationController ctrl)
        {
            var allJobs = Resources.FindObjectsOfTypeAll<JobClassData>();
            foreach (var job in allJobs)
            {
                if (string.IsNullOrEmpty(job.jobName)) continue;
                string n = job.jobName.ToLower();
                if (n.Contains("warrior") && ctrl.warriorData == null) ctrl.warriorData = job;
                else if (n.Contains("mage") && ctrl.mageData == null) ctrl.mageData = job;
                else if (n.Contains("rogue") && ctrl.rogueData == null) ctrl.rogueData = job;
            }

            // Runtime fallbacks if assets not found (e.g. in builds)
            if (ctrl.warriorData == null)
            {
                ctrl.warriorData = ScriptableObject.CreateInstance<JobClassData>();
                ctrl.warriorData.jobId = "warrior";
                ctrl.warriorData.jobName = "Warrior";
                ctrl.warriorData.description = "A mighty fighter who excels in melee combat and physical strength.";
                ctrl.warriorData.attackModifier = 1.3f;
                ctrl.warriorData.magicModifier = 0.7f;
                ctrl.warriorData.defenseModifier = 1.2f;
                ctrl.warriorData.strPerLevel = 3;
                ctrl.warriorData.intPerLevel = 1;
                ctrl.warriorData.agiPerLevel = 1;
                ctrl.warriorData.spPerLevel = 3;
            }

            if (ctrl.mageData == null)
            {
                ctrl.mageData = ScriptableObject.CreateInstance<JobClassData>();
                ctrl.mageData.jobId = "mage";
                ctrl.mageData.jobName = "Mage";
                ctrl.mageData.description = "A spell-caster who wields devastating arcane magic.";
                ctrl.mageData.attackModifier = 0.7f;
                ctrl.mageData.magicModifier = 1.3f;
                ctrl.mageData.defenseModifier = 0.8f;
                ctrl.mageData.strPerLevel = 1;
                ctrl.mageData.intPerLevel = 3;
                ctrl.mageData.agiPerLevel = 1;
                ctrl.mageData.spPerLevel = 3;
            }

            if (ctrl.rogueData == null)
            {
                ctrl.rogueData = ScriptableObject.CreateInstance<JobClassData>();
                ctrl.rogueData.jobId = "rogue";
                ctrl.rogueData.jobName = "Rogue";
                ctrl.rogueData.description = "A swift and cunning fighter who relies on speed and precision.";
                ctrl.rogueData.attackModifier = 1.0f;
                ctrl.rogueData.magicModifier = 0.9f;
                ctrl.rogueData.defenseModifier = 0.9f;
                ctrl.rogueData.strPerLevel = 1;
                ctrl.rogueData.intPerLevel = 1;
                ctrl.rogueData.agiPerLevel = 3;
                ctrl.rogueData.spPerLevel = 3;
            }
        }

        // --- UI element helpers ---

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
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static GameObject MakeDarkPanel(Transform parent, string name)
        {
            var go = MakeUIObject(name, parent);
            Stretch(go);
            var img = go.AddComponent<Image>();
            img.color = PanelBg;
            return go;
        }

        private static GameObject MakeContentColumn(Transform parent)
        {
            var go = MakeUIObject("Content", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(600f, 0f);

            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 5f;
            vlg.padding = new RectOffset(20, 20, 20, 20);

            var csf = go.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return go;
        }

        private static TMP_Text MakeLabel(Transform parent, string text, float fontSize)
        {
            var go = MakeUIObject("Label", parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = TextCol;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            var layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = 500f;
            layout.preferredHeight = fontSize * 1.5f;

            return tmp;
        }

        private static Button MakeButton(Transform parent, string label, float width, float height = 45f)
        {
            var go = MakeUIObject(label + "Button", parent);
            var img = go.AddComponent<Image>();
            img.color = Color.white;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = BtnNormal;
            colors.highlightedColor = BtnHover;
            colors.pressedColor = BtnPress;
            colors.selectedColor = BtnHover;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;

            var layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;

            var textGo = MakeUIObject("Text", go.transform);
            Stretch(textGo);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 22f;
            tmp.color = TextCol;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        private static GameObject MakeHRow(Transform parent, float spacing, float height)
        {
            var go = MakeUIObject("Row", parent);
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.spacing = spacing;

            var layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = height;

            var csf = go.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            return go;
        }

        private static void MakeAutoHeight(GameObject go)
        {
            var layout = go.GetComponent<LayoutElement>();
            if (layout != null)
                layout.preferredHeight = -1;

            var csf = go.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void MakeSpacer(Transform parent, float height)
        {
            var go = MakeUIObject("Spacer", parent);
            var layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.flexibleWidth = 1f;
        }

        private static TMP_InputField MakeInputField(Transform parent)
        {
            var go = MakeUIObject("NameInputField", parent);
            var bgImg = go.AddComponent<Image>();
            bgImg.color = InputBg;
            var layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = 400f;
            layout.preferredHeight = 50f;

            // Text area with mask
            var textArea = MakeUIObject("Text Area", go.transform);
            Stretch(textArea);
            var taRt = textArea.GetComponent<RectTransform>();
            taRt.offsetMin = new Vector2(10f, 5f);
            taRt.offsetMax = new Vector2(-10f, -5f);
            textArea.AddComponent<RectMask2D>();

            // Input text
            var textGo = MakeUIObject("Text", textArea.transform);
            Stretch(textGo);
            var textTmp = textGo.AddComponent<TextMeshProUGUI>();
            textTmp.fontSize = 24f;
            textTmp.color = TextCol;
            textTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Placeholder
            var phGo = MakeUIObject("Placeholder", textArea.transform);
            Stretch(phGo);
            var phTmp = phGo.AddComponent<TextMeshProUGUI>();
            phTmp.text = "Enter name...";
            phTmp.fontSize = 24f;
            phTmp.fontStyle = FontStyles.Italic;
            phTmp.color = new Color(0.5f, 0.5f, 0.55f, 0.6f);
            phTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Wire TMP_InputField
            var inputField = go.AddComponent<TMP_InputField>();
            inputField.textComponent = textTmp;
            inputField.placeholder = phTmp;
            inputField.textViewport = taRt;
            inputField.pointSize = 24;
            inputField.characterLimit = 16;
            inputField.contentType = TMP_InputField.ContentType.Standard;
            inputField.lineType = TMP_InputField.LineType.SingleLine;

            return inputField;
        }

        #endregion

        private void OnDestroy()
        {
            if (nameConfirmButton != null) nameConfirmButton.onClick.RemoveAllListeners();
            if (nameBackButton != null) nameBackButton.onClick.RemoveAllListeners();
            if (warriorButton != null) warriorButton.onClick.RemoveAllListeners();
            if (mageButton != null) mageButton.onClick.RemoveAllListeners();
            if (rogueButton != null) rogueButton.onClick.RemoveAllListeners();
            if (classBackButton != null) classBackButton.onClick.RemoveAllListeners();
            if (classConfirmButton != null) classConfirmButton.onClick.RemoveAllListeners();
            if (appearanceLeftButton != null) appearanceLeftButton.onClick.RemoveAllListeners();
            if (appearanceRightButton != null) appearanceRightButton.onClick.RemoveAllListeners();
            if (appearanceBackButton != null) appearanceBackButton.onClick.RemoveAllListeners();
            if (appearanceConfirmButton != null) appearanceConfirmButton.onClick.RemoveAllListeners();
        }
    }
}
