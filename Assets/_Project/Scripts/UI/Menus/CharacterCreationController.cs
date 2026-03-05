using System;
using System.Collections.Generic;
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
            ClassSelection,
            AppearanceCustomization
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

        [Header("Appearance Customization")]
        [SerializeField] private GameObject appearancePanel;
        [SerializeField] private BodyPartRegistry bodyPartRegistry;
        [SerializeField] private Button appearanceBackButton;
        [SerializeField] private Button appearanceConfirmButton;
        [SerializeField] private Button hairPrevButton;
        [SerializeField] private Button hairNextButton;
        [SerializeField] private TMP_Text hairNameText;
        [SerializeField] private Image skinColorPreview;
        [SerializeField] private Button skinPrevButton;
        [SerializeField] private Button skinNextButton;
        [SerializeField] private Image hairColorPreview;
        [SerializeField] private TMP_Text hairColorNameText;
        [SerializeField] private Button hairColorPrevButton;
        [SerializeField] private Button hairColorNextButton;

        [Header("Body Type Selection")]
        [SerializeField] private Button bodyTypeMaleButton;
        [SerializeField] private Button bodyTypeFemaleButton;
        [SerializeField] private TMP_Text bodyTypeLabel;

        [Header("Beard")]
        [SerializeField] private Button beardPrevButton;
        [SerializeField] private Button beardNextButton;
        [SerializeField] private TMP_Text beardNameText;

        [Header("Eyes")]
        [SerializeField] private Button eyeColorPrevButton;
        [SerializeField] private Button eyeColorNextButton;
        [SerializeField] private Image eyeColorPreview;
        [SerializeField] private TMP_Text eyeColorNameText;
        [SerializeField] private TMP_Text skinToneNameText;

        private UILayeredSpritePreview appearancePreview;

        // Animated preview components (added at runtime)
        private UIAnimatedSprite warriorAnimSprite;
        private UIAnimatedSprite mageAnimSprite;
        private UIAnimatedSprite rogueAnimSprite;

        // Layered character previews for class cards
        private UILayeredSpritePreview warriorLayeredPreview;
        private UILayeredSpritePreview mageLayeredPreview;
        private UILayeredSpritePreview rogueLayeredPreview;

        // Selection highlight borders for class cards
        private GameObject warriorSelectionBorder;
        private GameObject mageSelectionBorder;
        private GameObject rogueSelectionBorder;

        // Creation data
        private string characterName = "";
        private JobClassData selectedClass;
        private int targetSlotIndex = -1;
        private CreationStep currentStep;
        private bool classSpritesLoaded;

        // Appearance selection state
        private BodyPartData[] availableHairStyles;
        private int selectedHairIndex;
        private CharacterAppearanceConfig builtAppearance;
        private string selectedBodyType = "male";

        // Beard cycling state
        private BodyPartData[] availableBeardStyles;
        private int selectedBeardIndex = -1; // -1 = None

        // Ears cycling state
        private BodyPartData[] availableEarStyles;
        private int selectedEarsIndex = -1; // -1 = None (human ears from head base)

        // Eye color cycling state
        private int selectedEyeColorIndex;

        private static readonly Color[] EyeColorPresets = new Color[]
        {
            new Color(0.45f, 0.30f, 0.15f),     // Brown
            new Color(0.30f, 0.50f, 0.80f),     // Blue
            new Color(0.30f, 0.60f, 0.30f),     // Green
            new Color(0.55f, 0.45f, 0.25f),     // Hazel
            new Color(0.55f, 0.55f, 0.55f),     // Gray
            new Color(0.75f, 0.50f, 0.10f),     // Amber
        };
        private static readonly string[] EyeColorNames = new string[]
        {
            "Brown", "Blue", "Green", "Hazel", "Gray", "Amber"
        };

        private static readonly Color[] SkinTonePresets = new Color[]
        {
            new Color(1.00f, 0.89f, 0.78f),     // Light
            new Color(0.90f, 0.72f, 0.49f),     // Amber
            new Color(0.78f, 0.72f, 0.55f),     // Olive
            new Color(0.70f, 0.58f, 0.48f),     // Taupe
            new Color(0.57f, 0.42f, 0.28f),     // Bronze
            new Color(0.42f, 0.30f, 0.20f),     // Brown
            new Color(0.28f, 0.20f, 0.15f),     // Black
        };
        private int selectedSkinToneIndex;
        private static readonly string[] SkinToneNames = new string[]
        {
            "Light", "Amber", "Olive", "Taupe", "Bronze", "Brown", "Black"
        };

        private static readonly Color[] HairColorPresets = new Color[]
        {
            new Color(0.75f, 0.15f, 0.10f),     // Red
            new Color(0.90f, 0.50f, 0.10f),     // Orange
            new Color(0.95f, 0.85f, 0.30f),     // Blonde
            new Color(0.45f, 0.30f, 0.15f),     // Brown
            new Color(0.15f, 0.12f, 0.10f),     // Black
            Color.white,                          // White
            new Color(0.10f, 0.85f, 0.90f),     // Cyan
            new Color(0.15f, 0.70f, 0.25f),     // Green
            new Color(0.55f, 0.20f, 0.75f),     // Purple
        };
        private static readonly string[] HairColorNames = new string[]
        {
            "Red", "Orange", "Blonde", "Brown", "Black", "White", "Cyan", "Green", "Purple"
        };
        private int selectedHairColorIndex;

        // Results
        public string CharacterName => characterName;
        public JobClassData SelectedClass => selectedClass;
        public int TargetSlotIndex => targetSlotIndex;
        public CreationStep CurrentStep => currentStep;
        public CharacterAppearanceConfig BuiltAppearance => builtAppearance;

        public event Action OnCreationComplete;
        public event Action OnCreationCancelled;

        private void Awake()
        {
            SetupButtons();
        }

        private void SetupButtons()
        {
            // Name entry — RemoveAllListeners before Add to be idempotent on reuse
            if (nameConfirmButton != null)
            {
                nameConfirmButton.onClick.RemoveAllListeners();
                nameConfirmButton.onClick.AddListener(OnNameConfirm);
            }
            if (nameBackButton != null)
            {
                nameBackButton.onClick.RemoveAllListeners();
                nameBackButton.onClick.AddListener(OnNameBack);
            }
            if (nameInputField != null)
            {
                nameInputField.characterLimit = maxNameLength;
                nameInputField.onSubmit.RemoveAllListeners();
                nameInputField.onSubmit.AddListener(_ => OnNameConfirm());
            }

            // Class selection
            if (warriorButton != null)
            {
                warriorButton.onClick.RemoveAllListeners();
                warriorButton.onClick.AddListener(() => SelectClass(warriorData));
            }
            if (mageButton != null)
            {
                mageButton.onClick.RemoveAllListeners();
                mageButton.onClick.AddListener(() => SelectClass(mageData));
            }
            if (rogueButton != null)
            {
                rogueButton.onClick.RemoveAllListeners();
                rogueButton.onClick.AddListener(() => SelectClass(rogueData));
            }
            if (classBackButton != null)
            {
                classBackButton.onClick.RemoveAllListeners();
                classBackButton.onClick.AddListener(OnClassBack);
            }
            if (classConfirmButton != null)
            {
                classConfirmButton.onClick.RemoveAllListeners();
                classConfirmButton.onClick.AddListener(OnClassConfirm);
            }

            // Appearance customization
            if (appearanceBackButton != null)
            {
                appearanceBackButton.onClick.RemoveAllListeners();
                appearanceBackButton.onClick.AddListener(OnAppearanceBack);
            }
            if (appearanceConfirmButton != null)
            {
                appearanceConfirmButton.onClick.RemoveAllListeners();
                appearanceConfirmButton.onClick.AddListener(OnAppearanceConfirm);
            }
            if (hairPrevButton != null)
            {
                hairPrevButton.onClick.RemoveAllListeners();
                hairPrevButton.onClick.AddListener(() => CycleHair(-1));
            }
            if (hairNextButton != null)
            {
                hairNextButton.onClick.RemoveAllListeners();
                hairNextButton.onClick.AddListener(() => CycleHair(1));
            }
            if (skinPrevButton != null)
            {
                skinPrevButton.onClick.RemoveAllListeners();
                skinPrevButton.onClick.AddListener(() => CycleSkinTone(-1));
            }
            if (skinNextButton != null)
            {
                skinNextButton.onClick.RemoveAllListeners();
                skinNextButton.onClick.AddListener(() => CycleSkinTone(1));
            }
            if (hairColorPrevButton != null)
            {
                hairColorPrevButton.onClick.RemoveAllListeners();
                hairColorPrevButton.onClick.AddListener(() => CycleHairColor(-1));
            }
            if (hairColorNextButton != null)
            {
                hairColorNextButton.onClick.RemoveAllListeners();
                hairColorNextButton.onClick.AddListener(() => CycleHairColor(1));
            }

            // Body type toggle
            if (bodyTypeMaleButton != null)
            {
                bodyTypeMaleButton.onClick.RemoveAllListeners();
                bodyTypeMaleButton.onClick.AddListener(() => SetBodyType("male"));
            }
            if (bodyTypeFemaleButton != null)
            {
                bodyTypeFemaleButton.onClick.RemoveAllListeners();
                bodyTypeFemaleButton.onClick.AddListener(() => SetBodyType("female"));
            }

            // Beard cycling
            if (beardPrevButton != null)
            {
                beardPrevButton.onClick.RemoveAllListeners();
                beardPrevButton.onClick.AddListener(() => CycleBeard(-1));
            }
            if (beardNextButton != null)
            {
                beardNextButton.onClick.RemoveAllListeners();
                beardNextButton.onClick.AddListener(() => CycleBeard(1));
            }

            // Eye color cycling
            if (eyeColorPrevButton != null)
            {
                eyeColorPrevButton.onClick.RemoveAllListeners();
                eyeColorPrevButton.onClick.AddListener(() => CycleEyeColor(-1));
            }
            if (eyeColorNextButton != null)
            {
                eyeColorNextButton.onClick.RemoveAllListeners();
                eyeColorNextButton.onClick.AddListener(() => CycleEyeColor(1));
            }

        }

        /// <summary>
        /// Resets all creation state for a fresh run. Call before showing character creation
        /// to ensure no stale data from a previous session.
        /// </summary>
        public void ResetState()
        {
            characterName = "";
            selectedClass = null;
            targetSlotIndex = -1;
            currentStep = CreationStep.NameEntry;
            classSpritesLoaded = false;
            selectedHairIndex = 0;
            selectedSkinToneIndex = 0;
            selectedHairColorIndex = 0;
            selectedBeardIndex = -1;
            selectedEarsIndex = -1;
            selectedEyeColorIndex = 0;
            builtAppearance = null;
            selectedBodyType = "male";

            if (appearancePreview != null)
                appearancePreview.Clear();
            if (warriorLayeredPreview != null)
                warriorLayeredPreview.Clear();
            if (mageLayeredPreview != null)
                mageLayeredPreview.Clear();
            if (rogueLayeredPreview != null)
                rogueLayeredPreview.Clear();

            // Re-enable fallback images that ApplyAppearanceToCard disabled
            if (warriorPreviewImage != null) warriorPreviewImage.enabled = true;
            if (magePreviewImage != null) magePreviewImage.enabled = true;
            if (roguePreviewImage != null) roguePreviewImage.enabled = true;

            // Hide selection borders
            if (warriorSelectionBorder != null) warriorSelectionBorder.SetActive(false);
            if (mageSelectionBorder != null) mageSelectionBorder.SetActive(false);
            if (rogueSelectionBorder != null) rogueSelectionBorder.SetActive(false);

            HideAllPanels();
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

            // Appearance before class so the customized character shows in class previews
            if (HasAppearanceOptions())
                ShowAppearanceCustomization();
            else
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

            // Apply customized character appearance to class card previews
            ApplyAppearanceToClassCards();

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

            // Toggle selection borders
            if (warriorSelectionBorder != null)
                warriorSelectionBorder.SetActive(classData == warriorData);
            if (mageSelectionBorder != null)
                mageSelectionBorder.SetActive(classData == mageData);
            if (rogueSelectionBorder != null)
                rogueSelectionBorder.SetActive(classData == rogueData);

            if (classConfirmButton != null)
                classConfirmButton.interactable = true;
        }

        private void UpdateClassPreview(JobClassData classData)
        {
            if (classNameText != null)
                classNameText.text = classData.jobName;

            if (classDescriptionText != null)
                classDescriptionText.text = classData.description;

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

            // Filter out null frames before playing animation
            if (isSelected && jobData.idlePreviewFrames != null)
            {
                var validFrames = new System.Collections.Generic.List<Sprite>();
                foreach (var frame in jobData.idlePreviewFrames)
                {
                    if (frame != null)
                        validFrames.Add(frame);
                }

                if (validFrames.Count > 1)
                {
                    animSprite.Play(validFrames.ToArray(), jobData.idlePreviewFrameRate);
                    return;
                }
            }

            // Show static sprite: prefer defaultSprite, then first valid frame
            Sprite staticSprite = jobData.defaultSprite;
            if (staticSprite == null && jobData.idlePreviewFrames != null)
            {
                foreach (var frame in jobData.idlePreviewFrames)
                {
                    if (frame != null)
                    {
                        staticSprite = frame;
                        break;
                    }
                }
            }

            if (staticSprite != null)
                animSprite.SetStaticSprite(staticSprite);
            else
                animSprite.Stop();
        }

        private static string FormatClassStats(JobClassData classData)
        {
            if (classData == null) return "";

            string FormatMod(string label, float value)
            {
                string color = value > 1f ? "#7FFF7F" : value < 1f ? "#FF7F7F" : "#CFCFCF";
                string sign = value > 1f ? "+" : "";
                int pct = Mathf.RoundToInt((value - 1f) * 100f);
                return $"<color={color}>{label} {sign}{pct}%</color>";
            }

            var parts = new List<string>();
            parts.Add(FormatMod("ATK", classData.attackModifier));
            parts.Add(FormatMod("MAG", classData.magicModifier));
            parts.Add(FormatMod("DEF", classData.defenseModifier));

            if (classData.baseHPBonus != 0)
            {
                string hpColor = classData.baseHPBonus > 0 ? "#7FFF7F" : "#FF7F7F";
                string hpSign = classData.baseHPBonus > 0 ? "+" : "";
                parts.Add($"<color={hpColor}>HP {hpSign}{classData.baseHPBonus}</color>");
            }
            if (classData.baseMPBonus != 0)
            {
                string mpColor = classData.baseMPBonus > 0 ? "#7FFF7F" : "#FF7F7F";
                string mpSign = classData.baseMPBonus > 0 ? "+" : "";
                parts.Add($"<color={mpColor}>MP {mpSign}{classData.baseMPBonus}</color>");
            }

            return string.Join("   ", parts);
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

            // Find the first valid sprite: prefer defaultSprite, then search idle frames
            Sprite preview = jobData.defaultSprite;
            if (preview == null && jobData.idlePreviewFrames != null)
            {
                foreach (var frame in jobData.idlePreviewFrames)
                {
                    if (frame != null)
                    {
                        preview = frame;
                        break;
                    }
                }
            }

            if (preview != null)
            {
                previewImage.sprite = preview;
                previewImage.color = Color.white;
            }
            else
            {
                // No sprite data — apply a class-themed background tint.
                // The layered preview (ApplyAppearanceToCard) will render over this.
                previewImage.sprite = null;
                previewImage.color = jobData.jobColor;
            }
        }

        /// <summary>
        /// Applies the player's customized appearance to each class card's layered preview.
        /// Merges the player's base appearance (body/head/hair/tints) with each class's
        /// gear (torso/legs/weapons) when available.
        /// </summary>
        private void ApplyAppearanceToClassCards()
        {
            // Build a default appearance from the registry if the user skipped customization
            if (builtAppearance == null)
            {
                if (bodyPartRegistry == null)
                    bodyPartRegistry = Resources.Load<BodyPartRegistry>("BodyPartRegistry");

                if (bodyPartRegistry != null)
                {
                    builtAppearance = ScriptableObject.CreateInstance<CharacterAppearanceConfig>();
                    var bodyParts = bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Body);
                    var headParts = bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Head);
                    var faceHeads = FilterByPrefix(headParts, "head_");
                    var hairParts = bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Hair);
                    if (bodyParts.Length > 0) builtAppearance.body = bodyParts[0];
                    builtAppearance.head = FindDefaultHumanHead(faceHeads, headParts, "male");
                    if (hairParts.Length > 0) builtAppearance.hair = hairParts[0];
                    builtAppearance.skinTint = SkinTonePresets[0];
                    builtAppearance.hairTint = HairColorPresets[0];

                    // Add default eye overlay so eye color tinting works
                    var eyeParts = bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Eyes);
                    var defaultEyeParts = FilterByPrefix(eyeParts, "eyes_default");
                    if (defaultEyeParts.Length > 0) builtAppearance.SetPart(BodyPartSlot.Eyes, defaultEyeParts[0]);
                    else if (eyeParts.Length > 0) builtAppearance.SetPart(BodyPartSlot.Eyes, eyeParts[0]);
                    builtAppearance.eyeTint = EyeColorPresets[0];

                    // Add default clothing so character isn't naked
                    var torsoParts = bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Torso);
                    if (torsoParts.Length > 0) builtAppearance.SetPart(BodyPartSlot.Torso, torsoParts[0]);
                    var legsParts = bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Legs);
                    if (legsParts.Length > 0) builtAppearance.SetPart(BodyPartSlot.Legs, legsParts[0]);
                    var feetParts = bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Feet);
                    if (feetParts.Length > 0) builtAppearance.SetPart(BodyPartSlot.Feet, feetParts[0]);
                }
            }

            if (builtAppearance == null) return;

            ApplyAppearanceToCard(warriorLayeredPreview, warriorPreviewImage, warriorData);
            ApplyAppearanceToCard(mageLayeredPreview, magePreviewImage, mageData);
            ApplyAppearanceToCard(rogueLayeredPreview, roguePreviewImage, rogueData);
        }

        private void ApplyAppearanceToCard(UILayeredSpritePreview preview, Image fallbackImage, JobClassData jobData)
        {
            if (preview == null || builtAppearance == null) return;

            // Create a merged config: player's base appearance + class gear
            var merged = ScriptableObject.CreateInstance<CharacterAppearanceConfig>();
            merged.body = builtAppearance.body;
            merged.head = builtAppearance.head;
            merged.hair = builtAppearance.hair;
            merged.skinTint = builtAppearance.skinTint;
            merged.hairTint = builtAppearance.hairTint;
            merged.eyeTint = builtAppearance.eyeTint;
            var baseEyes = builtAppearance.GetPart(BodyPartSlot.Eyes);
            if (baseEyes != null) merged.SetPart(BodyPartSlot.Eyes, baseEyes);

            // Carry default clothing as baseline so character isn't naked if equipment visuals fail
            var baseTorso = builtAppearance.GetPart(BodyPartSlot.Torso);
            if (baseTorso != null) merged.torso = baseTorso;
            var baseLegs = builtAppearance.GetPart(BodyPartSlot.Legs);
            if (baseLegs != null) merged.legs = baseLegs;
            var baseFeet = builtAppearance.GetPart(BodyPartSlot.Feet);
            if (baseFeet != null) merged.SetPart(BodyPartSlot.Feet, baseFeet);

            bool hasEquipment = false;

            // Primary: per-class starter equipment visuals
            if (jobData != null && jobData.starterEquipment != null)
            {
                foreach (var equip in jobData.starterEquipment)
                {
                    if (equip == null || equip.visualPart == null) continue;
                    switch (equip.slotType)
                    {
                        case EquipmentSlotType.Armor:
                            merged.torso = equip.visualPart;
                            hasEquipment = true;
                            break;
                        case EquipmentSlotType.Boots:
                            merged.legs = equip.visualPart;
                            merged.SetPart(BodyPartSlot.Feet, null); // Equipment boots cover feet
                            hasEquipment = true;
                            break;
                        case EquipmentSlotType.Weapon:
                            merged.weaponFront = equip.visualPart;
                            hasEquipment = true;
                            break;
                    }
                }
            }

            // Fallback: shared default appearance config
            if (!hasEquipment && jobData != null && jobData.defaultAppearance != null)
            {
                var da = jobData.defaultAppearance;
                if (da.torso != null) merged.torso = da.torso;
                if (da.legs != null) merged.legs = da.legs;
                if (da.weaponBehind != null) merged.weaponBehind = da.weaponBehind;
                if (da.weaponFront != null) merged.weaponFront = da.weaponFront;
            }

            preview.ApplyConfig(merged);

            // Hide fallback static image when layered preview is active
            if (fallbackImage != null)
                fallbackImage.enabled = false;
        }

        /// <summary>
        /// Checks if a JobClassData has at least one valid (non-null) visual asset assigned.
        /// Validates individual array entries because GUID references can resolve to null
        /// if the source texture is not loaded yet.
        /// </summary>
        private static bool HasJobVisualData(JobClassData jobData)
        {
            if (jobData == null) return false;

            if (jobData.defaultSprite != null)
                return true;

            if (jobData.idlePreviewFrames != null)
            {
                foreach (var frame in jobData.idlePreviewFrames)
                {
                    if (frame != null)
                        return true;
                }
            }

            return false;
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
            SetPanelActive(classSelectionPanel, false);
            // Go back to appearance if available, otherwise name entry
            if (HasAppearanceOptions())
            {
                ShowAppearanceCustomization();
            }
            else
            {
                ShowNameEntry(targetSlotIndex);
                if (nameInputField != null)
                    nameInputField.text = characterName;
            }
        }

        #endregion

        #region Appearance Customization

        private bool HasAppearanceOptions()
        {
            // Lazy-find registry if not set (e.g. reused from AutoFindReferences)
            if (bodyPartRegistry == null)
                bodyPartRegistry = Resources.Load<BodyPartRegistry>("BodyPartRegistry");

            if (bodyPartRegistry == null || bodyPartRegistry.allParts == null)
                return false;

            // Need at least a body part to show anything
            var bodyParts = bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Body);
            return bodyParts.Length > 0;
        }

        private void ShowAppearanceCustomization()
        {
            currentStep = CreationStep.AppearanceCustomization;
            SetPanelActive(nameEntryPanel, false);
            SetPanelActive(classSelectionPanel, false);
            SetPanelActive(appearancePanel, true);

            // Gather available styles (filtered by body type)
            availableHairStyles = FilterHairStyles(
                bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Hair, selectedBodyType), selectedBodyType);
            availableBeardStyles = bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Beard, selectedBodyType);

            // Filter head parts: only "head_" prefixed parts are actual face bases
            var allHeadParts = bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Head, selectedBodyType);
            var filteredHeadParts = FilterByPrefix(allHeadParts, "head_");
            availableEarStyles = FilterByPrefix(allHeadParts, "ears_");

            // Only initialize appearance if not already built (preserve choices when navigating back)
            if (builtAppearance == null)
            {
                selectedHairIndex = FindPartIndex(availableHairStyles, "hair_messy1");
                selectedSkinToneIndex = 0;
                selectedHairColorIndex = 3; // Brown
                selectedBeardIndex = -1;
                selectedEarsIndex = -1;
                selectedEyeColorIndex = 0;

                builtAppearance = ScriptableObject.CreateInstance<CharacterAppearanceConfig>();
                builtAppearance.bodyType = selectedBodyType;

                var bodyParts = bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Body, selectedBodyType);
                if (bodyParts.Length > 0) builtAppearance.body = bodyParts[0];

                // Pick the default human head matching body type
                builtAppearance.head = FindDefaultHumanHead(filteredHeadParts, allHeadParts, selectedBodyType);

                if (availableHairStyles.Length > 0)
                    builtAppearance.hair = availableHairStyles[selectedHairIndex];

                // Ears default to None — human ears come from the head base.

                builtAppearance.skinTint = SkinTonePresets[0];
                builtAppearance.hairTint = HairColorPresets[selectedHairColorIndex];
                builtAppearance.eyeTint = EyeColorPresets[0];

                AssignDefaultClothing();
            }

            UpdateBodyTypeUI();
            RefreshAppearancePreview();
            UpdateAppearanceUI();
        }

        private void CycleHair(int direction)
        {
            if (availableHairStyles == null || availableHairStyles.Length == 0) return;

            selectedHairIndex = (selectedHairIndex + direction + availableHairStyles.Length) % availableHairStyles.Length;
            builtAppearance.hair = availableHairStyles[selectedHairIndex];
            UIManager.Instance?.PlayNavigateSound();
            RefreshAppearancePreview();
            UpdateAppearanceUI();
        }

        private void CycleSkinTone(int direction)
        {
            selectedSkinToneIndex = (selectedSkinToneIndex + direction + SkinTonePresets.Length) % SkinTonePresets.Length;
            builtAppearance.skinTint = SkinTonePresets[selectedSkinToneIndex];
            UIManager.Instance?.PlayNavigateSound();
            RefreshAppearancePreview();
            UpdateAppearanceUI();
        }

        private void CycleHairColor(int direction)
        {
            selectedHairColorIndex = (selectedHairColorIndex + direction + HairColorPresets.Length) % HairColorPresets.Length;
            builtAppearance.hairTint = HairColorPresets[selectedHairColorIndex];
            UIManager.Instance?.PlayNavigateSound();
            RefreshAppearancePreview();
            UpdateAppearanceUI();
        }

        private void CycleBeard(int direction)
        {
            if (availableBeardStyles == null || availableBeardStyles.Length == 0)
            {
                selectedBeardIndex = -1;
                if (builtAppearance != null)
                    builtAppearance.SetPart(BodyPartSlot.Beard, null);
                UpdateAppearanceUI();
                return;
            }

            int totalOptions = availableBeardStyles.Length + 1; // +1 for "None"
            int adjustedIndex = selectedBeardIndex + 1; // shift so None=0
            adjustedIndex = (adjustedIndex + direction + totalOptions) % totalOptions;
            selectedBeardIndex = adjustedIndex - 1;

            var selectedPart = selectedBeardIndex >= 0 ? availableBeardStyles[selectedBeardIndex] : null;
            if (builtAppearance != null)
                builtAppearance.SetPart(BodyPartSlot.Beard, selectedPart);

            UIManager.Instance?.PlayNavigateSound();
            RefreshAppearancePreview();
            UpdateAppearanceUI();
        }

        private void CycleEyeColor(int direction)
        {
            selectedEyeColorIndex = (selectedEyeColorIndex + direction + EyeColorPresets.Length) % EyeColorPresets.Length;
            builtAppearance.eyeTint = EyeColorPresets[selectedEyeColorIndex];
            UIManager.Instance?.PlayNavigateSound();
            RefreshAppearancePreview();
            UpdateAppearanceUI();
        }

        private static BodyPartData[] FilterByPrefix(BodyPartData[] parts, string prefix)
        {
            return Array.FindAll(parts, p => p.partId != null && p.partId.StartsWith(prefix));
        }

        private static int FindPartIndex(BodyPartData[] parts, string partId)
        {
            if (parts == null) return 0;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].partId == partId)
                    return i;
            }
            return 0;
        }

        /// <summary>
        /// Filters hair parts to only standalone styles for the given body type.
        /// Excludes extension overlays (hairextl_, hairextr_, hairtie_, ponytail_, updo_)
        /// and drops universal/unsuffixed duplicates when a gender-specific variant exists.
        /// </summary>
        private static BodyPartData[] FilterHairStyles(BodyPartData[] parts, string bodyType)
        {
            string suffix = "_" + bodyType; // "_male" or "_female"
            var genderedIds = new HashSet<string>();

            // First pass: collect base names that have a gender-specific variant
            foreach (var p in parts)
            {
                if (p.partId == null) continue;
                if (p.partId.EndsWith(suffix))
                    genderedIds.Add(p.partId.Substring(0, p.partId.Length - suffix.Length));
            }

            var result = new List<BodyPartData>();
            foreach (var p in parts)
            {
                if (p.partId == null) continue;

                // Exclude extension/overlay prefixes — these aren't standalone hairstyles
                if (p.partId.StartsWith("hairextl_") || p.partId.StartsWith("hairextr_") ||
                    p.partId.StartsWith("hairtie_") || p.partId.StartsWith("ponytail_") ||
                    p.partId.StartsWith("updo_"))
                    continue;

                // If this is a universal part and a gendered variant exists, skip the duplicate
                if (!p.partId.EndsWith(suffix) && genderedIds.Contains(p.partId))
                    continue;

                result.Add(p);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Finds the best default human head for the given body type.
        /// Prefers "head_human_male_male" / "head_human_female_female",
        /// then any "head_human_" part, then first filtered, then first raw.
        /// </summary>
        private static BodyPartData FindDefaultHumanHead(BodyPartData[] filteredHeads, BodyPartData[] allHeads, string bodyType)
        {
            // Exact match: head_human_male_male or head_human_female_female
            string preferredId = bodyType == "female" ? "head_human_female_female" : "head_human_male_male";
            foreach (var part in filteredHeads)
            {
                if (part.partId == preferredId)
                    return part;
            }

            // Any human head
            foreach (var part in filteredHeads)
            {
                if (part.partId != null && part.partId.StartsWith("head_human_"))
                    return part;
            }

            // First filtered head (non-human but at least a head_ part)
            if (filteredHeads.Length > 0)
                return filteredHeads[0];

            // Last resort: first raw head part
            if (allHeads.Length > 0)
                return allHeads[0];

            return null;
        }

        private void AssignDefaultClothing()
        {
            if (builtAppearance == null || bodyPartRegistry == null) return;

            // Auto-assign default eye overlay so eye color tinting works
            var eyeParts = bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Eyes, selectedBodyType);
            var eyeStyles = FilterByPrefix(eyeParts, "eyes_");
            if (eyeStyles.Length == 0) eyeStyles = eyeParts;
            BodyPartData defaultEyes = null;
            foreach (var part in eyeStyles)
            {
                if (part.partId != null && part.partId.Contains("eyes_default"))
                {
                    defaultEyes = part;
                    break;
                }
            }
            if (defaultEyes == null && eyeStyles.Length > 0)
                defaultEyes = eyeStyles[0];
            if (defaultEyes != null)
                builtAppearance.SetPart(BodyPartSlot.Eyes, defaultEyes);

            var torsoParts = bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Torso, selectedBodyType);
            BodyPartData defaultTorso = null;
            foreach (var part in torsoParts)
            {
                if (part.partId != null && part.partId.Contains("tshirt"))
                {
                    defaultTorso = part;
                    break;
                }
            }
            if (defaultTorso == null && torsoParts.Length > 0)
                defaultTorso = torsoParts[0];
            if (defaultTorso != null)
                builtAppearance.SetPart(BodyPartSlot.Torso, defaultTorso);

            var legsParts = bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Legs, selectedBodyType);
            BodyPartData defaultLegs = null;
            foreach (var part in legsParts)
            {
                if (part.partId != null && part.partId.Contains("pants"))
                {
                    defaultLegs = part;
                    break;
                }
            }
            if (defaultLegs == null && legsParts.Length > 0)
                defaultLegs = legsParts[0];
            if (defaultLegs != null)
                builtAppearance.SetPart(BodyPartSlot.Legs, defaultLegs);

            // Feet: prefers "basic_shoes", falls back to first feet part
            var feetParts = bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Feet, selectedBodyType);
            BodyPartData defaultFeet = null;
            foreach (var part in feetParts)
            {
                if (part.partId != null && part.partId.Contains("basic_shoes"))
                {
                    defaultFeet = part;
                    break;
                }
            }
            if (defaultFeet == null && feetParts.Length > 0)
                defaultFeet = feetParts[0];
            if (defaultFeet != null)
                builtAppearance.SetPart(BodyPartSlot.Feet, defaultFeet);
        }

        private void RefreshAppearancePreview()
        {
            if (appearancePreview != null)
                appearancePreview.ApplyConfig(builtAppearance);
        }

        private void UpdateAppearanceUI()
        {
            if (hairNameText != null && builtAppearance.hair != null)
                hairNameText.text = !string.IsNullOrEmpty(builtAppearance.hair.displayName)
                    ? builtAppearance.hair.displayName
                    : builtAppearance.hair.partId;

            if (skinColorPreview != null)
                skinColorPreview.color = builtAppearance.skinTint;
            if (skinToneNameText != null && selectedSkinToneIndex >= 0 && selectedSkinToneIndex < SkinToneNames.Length)
                skinToneNameText.text = SkinToneNames[selectedSkinToneIndex];

            if (hairColorPreview != null)
                hairColorPreview.color = builtAppearance.hairTint;

            if (hairColorNameText != null && selectedHairColorIndex >= 0 && selectedHairColorIndex < HairColorNames.Length)
                hairColorNameText.text = HairColorNames[selectedHairColorIndex];

            // Beard name
            if (beardNameText != null)
            {
                if (selectedBeardIndex < 0 || availableBeardStyles == null || availableBeardStyles.Length == 0)
                    beardNameText.text = "None";
                else
                {
                    var part = availableBeardStyles[selectedBeardIndex];
                    beardNameText.text = !string.IsNullOrEmpty(part.displayName) ? part.displayName : part.partId;
                }
            }

            // Eye color
            if (eyeColorPreview != null)
                eyeColorPreview.color = builtAppearance.eyeTint;
            if (eyeColorNameText != null && selectedEyeColorIndex >= 0 && selectedEyeColorIndex < EyeColorNames.Length)
                eyeColorNameText.text = EyeColorNames[selectedEyeColorIndex];
        }

        // ----- Body Type -----

        private void SetBodyType(string bodyType)
        {
            if (selectedBodyType == bodyType) return;
            selectedBodyType = bodyType;
            UIManager.Instance?.PlayNavigateSound();

            // Rebuild appearance for new body type
            builtAppearance = null;
            ShowAppearanceCustomization();
        }

        private void UpdateBodyTypeUI()
        {
            if (bodyTypeLabel != null)
                bodyTypeLabel.text = selectedBodyType == "male" ? "A" : "B";

            // Active: crimson bg + gold text; inactive: normal midnight blue
            ApplyBodyTypeButtonStyle(bodyTypeMaleButton, selectedBodyType == "male");
            ApplyBodyTypeButtonStyle(bodyTypeFemaleButton, selectedBodyType == "female");
        }

        private static void ApplyBodyTypeButtonStyle(Button btn, bool isActive)
        {
            if (btn == null) return;
            var colors = btn.colors;
            if (isActive)
            {
                colors.normalColor = BtnSelected;
                colors.highlightedColor = BtnSelected;
                colors.pressedColor = BtnSelected;
                colors.selectedColor = BtnSelected;
            }
            else
            {
                colors.normalColor = BtnNormal;
                colors.highlightedColor = BtnHover;
                colors.pressedColor = BtnPress;
                colors.selectedColor = BtnHover;
            }
            btn.colors = colors;

            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
                tmp.color = isActive ? FrameGold : TextCol;
        }

        private void OnAppearanceBack()
        {
            UIManager.Instance?.PlayCancelSound();
            SetPanelActive(appearancePanel, false);
            ShowNameEntry(targetSlotIndex);
            if (nameInputField != null)
                nameInputField.text = characterName;
        }

        private void OnAppearanceConfirm()
        {
            UIManager.Instance?.PlayConfirmSound();
            SetPanelActive(appearancePanel, false);
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

        // Gothic color palette for runtime-built UI
        private static readonly Color PanelBg = new Color(0.06f, 0.05f, 0.08f, 0.97f);      // Obsidian
        private static readonly Color BtnNormal = new Color(0.10f, 0.10f, 0.18f, 1f);        // Midnight Blue btn
        private static readonly Color BtnHover = new Color(0.15f, 0.15f, 0.25f, 1f);
        private static readonly Color BtnPress = new Color(0.08f, 0.08f, 0.14f, 1f);
        private static readonly Color TextCol = new Color(0.93f, 0.89f, 0.82f, 1f);          // Bone White
        private static readonly Color InputBg = new Color(0.10f, 0.09f, 0.12f, 1f);
        private static readonly Color FrameGold = new Color(0.81f, 0.71f, 0.23f, 1f);        // Aged Gold #CFB53B
        private static readonly Color DeepCrimson = new Color(0.55f, 0f, 0f, 1f);             // #8B0000
        private static readonly Color MidnightBlue = new Color(0.10f, 0.10f, 0.44f, 1f);     // #191970
        private static readonly Color SpectralCyan = new Color(0f, 0.81f, 0.82f, 1f);         // #00CED1
        private static readonly Color TextSecondary = new Color(0.65f, 0.60f, 0.52f, 1f);
        private static readonly Color DividerColor = new Color(0.81f, 0.71f, 0.23f, 0.4f);
        private static readonly Color BtnSelected = new Color(0.55f, 0f, 0f, 1f);             // Deep Crimson

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
            WireStarterEquipmentIfMissing(ctrl);
            FindBodyPartRegistry(ctrl);
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

            // Gothic title
            var title = MakeLabel(content.transform, "Speak Thy Name", 42f);
            title.color = FrameGold;
            MakeSpacer(content.transform, 6f);

            MakeSpacer(content.transform, 20f);

            // Input field with gold border
            var inputFrame = MakeGothicFrame(content.transform, 440f, 54f);
            ctrl.nameInputField = MakeInputFieldInto(inputFrame);
            MakeSpacer(content.transform, 6f);

            var err = MakeLabel(content.transform, "", 18f);
            err.color = new Color(0.9f, 0.3f, 0.3f, 1f);
            err.gameObject.SetActive(false);
            ctrl.nameErrorText = err;
            MakeSpacer(content.transform, 24f);

            // Nav buttons
            var row = MakeHRow(content.transform, 20f, 55f);
            ctrl.nameBackButton = MakeButton(row.transform, "Back", 180f, 50f);
            var backTmp = ctrl.nameBackButton.GetComponentInChildren<TextMeshProUGUI>();
            if (backTmp != null) backTmp.fontSize = 24f;

            ctrl.nameConfirmButton = MakeButton(row.transform, "Next", 200f, 50f);
            var nextColors = ctrl.nameConfirmButton.colors;
            nextColors.normalColor = DeepCrimson;
            nextColors.highlightedColor = new Color(0.65f, 0.05f, 0.05f, 1f);
            nextColors.pressedColor = new Color(0.40f, 0f, 0f, 1f);
            nextColors.selectedColor = new Color(0.65f, 0.05f, 0.05f, 1f);
            ctrl.nameConfirmButton.colors = nextColors;
            var nextTmp = ctrl.nameConfirmButton.GetComponentInChildren<TextMeshProUGUI>();
            if (nextTmp != null)
            {
                nextTmp.fontSize = 24f;
                nextTmp.color = FrameGold;
            }
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
            contentVlg.spacing = 12f;
            contentVlg.padding = new RectOffset(20, 20, 20, 20);

            var contentCsf = content.AddComponent<ContentSizeFitter>();
            contentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Gothic title
            var title = MakeLabel(content.transform, "Choose Your Path", 34f);
            title.color = FrameGold;
            MakeSpacer(content.transform, 8f);

            // Class cards row
            var classRow = MakeHRow(content.transform, 60f, 530f);

            ctrl.warriorButton = MakeClassCard(classRow.transform, "Warrior", out var warriorImg, out var warriorLP, out var warriorBorder);
            ctrl.warriorPreviewImage = warriorImg;
            ctrl.warriorLayeredPreview = warriorLP;
            ctrl.warriorSelectionBorder = warriorBorder;

            ctrl.mageButton = MakeClassCard(classRow.transform, "Mage", out var mageImg, out var mageLP, out var mageBorder);
            ctrl.magePreviewImage = mageImg;
            ctrl.mageLayeredPreview = mageLP;
            ctrl.mageSelectionBorder = mageBorder;

            ctrl.rogueButton = MakeClassCard(classRow.transform, "Rogue", out var rogueImg, out var rogueLP, out var rogueBorder);
            ctrl.roguePreviewImage = rogueImg;
            ctrl.rogueLayeredPreview = rogueLP;
            ctrl.rogueSelectionBorder = rogueBorder;

            // Gold divider
            var divGo = MakeUIObject("Divider", content.transform);
            divGo.AddComponent<Image>().color = DividerColor;
            var divLayout = divGo.AddComponent<LayoutElement>();
            divLayout.preferredHeight = 1f;
            divLayout.preferredWidth = 600f;

            // Class name (gold when selected)
            ctrl.classNameText = MakeLabel(content.transform, "Which road will you travel?", 32f);
            ctrl.classNameText.color = FrameGold;
            ctrl.classNameText.GetComponent<LayoutElement>().preferredWidth = -1f;

            // Description
            ctrl.classDescriptionText = MakeLabel(content.transform, "", 22f);
            ctrl.classDescriptionText.color = TextSecondary;
            ctrl.classDescriptionText.GetComponent<LayoutElement>().preferredWidth = -1f;
            MakeAutoHeight(ctrl.classDescriptionText.gameObject);

            MakeSpacer(content.transform, 8f);

            // Nav buttons
            var navRow = MakeHRow(content.transform, 20f, 55f);
            ctrl.classBackButton = MakeButton(navRow.transform, "Back", 200f, 50f);
            var backTmp = ctrl.classBackButton.GetComponentInChildren<TextMeshProUGUI>();
            if (backTmp != null) backTmp.fontSize = 26f;

            ctrl.classConfirmButton = MakeButton(navRow.transform, "Begin", 240f, 50f);
            var confirmColors = ctrl.classConfirmButton.colors;
            confirmColors.normalColor = DeepCrimson;
            confirmColors.highlightedColor = new Color(0.65f, 0.05f, 0.05f, 1f);
            confirmColors.pressedColor = new Color(0.40f, 0f, 0f, 1f);
            confirmColors.selectedColor = new Color(0.65f, 0.05f, 0.05f, 1f);
            ctrl.classConfirmButton.colors = confirmColors;
            var confirmTmp = ctrl.classConfirmButton.GetComponentInChildren<TextMeshProUGUI>();
            if (confirmTmp != null)
            {
                confirmTmp.fontSize = 26f;
                confirmTmp.color = FrameGold;
            }
            ctrl.classConfirmButton.interactable = false;
        }

        private static void BuildAppearancePanel(CharacterCreationController ctrl, Transform parent)
        {
            var panel = MakeDarkPanel(parent, "AppearancePanel");
            panel.SetActive(false);
            ctrl.appearancePanel = panel;

            var content = MakeContentColumn(panel.transform);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.sizeDelta = new Vector2(900f, 0f);

            // Gothic title
            var title = MakeLabel(content.transform, "Forge Your Likeness", 38f);
            title.color = FrameGold;
            MakeSpacer(content.transform, 4f);

            // === Two-column layout ===
            var columnsRow = MakeUIObject("ColumnsRow", content.transform);
            var columnsHlg = columnsRow.AddComponent<HorizontalLayoutGroup>();
            columnsHlg.childAlignment = TextAnchor.UpperCenter;
            columnsHlg.childControlWidth = true;
            columnsHlg.childControlHeight = true;
            columnsHlg.childForceExpandWidth = false;
            columnsHlg.childForceExpandHeight = true;
            columnsHlg.spacing = 20f;
            var columnsLayout = columnsRow.AddComponent<LayoutElement>();
            columnsLayout.flexibleWidth = 1f;

            // --- Left column: preview + body type ---
            var leftCol = MakeUIObject("LeftColumn", columnsRow.transform);
            var leftVlg = leftCol.AddComponent<VerticalLayoutGroup>();
            leftVlg.childAlignment = TextAnchor.UpperCenter;
            leftVlg.childControlWidth = true;
            leftVlg.childControlHeight = true;
            leftVlg.childForceExpandWidth = false;
            leftVlg.childForceExpandHeight = false;
            leftVlg.spacing = 6f;
            var leftLayout = leftCol.AddComponent<LayoutElement>();
            leftLayout.preferredWidth = 340f;

            var previewFrame = MakeGothicFrame(leftCol.transform, 280f, 360f);
            ctrl.appearancePreview = previewFrame.AddComponent<UILayeredSpritePreview>();

            var bodyTypeRow = MakeHRow(leftCol.transform, 10f, 45f);
            var btLabel = MakeLabel(bodyTypeRow.transform, "Body Type", 22f);
            btLabel.textWrappingMode = TextWrappingModes.NoWrap;
            btLabel.GetComponent<LayoutElement>().preferredWidth = 110f;
            ctrl.bodyTypeMaleButton = MakeButton(bodyTypeRow.transform, "A", 90f, 40f);
            ctrl.bodyTypeFemaleButton = MakeButton(bodyTypeRow.transform, "B", 90f, 40f);
            ctrl.bodyTypeLabel = MakeLabel(bodyTypeRow.transform, "A", 20f);
            ctrl.bodyTypeLabel.GetComponent<LayoutElement>().preferredWidth = 0f;
            ctrl.bodyTypeLabel.gameObject.SetActive(false);

            // --- Right column: options in scroll view ---
            var rightCol = MakeUIObject("RightColumn", columnsRow.transform);
            var rightVlg = rightCol.AddComponent<VerticalLayoutGroup>();
            rightVlg.childAlignment = TextAnchor.UpperCenter;
            rightVlg.childControlWidth = true;
            rightVlg.childControlHeight = true;
            rightVlg.childForceExpandWidth = true;
            rightVlg.childForceExpandHeight = true;
            rightVlg.spacing = 0f;
            var rightLayout = rightCol.AddComponent<LayoutElement>();
            rightLayout.flexibleWidth = 1f;

            // ScrollRect wrapping the options
            var scrollGo = MakeUIObject("ScrollArea", rightCol.transform);
            Stretch(scrollGo);
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;
            var scrollLayout = scrollGo.AddComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1f;
            scrollLayout.flexibleWidth = 1f;

            var viewport = MakeUIObject("Viewport", scrollGo.transform);
            Stretch(viewport);
            viewport.AddComponent<RectMask2D>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();

            var scrollContent = MakeUIObject("ScrollContent", viewport.transform);
            var scrollContentRt = scrollContent.GetComponent<RectTransform>();
            scrollContentRt.anchorMin = new Vector2(0f, 1f);
            scrollContentRt.anchorMax = new Vector2(1f, 1f);
            scrollContentRt.pivot = new Vector2(0.5f, 1f);
            scrollContentRt.sizeDelta = new Vector2(0f, 0f);
            var scrollVlg = scrollContent.AddComponent<VerticalLayoutGroup>();
            scrollVlg.childAlignment = TextAnchor.UpperCenter;
            scrollVlg.childControlWidth = true;
            scrollVlg.childControlHeight = true;
            scrollVlg.childForceExpandWidth = true;
            scrollVlg.childForceExpandHeight = false;
            scrollVlg.spacing = 5f;
            scrollVlg.padding = new RectOffset(5, 5, 0, 5);
            var scrollCsf = scrollContent.AddComponent<ContentSizeFitter>();
            scrollCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = scrollContentRt;

            // === FEATURES section header ===
            MakeSectionHeader(scrollContent.transform, "FEATURES");

            // Hair style
            MakeOptionRow(scrollContent.transform, "Hair Style", out ctrl.hairPrevButton, out ctrl.hairNextButton, out ctrl.hairNameText);

            // Beard
            MakeOptionRow(scrollContent.transform, "Beard", out ctrl.beardPrevButton, out ctrl.beardNextButton, out ctrl.beardNameText);

            // === COLORS section header ===
            MakeSectionHeader(scrollContent.transform, "COLORS");

            // Eye color
            MakeColorRow(scrollContent.transform, "Eye Color", EyeColorPresets[0], EyeColorNames[0],
                out ctrl.eyeColorPrevButton, out ctrl.eyeColorNextButton, out ctrl.eyeColorPreview, out ctrl.eyeColorNameText);

            // Skin tone
            MakeColorRow(scrollContent.transform, "Skin Tone", SkinTonePresets[0], SkinToneNames[0],
                out ctrl.skinPrevButton, out ctrl.skinNextButton, out ctrl.skinColorPreview, out ctrl.skinToneNameText);

            // Hair color
            MakeColorRow(scrollContent.transform, "Hair Color", HairColorPresets[0], HairColorNames[0],
                out ctrl.hairColorPrevButton, out ctrl.hairColorNextButton, out ctrl.hairColorPreview, out ctrl.hairColorNameText);

            MakeSpacer(content.transform, 10f);

            // Nav buttons
            var navRow = MakeHRow(content.transform, 20f, 55f);
            ctrl.appearanceBackButton = MakeButton(navRow.transform, "Back", 200f, 50f);
            var backTmp = ctrl.appearanceBackButton.GetComponentInChildren<TextMeshProUGUI>();
            if (backTmp != null) backTmp.fontSize = 26f;

            ctrl.appearanceConfirmButton = MakeButton(navRow.transform, "Next", 240f, 50f);
            var appConfirmColors = ctrl.appearanceConfirmButton.colors;
            appConfirmColors.normalColor = DeepCrimson;
            appConfirmColors.highlightedColor = new Color(0.65f, 0.05f, 0.05f, 1f);
            appConfirmColors.pressedColor = new Color(0.40f, 0f, 0f, 1f);
            appConfirmColors.selectedColor = new Color(0.65f, 0.05f, 0.05f, 1f);
            ctrl.appearanceConfirmButton.colors = appConfirmColors;
            var confirmTmp = ctrl.appearanceConfirmButton.GetComponentInChildren<TextMeshProUGUI>();
            if (confirmTmp != null)
            {
                confirmTmp.fontSize = 26f;
                confirmTmp.color = FrameGold;
            }
        }

        private static void FindBodyPartRegistry(CharacterCreationController ctrl)
        {
            if (ctrl.bodyPartRegistry != null) return;

            ctrl.bodyPartRegistry = Resources.Load<BodyPartRegistry>("BodyPartRegistry");
        }

        /// <summary>
        /// Builds a class card: vertical container with preview image on top and button below.
        /// Includes a gold selection border that toggles on/off.
        /// </summary>
        private static Button MakeClassCard(Transform parent, string className,
            out Image previewImage, out UILayeredSpritePreview layeredPreview,
            out GameObject selectionBorder)
        {
            var cardGo = MakeUIObject(className + "Card", parent);
            var cardLayout = cardGo.AddComponent<LayoutElement>();
            cardLayout.preferredWidth = 380f;
            cardLayout.preferredHeight = 560f;

            var vlg = cardGo.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 8f;

            // Preview image container with midnight blue tint
            var imgContainer = MakeUIObject("PreviewContainer", cardGo.transform);
            var containerImg = imgContainer.AddComponent<Image>();
            containerImg.color = MidnightBlue;
            var containerLayout = imgContainer.AddComponent<LayoutElement>();
            containerLayout.preferredHeight = 440f;

            // Selection border (gold, fills container — hidden by default)
            var borderGo = MakeUIObject("SelectionBorder", imgContainer.transform);
            var borderImg = borderGo.AddComponent<Image>();
            borderImg.color = FrameGold;
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            selectionBorder = borderGo;
            borderGo.SetActive(false);

            // Inner bg (inset from border to reveal gold edges)
            var innerBg = MakeUIObject("InnerBg", imgContainer.transform);
            var innerBgImg = innerBg.AddComponent<Image>();
            innerBgImg.color = MidnightBlue;
            var innerBgRect = innerBg.GetComponent<RectTransform>();
            innerBgRect.anchorMin = Vector2.zero;
            innerBgRect.anchorMax = Vector2.one;
            innerBgRect.offsetMin = new Vector2(3f, 3f);
            innerBgRect.offsetMax = new Vector2(-3f, -3f);

            // Preview image (fills container, preserves aspect ratio) — fallback when no layered appearance
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

            // Layered character preview (on top of fallback image, stretched to fill)
            var layeredGo = MakeUIObject("LayeredPreview", imgContainer.transform);
            var layeredRect = layeredGo.GetComponent<RectTransform>();
            layeredRect.anchorMin = Vector2.zero;
            layeredRect.anchorMax = Vector2.one;
            layeredRect.offsetMin = Vector2.zero;
            layeredRect.offsetMax = Vector2.zero;
            layeredPreview = layeredGo.AddComponent<UILayeredSpritePreview>();

            // Gold divider between preview and button
            var dividerGo = MakeUIObject("Divider", cardGo.transform);
            var dividerImg = dividerGo.AddComponent<Image>();
            dividerImg.color = DividerColor;
            var dividerLayout = dividerGo.AddComponent<LayoutElement>();
            dividerLayout.preferredHeight = 2f;

            // Class button with deep crimson
            var btn = MakeButton(cardGo.transform, className, 380f, 70f);
            var btnColors = btn.colors;
            btnColors.normalColor = DeepCrimson;
            btnColors.highlightedColor = new Color(0.65f, 0.05f, 0.05f, 1f);
            btnColors.pressedColor = new Color(0.40f, 0f, 0f, 1f);
            btnColors.selectedColor = new Color(0.65f, 0.05f, 0.05f, 1f);
            btn.colors = btnColors;
            var btnTmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnTmp != null)
            {
                btnTmp.fontSize = 42f;
                btnTmp.color = FrameGold;
            }

            return btn;
        }

        /// <summary>
        /// Attempts to load class preview sprites. Called early from CreateRuntimeUI
        /// and again deferred from ShowClassSelection when more assets are in memory.
        /// Uses JobClassData visuals when available, falls back to class-themed tints.
        /// </summary>
        private static void LoadClassPreviewSprites(CharacterCreationController ctrl)
        {
            // Ensure UIAnimatedSprite components exist on preview images
            EnsureAnimatedSprite(ctrl.warriorPreviewImage, ref ctrl.warriorAnimSprite);
            EnsureAnimatedSprite(ctrl.magePreviewImage, ref ctrl.mageAnimSprite);
            EnsureAnimatedSprite(ctrl.roguePreviewImage, ref ctrl.rogueAnimSprite);

            // Try JobClassData visuals; layered preview handles the actual display
            TryApplyJobPreview(ctrl.warriorPreviewImage, ctrl.warriorData);
            TryApplyJobPreview(ctrl.magePreviewImage, ctrl.mageData);
            TryApplyJobPreview(ctrl.roguePreviewImage, ctrl.rogueData);
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
        /// Returns true if a valid (non-null) sprite was found and applied.
        /// Searches defaultSprite first, then all idlePreviewFrames entries.
        /// </summary>
        private static bool TryApplyJobPreview(Image image, JobClassData jobData)
        {
            if (image == null || jobData == null)
                return false;

            Sprite preview = jobData.defaultSprite;
            if (preview == null && jobData.idlePreviewFrames != null)
            {
                foreach (var frame in jobData.idlePreviewFrames)
                {
                    if (frame != null)
                    {
                        preview = frame;
                        break;
                    }
                }
            }

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
        /// Sets class-themed background tints when no visual data exists.
        /// The layered preview system handles the primary display.
        /// </summary>
        private void TryLoadClassSprites()
        {
            ApplyClassTintIfMissing(warriorPreviewImage, warriorData);
            ApplyClassTintIfMissing(magePreviewImage, mageData);
            ApplyClassTintIfMissing(roguePreviewImage, rogueData);
        }

        private static void ApplyClassTintIfMissing(Image image, JobClassData jobData)
        {
            if (image == null || jobData == null) return;
            if (HasJobVisualData(jobData)) return;

            image.sprite = null;
            image.color = jobData.jobColor;
        }

        private static void FindJobData(CharacterCreationController ctrl)
        {
            // First priority: SkillManager already holds resolved asset references.
            // These point to the real ScriptableObjects (with visual data) when the
            // project assets were loaded before the runtime fallback was created.
            if (SkillManager.Instance != null)
            {
                var smJobs = Resources.LoadAll<JobClassData>("Jobs");
                foreach (var job in smJobs)
                {
                    if (job == null || string.IsNullOrEmpty(job.jobId)) continue;
                    string id = job.jobId.ToLower();

                    if (id == "warrior" && (ctrl.warriorData == null || HasJobVisualData(job)))
                        ctrl.warriorData = job;
                    else if (id == "mage" && (ctrl.mageData == null || HasJobVisualData(job)))
                        ctrl.mageData = job;
                    else if (id == "rogue" && (ctrl.rogueData == null || HasJobVisualData(job)))
                        ctrl.rogueData = job;
                }
            }

            // Second pass: search all loaded JobClassData assets.
            // Always upgrade to an asset with visual data if we have one without.
            var allJobs = Resources.LoadAll<JobClassData>("Jobs");
            foreach (var job in allJobs)
            {
                if (string.IsNullOrEmpty(job.jobName)) continue;
                string n = job.jobName.ToLower();

                // Prefer assets with visual data; always replace if current has none
                if (n.Contains("warrior"))
                {
                    if (ctrl.warriorData == null
                        || (HasJobVisualData(job) && !HasJobVisualData(ctrl.warriorData)))
                        ctrl.warriorData = job;
                }
                else if (n.Contains("mage"))
                {
                    if (ctrl.mageData == null
                        || (HasJobVisualData(job) && !HasJobVisualData(ctrl.mageData)))
                        ctrl.mageData = job;
                }
                else if (n.Contains("rogue"))
                {
                    if (ctrl.rogueData == null
                        || (HasJobVisualData(job) && !HasJobVisualData(ctrl.rogueData)))
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

        /// <summary>
        /// Loads starter equipment from Resources for any job that is missing it.
        /// This handles the case where FindJobData creates runtime fallback instances
        /// or finds assets that aren't fully loaded yet.
        /// </summary>
        private static void WireStarterEquipmentIfMissing(CharacterCreationController ctrl)
        {
            WireJobEquipment(ctrl.warriorData, "warrior_sword", "warrior_chainmail", "warrior_greaves");
            WireJobEquipment(ctrl.mageData, "mage_staff", "mage_robe", "mage_shoes");
            WireJobEquipment(ctrl.rogueData, "rogue_dagger", "rogue_vest", "rogue_boots");
        }

        private static void WireJobEquipment(JobClassData job, params string[] equipmentIds)
        {
            if (job == null) return;
            if (job.starterEquipment != null && job.starterEquipment.Length > 0) return;

            var equipment = new System.Collections.Generic.List<EquipmentData>();
            foreach (var id in equipmentIds)
            {
                var equip = Resources.Load<EquipmentData>($"Equipment/{id}");
                if (equip != null) equipment.Add(equip);
            }

            if (equipment.Count > 0)
                job.starterEquipment = equipment.ToArray();
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

        /// <summary>
        /// Creates a gothic frame: a gold-bordered container with dark inner background.
        /// </summary>
        private static GameObject MakeGothicFrame(Transform parent, float width, float height)
        {
            var outerGo = MakeUIObject("GothicFrame", parent);
            var outerLayout = outerGo.AddComponent<LayoutElement>();
            outerLayout.preferredWidth = width + 8f;
            outerLayout.preferredHeight = height + 8f;

            // Gold border (outer image)
            var borderImg = outerGo.AddComponent<Image>();
            borderImg.color = FrameGold;

            // Dark inner background
            var innerGo = MakeUIObject("Inner", outerGo.transform);
            var innerImg = innerGo.AddComponent<Image>();
            innerImg.color = new Color(0.08f, 0.07f, 0.10f, 1f);
            var innerRect = innerGo.GetComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(3f, 3f);
            innerRect.offsetMax = new Vector2(-3f, -3f);

            return innerGo;
        }

        /// <summary>
        /// Creates a section header with gold text and a divider line.
        /// </summary>
        private static void MakeSectionHeader(Transform parent, string title)
        {
            MakeSpacer(parent, 4f);

            // Build row without ContentSizeFitter so dividers stretch
            // to fill width from the parent VLG
            var go = MakeUIObject("SectionHeader", parent);
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.spacing = 8f;

            var rowLayout = go.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 24f;

            // Left divider — flexible width to fill available space
            var leftDiv = MakeUIObject("DivL", go.transform);
            leftDiv.AddComponent<Image>().color = DividerColor;
            var ldLayout = leftDiv.AddComponent<LayoutElement>();
            ldLayout.flexibleWidth = 1f;
            ldLayout.preferredHeight = 1f;

            // Title text — fixed width
            var label = MakeLabel(go.transform, title, 18f);
            label.color = FrameGold;
            label.GetComponent<LayoutElement>().preferredWidth = 120f;

            // Right divider — flexible width to fill available space
            var rightDiv = MakeUIObject("DivR", go.transform);
            rightDiv.AddComponent<Image>().color = DividerColor;
            var rdLayout = rightDiv.AddComponent<LayoutElement>();
            rdLayout.flexibleWidth = 1f;
            rdLayout.preferredHeight = 1f;
        }

        /// <summary>
        /// Creates an option row with label, left/right arrow buttons, and name text.
        /// Uses Unicode triangles for arrow buttons.
        /// </summary>
        private static void MakeOptionRow(Transform parent, string labelText,
            out Button prevBtn, out Button nextBtn, out TMP_Text nameText)
        {
            var row = MakeHRow(parent, 10f, 42f);
            var label = MakeLabel(row.transform, labelText, 20f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.GetComponent<LayoutElement>().preferredWidth = 130f;
            label.alignment = TextAlignmentOptions.MidlineRight;
            label.color = TextSecondary;

            prevBtn = MakeArrowButton(row.transform, "\u25C0"); // filled left triangle
            nameText = MakeLabel(row.transform, "None", 19f);
            nameText.GetComponent<LayoutElement>().preferredWidth = 180f;
            nextBtn = MakeArrowButton(row.transform, "\u25B6"); // filled right triangle
        }

        /// <summary>
        /// Creates a color row with label, left/right arrows, a color swatch, and name text.
        /// </summary>
        private static void MakeColorRow(Transform parent, string labelText,
            Color initialColor, string initialName,
            out Button prevBtn, out Button nextBtn, out Image colorPreview, out TMP_Text nameText)
        {
            var row = MakeHRow(parent, 10f, 42f);
            var label = MakeLabel(row.transform, labelText, 20f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.GetComponent<LayoutElement>().preferredWidth = 130f;
            label.alignment = TextAlignmentOptions.MidlineRight;
            label.color = TextSecondary;

            prevBtn = MakeArrowButton(row.transform, "\u25C0");

            // Color swatch with gold border
            var swatchOuter = MakeUIObject("SwatchBorder", row.transform);
            swatchOuter.AddComponent<Image>().color = FrameGold;
            var outerLE = swatchOuter.AddComponent<LayoutElement>();
            outerLE.preferredWidth = 62f;
            outerLE.preferredHeight = 37f;

            var swatchInner = MakeUIObject("Swatch", swatchOuter.transform);
            var swatchImg = swatchInner.AddComponent<Image>();
            swatchImg.color = initialColor;
            var swatchRect = swatchInner.GetComponent<RectTransform>();
            swatchRect.anchorMin = Vector2.zero;
            swatchRect.anchorMax = Vector2.one;
            swatchRect.offsetMin = new Vector2(1f, 1f);
            swatchRect.offsetMax = new Vector2(-1f, -1f);
            colorPreview = swatchImg;

            // Name text
            var nameTmp = MakeLabel(row.transform, initialName, 18f);
            nameTmp.GetComponent<LayoutElement>().preferredWidth = 80f;
            nameText = nameTmp;

            nextBtn = MakeArrowButton(row.transform, "\u25B6");
        }

        /// <summary>
        /// Creates a gold-text arrow button on midnight blue background.
        /// </summary>
        private static Button MakeArrowButton(Transform parent, string symbol)
        {
            var btn = MakeButton(parent, symbol, 44f, 38f);
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = symbol;
                tmp.fontSize = 20f;
                tmp.color = FrameGold;
            }
            return btn;
        }

        private static void MakeSpacer(Transform parent, float height)
        {
            var go = MakeUIObject("Spacer", parent);
            var layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.flexibleWidth = 1f;
        }

        /// <summary>
        /// Creates an input field inside an existing container (e.g. a gothic frame inner panel).
        /// The container is stretched to fill, so the input conforms to the frame.
        /// </summary>
        private static TMP_InputField MakeInputFieldInto(GameObject container)
        {
            // Text area with mask
            var textArea = MakeUIObject("Text Area", container.transform);
            Stretch(textArea);
            var taRt = textArea.GetComponent<RectTransform>();
            taRt.offsetMin = new Vector2(12f, 4f);
            taRt.offsetMax = new Vector2(-12f, -4f);
            textArea.AddComponent<RectMask2D>();

            // Input text
            var textGo = MakeUIObject("Text", textArea.transform);
            Stretch(textGo);
            var textTmp = textGo.AddComponent<TextMeshProUGUI>();
            textTmp.fontSize = 26f;
            textTmp.color = TextCol;
            textTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Placeholder
            var phGo = MakeUIObject("Placeholder", textArea.transform);
            Stretch(phGo);
            var phTmp = phGo.AddComponent<TextMeshProUGUI>();
            phTmp.text = "What name echoes in the dark?";
            phTmp.fontSize = 24f;
            phTmp.fontStyle = FontStyles.Italic;
            phTmp.color = new Color(0.50f, 0.45f, 0.35f, 0.5f);
            phTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Wire TMP_InputField
            var inputField = container.AddComponent<TMP_InputField>();
            inputField.textComponent = textTmp;
            inputField.placeholder = phTmp;
            inputField.textViewport = taRt;
            inputField.pointSize = 26;
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
            if (appearanceBackButton != null) appearanceBackButton.onClick.RemoveAllListeners();
            if (appearanceConfirmButton != null) appearanceConfirmButton.onClick.RemoveAllListeners();
            if (hairPrevButton != null) hairPrevButton.onClick.RemoveAllListeners();
            if (hairNextButton != null) hairNextButton.onClick.RemoveAllListeners();
            if (skinPrevButton != null) skinPrevButton.onClick.RemoveAllListeners();
            if (skinNextButton != null) skinNextButton.onClick.RemoveAllListeners();
            if (hairColorPrevButton != null) hairColorPrevButton.onClick.RemoveAllListeners();
            if (hairColorNextButton != null) hairColorNextButton.onClick.RemoveAllListeners();
            if (beardPrevButton != null) beardPrevButton.onClick.RemoveAllListeners();
            if (beardNextButton != null) beardNextButton.onClick.RemoveAllListeners();
            if (eyeColorPrevButton != null) eyeColorPrevButton.onClick.RemoveAllListeners();
            if (eyeColorNextButton != null) eyeColorNextButton.onClick.RemoveAllListeners();
        }
    }
}
