using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Handles the character creation flow: Name -> Class Selection (with preview) -> Start.
    /// Works with MainMenuController to manage state transitions.
    /// </summary>
    public class CharacterCreationController : MonoBehaviour
    {
        public enum CreationStep
        {
            NameEntry,
            ClassSelection
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

        [Header("Class Preview Images")]
        [SerializeField] private Image warriorPreviewImage;
        [SerializeField] private Image magePreviewImage;
        [SerializeField] private Image roguePreviewImage;

        [Header("Job Data References")]
        [SerializeField] private JobClassData warriorData;
        [SerializeField] private JobClassData mageData;
        [SerializeField] private JobClassData rogueData;

        // Animated preview components (added at runtime)
        private UIAnimatedSprite warriorAnimSprite;
        private UIAnimatedSprite mageAnimSprite;
        private UIAnimatedSprite rogueAnimSprite;

        // Creation data
        private string characterName = "";
        private JobClassData selectedClass;
        private int targetSlotIndex = -1;
        private CreationStep currentStep;
        private bool classSpritesLoaded;

        // Results
        public string CharacterName => characterName;
        public JobClassData SelectedClass => selectedClass;
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

            SetPanelActive(nameEntryPanel, true);
            SetPanelActive(classSelectionPanel, false);

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

            // Load class preview sprites on first show (deferred so assets are in memory)
            if (!classSpritesLoaded)
            {
                classSpritesLoaded = true;
                TryLoadClassSprites();
            }

            // Set initial static previews from JobClassData
            SetInitialPreviews();

            // Disable confirm until a class is selected
            if (classConfirmButton != null)
                classConfirmButton.interactable = selectedClass != null;

            // Clear preview text when no class is selected
            if (selectedClass == null)
            {
                if (classNameText != null)
                    classNameText.text = "Which road will you travel?";
                if (classDescriptionText != null)
                    classDescriptionText.text = "";
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
                classStatsPreviewText.text = "";
            }

            // Animate selected class, show static first frame on others
            UpdatePreviewAnimation(warriorAnimSprite, warriorData, classData == warriorData);
            UpdatePreviewAnimation(mageAnimSprite, mageData, classData == mageData);
            UpdatePreviewAnimation(rogueAnimSprite, rogueData, classData == rogueData);
        }

        /// <summary>
        /// Plays or stops the animated preview for a class card.
        /// </summary>
        private void UpdatePreviewAnimation(UIAnimatedSprite animSprite, JobClassData jobData, bool isSelected)
        {
            if (animSprite == null || jobData == null)
                return;

            if (!HasJobVisualData(jobData))
                return;

            if (isSelected && jobData.idlePreviewFrames != null && jobData.idlePreviewFrames.Length > 1)
            {
                animSprite.Play(jobData.idlePreviewFrames, jobData.idlePreviewFrameRate);
            }
            else
            {
                // Show static first frame or default sprite
                Sprite staticSprite = jobData.defaultSprite;
                if (staticSprite == null && jobData.idlePreviewFrames != null && jobData.idlePreviewFrames.Length > 0)
                    staticSprite = jobData.idlePreviewFrames[0];

                if (staticSprite != null)
                    animSprite.SetStaticSprite(staticSprite);
                else
                    animSprite.Stop();
            }
        }

        /// <summary>
        /// Sets initial static previews from JobClassData visual fields.
        /// </summary>
        private void SetInitialPreviews()
        {
            SetInitialPreview(warriorAnimSprite, warriorData, warriorPreviewImage);
            SetInitialPreview(mageAnimSprite, mageData, magePreviewImage);
            SetInitialPreview(rogueAnimSprite, rogueData, roguePreviewImage);
        }

        private void SetInitialPreview(UIAnimatedSprite animSprite, JobClassData jobData, Image previewImage)
        {
            if (jobData == null || previewImage == null)
                return;

            if (!HasJobVisualData(jobData))
                return;

            Sprite preview = jobData.defaultSprite;
            if (preview == null && jobData.idlePreviewFrames != null && jobData.idlePreviewFrames.Length > 0)
                preview = jobData.idlePreviewFrames[0];

            if (preview != null)
            {
                previewImage.sprite = preview;
                previewImage.color = Color.white;
            }
        }

        /// <summary>
        /// Checks if any JobClassData has visual data assigned.
        /// </summary>
        private static bool HasJobVisualData(JobClassData jobData)
        {
            if (jobData == null) return false;
            return jobData.defaultSprite != null
                || (jobData.idlePreviewFrames != null && jobData.idlePreviewFrames.Length > 0);
        }

        private void OnClassConfirm()
        {
            if (selectedClass == null)
                return;

            UIManager.Instance?.PlayConfirmSound();
            HideAllPanels();
            OnCreationComplete?.Invoke();
        }

        private void OnClassBack()
        {
            UIManager.Instance?.PlayCancelSound();
            ShowNameEntry(targetSlotIndex);
            if (nameInputField != null)
                nameInputField.text = characterName;
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
            FindJobData(ctrl);
            LoadClassPreviewSprites(ctrl);

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

            // Centered content column, wide enough for 3 large cards
            var content = MakeUIObject("Content", panel.transform);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 0.5f);
            contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.pivot = new Vector2(0.5f, 0.5f);
            contentRt.sizeDelta = new Vector2(2000f, 0f);

            var contentVlg = content.AddComponent<VerticalLayoutGroup>();
            contentVlg.childAlignment = TextAnchor.UpperCenter;
            contentVlg.childControlWidth = true;
            contentVlg.childControlHeight = true;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;
            contentVlg.spacing = 10f;
            contentVlg.padding = new RectOffset(20, 20, 40, 40);

            var contentCsf = content.AddComponent<ContentSizeFitter>();
            contentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Class cards row
            var classRow = MakeHRow(content.transform, 80f, 620f);

            ctrl.warriorButton = MakeClassCard(classRow.transform, "Warrior", out var warriorImg);
            ctrl.warriorPreviewImage = warriorImg;

            ctrl.mageButton = MakeClassCard(classRow.transform, "Mage", out var mageImg);
            ctrl.magePreviewImage = mageImg;

            ctrl.rogueButton = MakeClassCard(classRow.transform, "Rogue", out var rogueImg);
            ctrl.roguePreviewImage = rogueImg;

            MakeSpacer(content.transform, 30f);
            ctrl.classNameText = MakeLabel(content.transform, "Which road will you travel?", 38f);
            ctrl.classNameText.GetComponent<LayoutElement>().preferredWidth = -1f;

            ctrl.classDescriptionText = MakeLabel(content.transform, "", 26f);
            ctrl.classDescriptionText.GetComponent<LayoutElement>().preferredWidth = -1f;
            MakeAutoHeight(ctrl.classDescriptionText.gameObject);

            ctrl.classStatsPreviewText = MakeLabel(content.transform, "", 24f);
            ctrl.classStatsPreviewText.GetComponent<LayoutElement>().preferredWidth = -1f;
            MakeAutoHeight(ctrl.classStatsPreviewText.gameObject);

            var navRow = MakeHRow(content.transform, 20f, 65f);
            ctrl.classBackButton = MakeButton(navRow.transform, "Back", 220f, 60f);
            var backTmp = ctrl.classBackButton.GetComponentInChildren<TextMeshProUGUI>();
            if (backTmp != null) backTmp.fontSize = 28f;
            ctrl.classConfirmButton = MakeButton(navRow.transform, "Start Game", 280f, 60f);
            var confirmTmp = ctrl.classConfirmButton.GetComponentInChildren<TextMeshProUGUI>();
            if (confirmTmp != null) confirmTmp.fontSize = 28f;
            ctrl.classConfirmButton.interactable = false;
        }

        /// <summary>
        /// Builds a class card: vertical container with preview image on top and button below.
        /// </summary>
        private static Button MakeClassCard(Transform parent, string className, out Image previewImage)
        {
            var cardGo = MakeUIObject(className + "Card", parent);
            var cardLayout = cardGo.AddComponent<LayoutElement>();
            cardLayout.preferredWidth = 400f;
            cardLayout.preferredHeight = 620f;

            var vlg = cardGo.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 12f;

            // Preview image container
            var imgContainer = MakeUIObject("PreviewContainer", cardGo.transform);
            var containerImg = imgContainer.AddComponent<Image>();
            containerImg.color = new Color(0.12f, 0.12f, 0.15f, 1f);
            var containerLayout = imgContainer.AddComponent<LayoutElement>();
            containerLayout.preferredHeight = 520f;

            // Preview image (fills container, preserves aspect ratio)
            var imgGo = MakeUIObject("Preview", imgContainer.transform);
            var img = imgGo.AddComponent<Image>();
            img.preserveAspect = true;
            img.color = Color.white;
            var imgRect = imgGo.GetComponent<RectTransform>();
            imgRect.anchorMin = Vector2.zero;
            imgRect.anchorMax = Vector2.one;
            imgRect.offsetMin = Vector2.zero;
            imgRect.offsetMax = Vector2.zero;
            previewImage = img;

            // Class button
            var btn = MakeButton(cardGo.transform, className, 400f, 80f);
            var btnTmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnTmp != null) btnTmp.fontSize = 48f;

            return btn;
        }

        /// <summary>
        /// Attempts to load class preview sprites. Called early from CreateRuntimeUI
        /// and again deferred from ShowClassSelection when more assets are in memory.
        /// Uses JobClassData visuals when available, falls back to procedural silhouettes.
        /// </summary>
        private static void LoadClassPreviewSprites(CharacterCreationController ctrl)
        {
            // Ensure UIAnimatedSprite components exist on preview images
            EnsureAnimatedSprite(ctrl.warriorPreviewImage, ref ctrl.warriorAnimSprite);
            EnsureAnimatedSprite(ctrl.magePreviewImage, ref ctrl.mageAnimSprite);
            EnsureAnimatedSprite(ctrl.roguePreviewImage, ref ctrl.rogueAnimSprite);

            // Try JobClassData visuals first, fall back to silhouettes
            if (!TryApplyJobPreview(ctrl.warriorPreviewImage, ctrl.warriorData))
            {
                if (ctrl.warriorPreviewImage != null && ctrl.warriorPreviewImage.sprite == null)
                {
                    ctrl.warriorPreviewImage.sprite = MakeCharacterSilhouette(
                        new Color(0.55f, 0f, 0f), new Color(0.8f, 0.6f, 0.2f));
                }
            }
            if (!TryApplyJobPreview(ctrl.magePreviewImage, ctrl.mageData))
            {
                if (ctrl.magePreviewImage != null && ctrl.magePreviewImage.sprite == null)
                {
                    ctrl.magePreviewImage.sprite = MakeCharacterSilhouette(
                        new Color(0.15f, 0.15f, 0.5f), new Color(0.3f, 0.7f, 1f));
                    ctrl.magePreviewImage.color = Color.white;
                }
            }
            if (!TryApplyJobPreview(ctrl.roguePreviewImage, ctrl.rogueData))
            {
                if (ctrl.roguePreviewImage != null && ctrl.roguePreviewImage.sprite == null)
                {
                    ctrl.roguePreviewImage.sprite = MakeCharacterSilhouette(
                        new Color(0.1f, 0.3f, 0.1f), new Color(0.4f, 0.9f, 0.4f));
                    ctrl.roguePreviewImage.color = Color.white;
                }
            }
        }

        private static void EnsureAnimatedSprite(Image image, ref UIAnimatedSprite animSprite)
        {
            if (image == null) return;
            if (animSprite == null)
                animSprite = image.gameObject.GetComponent<UIAnimatedSprite>();
            if (animSprite == null)
                animSprite = image.gameObject.AddComponent<UIAnimatedSprite>();
        }

        /// <summary>
        /// Tries to apply a preview from JobClassData visual fields.
        /// Returns true if visual data was found and applied.
        /// </summary>
        private static bool TryApplyJobPreview(Image image, JobClassData jobData)
        {
            if (image == null || jobData == null)
                return false;

            if (!HasJobVisualData(jobData))
                return false;

            Sprite preview = jobData.defaultSprite;
            if (preview == null && jobData.idlePreviewFrames != null && jobData.idlePreviewFrames.Length > 0)
                preview = jobData.idlePreviewFrames[0];

            if (preview != null)
            {
                image.sprite = preview;
                image.color = Color.white;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Deferred sprite loading. Called when class panel is first shown,
        /// giving Unity more time to load asset references into memory.
        /// Skips HeroKnight fallback if any JobClassData has visual data assigned.
        /// </summary>
        private void TryLoadClassSprites()
        {
            // If any job has visual data, skip the HeroKnight sprite hunt
            if (HasJobVisualData(warriorData) || HasJobVisualData(mageData) || HasJobVisualData(rogueData))
                return;

            Sprite knightSprite = FindSprite("HeroKnight_0");

            if (knightSprite != null)
            {
                // Warrior: actual knight sprite
                if (warriorPreviewImage != null)
                {
                    warriorPreviewImage.sprite = knightSprite;
                    warriorPreviewImage.color = Color.white;
                }

                // Mage: knight tinted blue until real mage art exists
                if (magePreviewImage != null)
                {
                    magePreviewImage.sprite = knightSprite;
                    magePreviewImage.color = new Color(0.4f, 0.5f, 1f, 1f);
                }

                // Rogue: knight tinted green until real rogue art exists
                if (roguePreviewImage != null)
                {
                    roguePreviewImage.sprite = knightSprite;
                    roguePreviewImage.color = new Color(0.5f, 1f, 0.5f, 1f);
                }
            }
        }

        private static Sprite FindSprite(string spriteName)
        {
            // Search all sprites currently in memory (includes AssetDatabase in editor)
            var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            foreach (var s in allSprites)
            {
                if (s.name == spriteName)
                    return s;
            }

            // Fallback: search SpriteRenderers on prefabs/objects for the sprite
            var allRenderers = Resources.FindObjectsOfTypeAll<SpriteRenderer>();
            foreach (var sr in allRenderers)
            {
                if (sr.sprite != null && sr.sprite.name == spriteName)
                    return sr.sprite;
            }

            return null;
        }

        /// <summary>
        /// Generates a 32x32 pixel character silhouette with body and accent colors.
        /// Used as placeholder until real class art is provided.
        /// </summary>
        private static Sprite MakeCharacterSilhouette(Color body, Color accent)
        {
            int w = 96, h = 96;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var clear = new Color(0, 0, 0, 0);

            // Fill transparent
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;

            // Head (rows 78-95, cols 36-59)
            FillRect(pixels, w, 36, 78, 60, 96, accent);
            // Neck (rows 72-77, cols 42-53)
            FillRect(pixels, w, 42, 72, 54, 78, body);
            // Torso (rows 42-72, cols 30-65)
            FillRect(pixels, w, 30, 42, 66, 72, body);
            // Belt/accent stripe (rows 45-50)
            FillRect(pixels, w, 30, 45, 66, 51, accent);
            // Left arm (rows 48-72, cols 18-30)
            FillRect(pixels, w, 18, 48, 30, 72, body);
            // Right arm (rows 48-72, cols 66-78)
            FillRect(pixels, w, 66, 48, 78, 72, body);
            // Left leg (rows 6-42, cols 33-45)
            FillRect(pixels, w, 33, 6, 45, 42, body);
            // Right leg (rows 6-42, cols 51-63)
            FillRect(pixels, w, 51, 6, 63, 42, body);
            // Boots accent (rows 6-12)
            FillRect(pixels, w, 33, 6, 45, 12, accent);
            FillRect(pixels, w, 51, 6, 63, 12, accent);
            // Shoulder accents
            FillRect(pixels, w, 24, 66, 33, 72, accent);
            FillRect(pixels, w, 63, 66, 72, 72, accent);

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 96f);
        }

        private static void FillRect(Color[] pixels, int texWidth, int x0, int y0, int x1, int y1, Color color)
        {
            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    if (x >= 0 && x < texWidth && y >= 0 && y < texWidth)
                        pixels[y * texWidth + x] = color;
                }
            }
        }

        private static void FindJobData(CharacterCreationController ctrl)
        {
            var allJobs = Resources.FindObjectsOfTypeAll<JobClassData>();
            foreach (var job in allJobs)
            {
                if (string.IsNullOrEmpty(job.jobName)) continue;
                string n = job.jobName.ToLower();

                // Prefer assets with visual data over runtime duplicates without
                if (n.Contains("warrior"))
                {
                    if (ctrl.warriorData == null || (!HasJobVisualData(ctrl.warriorData) && HasJobVisualData(job)))
                        ctrl.warriorData = job;
                }
                else if (n.Contains("mage"))
                {
                    if (ctrl.mageData == null || (!HasJobVisualData(ctrl.mageData) && HasJobVisualData(job)))
                        ctrl.mageData = job;
                }
                else if (n.Contains("rogue"))
                {
                    if (ctrl.rogueData == null || (!HasJobVisualData(ctrl.rogueData) && HasJobVisualData(job)))
                        ctrl.rogueData = job;
                }
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
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
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
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
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
        }
    }
}
