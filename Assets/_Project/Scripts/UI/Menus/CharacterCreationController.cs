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
        [SerializeField] private Button classPrevButton;
        [SerializeField] private Button classNextButton;
        [SerializeField] private Button classBackButton;
        [SerializeField] private Button classConfirmButton;
        [SerializeField] private TMP_Text classNameText;
        [SerializeField] private TMP_Text classDescriptionText;
        [SerializeField] private TMP_Text classStatsPreviewText;
        [SerializeField] private TMP_Text classEquipmentText;
        [SerializeField] private TMP_Text classPerLevelText;
        [SerializeField] private Image classPreviewImage;

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

        // Class cycling state
        private JobClassData[] availableClasses;
        private int selectedClassIndex;
        private UILayeredSpritePreview classLayeredPreview;
        private UIAnimatedSprite classAnimSprite;

        // Creation data
        private string characterName = "";
        private JobClassData selectedClass;
        private int targetSlotIndex = -1;
        private CreationStep currentStep;

        // Appearance selection state
        private BodyPartData[] availableHairStyles;
        private int selectedHairIndex;
        private CharacterAppearanceConfig builtAppearance;
        private string selectedBodyType = "male";

        // Beard cycling state
        private BodyPartData[] availableBeardStyles;
        private int selectedBeardIndex = -1; // -1 = None

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

        // Maps skin tone index to body/head partId suffixes (for pre-baked ULPC variants)
        private static readonly string[] SkinToneSuffixes = new string[]
        {
            "light", "amber", "olive", "taupe", "bronze", "brown", "black"
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

            // Class selection — cycling arrows
            if (classPrevButton != null)
            {
                classPrevButton.onClick.RemoveAllListeners();
                classPrevButton.onClick.AddListener(() => CycleClass(-1));
            }
            if (classNextButton != null)
            {
                classNextButton.onClick.RemoveAllListeners();
                classNextButton.onClick.AddListener(() => CycleClass(1));
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
            selectedClassIndex = 0;
            availableClasses = null;
            selectedHairIndex = 0;
            selectedSkinToneIndex = 0;
            selectedHairColorIndex = 0;
            selectedBeardIndex = -1;
            selectedEyeColorIndex = 0;
            builtAppearance = null;
            selectedBodyType = "male";

            if (appearancePreview != null)
                appearancePreview.Clear();
            if (classLayeredPreview != null)
                classLayeredPreview.Clear();
            if (classPreviewImage != null)
            {
                classPreviewImage.enabled = true;
                classPreviewImage.color = Color.clear;
            }

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

            // Build the class list on first show
            if (availableClasses == null)
            {
                var classes = new List<JobClassData>();
                if (warriorData != null) classes.Add(warriorData);
                if (mageData != null) classes.Add(mageData);
                if (rogueData != null) classes.Add(rogueData);
                availableClasses = classes.ToArray();
            }

            // Ensure animated sprite component exists
            if (classAnimSprite == null && classPreviewImage != null)
                classAnimSprite = classPreviewImage.gameObject.GetComponent<UIAnimatedSprite>()
                    ?? classPreviewImage.gameObject.AddComponent<UIAnimatedSprite>();

            // Show the current selection (or first class)
            if (availableClasses.Length > 0)
                ApplyClassSelection(selectedClassIndex);

            if (classConfirmButton != null)
                classConfirmButton.interactable = selectedClass != null;

            // Set initial focus for keyboard/controller navigation
            if (classNextButton != null)
            {
                var es = UnityEngine.EventSystems.EventSystem.current;
                if (es != null)
                    es.SetSelectedGameObject(classNextButton.gameObject);
            }
        }

        private void CycleClass(int direction)
        {
            if (availableClasses == null || availableClasses.Length == 0) return;
            selectedClassIndex = (selectedClassIndex + direction + availableClasses.Length) % availableClasses.Length;
            ApplyClassSelection(selectedClassIndex);
            UIManager.Instance?.PlaySelectSound();
        }

        private void ApplyClassSelection(int index)
        {
            if (availableClasses == null || index < 0 || index >= availableClasses.Length) return;

            var classData = availableClasses[index];
            selectedClass = classData;

            if (classConfirmButton != null)
                classConfirmButton.interactable = true;

            // Name
            if (classNameText != null)
                classNameText.text = classData.jobName;

            // Description
            if (classDescriptionText != null)
                classDescriptionText.text = classData.description;

            // Stats (left column: base modifiers)
            if (classStatsPreviewText != null)
                classStatsPreviewText.text = FormatClassStats(classData);

            // Stats (right column: per-level growth)
            if (classPerLevelText != null)
                classPerLevelText.text = FormatPerLevelStats(classData);

            // Equipment list
            if (classEquipmentText != null)
                classEquipmentText.text = FormatStarterEquipment(classData);

            // Layered character preview with class gear
            ApplyAppearanceToCard(classLayeredPreview, classPreviewImage, classData);

            // Animated preview fallback
            if (classAnimSprite != null && HasJobVisualData(classData))
            {
                if (classData.idlePreviewFrames != null && classData.idlePreviewFrames.Length > 1)
                {
                    classAnimSprite.Play(classData.idlePreviewFrames, classData.idlePreviewFrameRate);
                }
                else
                {
                    classAnimSprite.Stop();
                }
            }
        }

        private static string FormatClassStats(JobClassData classData)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"HP   +{classData.baseHPBonus}");
            sb.AppendLine($"MP   +{classData.baseMPBonus}");
            sb.AppendLine($"ATK  x{classData.attackModifier:F1}");
            sb.AppendLine($"MAG  x{classData.magicModifier:F1}");
            sb.Append($"DEF  x{classData.defenseModifier:F1}");
            return sb.ToString();
        }

        private static string FormatPerLevelStats(JobClassData classData)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<color=#CFB53B>Per Level</color>");
            sb.AppendLine($"STR  +{classData.strPerLevel}");
            sb.AppendLine($"INT  +{classData.intPerLevel}");
            sb.Append($"AGI  +{classData.agiPerLevel}");
            return sb.ToString();
        }

        private static string FormatStarterEquipment(JobClassData classData)
        {
            if (classData.starterEquipment == null || classData.starterEquipment.Length == 0)
                return "None";

            var sb = new System.Text.StringBuilder();
            foreach (var equip in classData.starterEquipment)
            {
                if (equip == null) continue;
                string name = !string.IsNullOrEmpty(equip.displayName) ? equip.displayName : equip.equipmentId;
                string stats = equip.GetStatSummary();
                if (!string.IsNullOrEmpty(stats))
                    sb.AppendLine($"{name}  ({stats})");
                else
                    sb.AppendLine(name);
            }
            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Ensures a default appearance exists for class preview rendering.
        /// Called before applying class gear to the layered preview.
        /// </summary>
        private void EnsureDefaultAppearance()
        {
            if (builtAppearance != null) return;

            if (bodyPartRegistry == null)
                bodyPartRegistry = Resources.Load<BodyPartRegistry>("BodyPartRegistry");

            if (bodyPartRegistry == null) return;

            builtAppearance = ScriptableObject.CreateInstance<CharacterAppearanceConfig>();
            builtAppearance.bodyType = selectedBodyType;
            builtAppearance.skinTint = SkinTonePresets[selectedSkinToneIndex];

            // Use pre-baked skin tone body + head
            ApplySkinToneSwap();

            // Fallback if swap didn't find assets
            if (builtAppearance.body == null)
            {
                var bodyParts = bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Body);
                if (bodyParts.Length > 0) builtAppearance.body = bodyParts[0];
            }
            if (builtAppearance.head == null)
            {
                var headParts = bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Head);
                var faceHeads = FilterByPrefix(headParts, "head_");
                builtAppearance.head = FindDefaultHumanHead(faceHeads, headParts, selectedBodyType);
            }

            var hairParts = bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Hair);
            if (hairParts.Length > 0) builtAppearance.hair = hairParts[0];
            builtAppearance.hairTint = HairColorPresets[0];

            var eyeParts = bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Eyes);
            BodyPartData defaultEye = null;
            foreach (var ep in eyeParts)
            {
                if (ep.partId != null && ep.partId.StartsWith("eye_color_"))
                { defaultEye = ep; break; }
            }
            if (defaultEye == null && eyeParts.Length > 0) defaultEye = eyeParts[0];
            if (defaultEye != null) builtAppearance.SetPart(BodyPartSlot.Eyes, defaultEye);
            builtAppearance.eyeTint = EyeColorPresets[0];

            AssignDefaultClothing();
        }

        private void ApplyAppearanceToCard(UILayeredSpritePreview preview, Image fallbackImage, JobClassData jobData)
        {
            EnsureDefaultAppearance();
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
                    if (equip == null) continue;
                    if (equip.visualPart == null)
                    {
                        Debug.LogWarning($"[CharCreator] '{equip.equipmentId}' has null visualPart");
                        continue;
                    }
                    switch (equip.slotType)
                    {
                        case EquipmentSlotType.Armor:
                            merged.torso = equip.visualPart;
                            hasEquipment = true;
                            break;
                        case EquipmentSlotType.Legs:
                            merged.legs = equip.visualPart;
                            hasEquipment = true;
                            break;
                        case EquipmentSlotType.Feet:
                            merged.SetPart(BodyPartSlot.Feet, equip.visualPart);
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

            // Only initialize appearance if not already built (preserve choices when navigating back)
            if (builtAppearance == null)
            {
                selectedHairIndex = availableHairStyles != null && availableHairStyles.Length > 0
                    ? FindPartIndex(availableHairStyles, "hair_messy1") : 0;
                selectedSkinToneIndex = 0;
                selectedHairColorIndex = 3; // Brown
                selectedBeardIndex = -1;
                selectedEyeColorIndex = 0;

                builtAppearance = ScriptableObject.CreateInstance<CharacterAppearanceConfig>();
                builtAppearance.bodyType = selectedBodyType;

                // Start with first skin tone's pre-baked body + head
                builtAppearance.skinTint = SkinTonePresets[0];
                ApplySkinToneSwap();

                // Fallback if skin tone swap didn't find assets
                if (builtAppearance.body == null)
                {
                    var bodyParts = bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Body, selectedBodyType);
                    if (bodyParts.Length > 0) builtAppearance.body = bodyParts[0];
                }
                if (builtAppearance.head == null)
                    builtAppearance.head = FindDefaultHumanHead(filteredHeadParts, allHeadParts, selectedBodyType);

                if (availableHairStyles != null && availableHairStyles.Length > 0)
                    builtAppearance.hair = availableHairStyles[selectedHairIndex];

                builtAppearance.hairTint = HairColorPresets[selectedHairColorIndex];
                builtAppearance.eyeTint = EyeColorPresets[0];

                AssignDefaultClothing();
            }

            // Show/hide option rows based on available data
            ShowRowIfHasData(hairPrevButton, availableHairStyles);
            ShowRowIfHasData(beardPrevButton, availableBeardStyles);
            ShowRowIfHasData(eyeColorPrevButton, bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Eyes, selectedBodyType));
            // Hair color row visible only if hair styles exist
            if (hairColorPrevButton != null)
                hairColorPrevButton.transform.parent.gameObject.SetActive(
                    availableHairStyles != null && availableHairStyles.Length > 0);
            // Body type toggle visible only if female body parts exist
            if (bodyTypeMaleButton != null)
            {
                var femaleParts = bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Body, "female");
                bodyTypeMaleButton.transform.parent.gameObject.SetActive(femaleParts.Length > 0);
            }

            UpdateBodyTypeUI();
            RefreshAppearancePreview();
            UpdateAppearanceUI();
        }

        private static void ShowRowIfHasData(Button rowButton, BodyPartData[] parts)
        {
            if (rowButton == null) return;
            rowButton.transform.parent.gameObject.SetActive(parts != null && parts.Length > 0);
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
            ApplySkinToneSwap();
            UIManager.Instance?.PlayNavigateSound();
            RefreshAppearancePreview();
            UpdateAppearanceUI();
        }

        /// <summary>
        /// Swaps body and head to pre-baked ULPC skin-tone variants.
        /// Falls back to tint-only if matching assets aren't found.
        /// </summary>
        private void ApplySkinToneSwap()
        {
            if (builtAppearance == null || bodyPartRegistry == null) return;

            string suffix = SkinToneSuffixes[selectedSkinToneIndex];
            string bodyId = $"body_{selectedBodyType}_{suffix}";
            string headId = $"head_human_{selectedBodyType}_{suffix}";

            var body = bodyPartRegistry.GetPartById(bodyId);
            if (body != null)
                builtAppearance.body = body;

            var head = bodyPartRegistry.GetPartById(headId);
            if (head != null)
                builtAppearance.head = head;
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

            // Auto-assign eye color overlay so eye tinting works.
            // Prefer "eye_color_" parts (aligned to standard heads) over "eyes_" (elderly).
            var eyeParts = bodyPartRegistry.GetPartsForSlot(BodyPartSlot.Eyes, selectedBodyType);
            BodyPartData defaultEyes = null;
            foreach (var part in eyeParts)
            {
                if (part.partId != null && part.partId.StartsWith("eye_color_"))
                {
                    defaultEyes = part;
                    break;
                }
            }
            if (defaultEyes == null)
            {
                foreach (var part in eyeParts)
                {
                    if (part.partId != null && part.partId.Contains("eyes_default"))
                    {
                        defaultEyes = part;
                        break;
                    }
                }
            }
            if (defaultEyes == null && eyeParts.Length > 0)
                defaultEyes = eyeParts[0];
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
            WireEquipmentVisualParts(ctrl);
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
            MakeSpacer(content.transform, 12f);

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
            var nextTmp = ctrl.nameConfirmButton.GetComponentInChildren<TextMeshProUGUI>();
            if (nextTmp != null) nextTmp.fontSize = 24f;
        }

        private static void BuildClassPanel(CharacterCreationController ctrl, Transform parent)
        {
            var panel = MakeDarkPanel(parent, "ClassSelectionPanel");
            panel.SetActive(false);
            ctrl.classSelectionPanel = panel;

            var content = MakeContentColumn(panel.transform);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.sizeDelta = new Vector2(900f, 0f);

            // Gothic title
            var title = MakeLabel(content.transform, "Choose Your Path", 34f);
            title.color = FrameGold;
            MakeSpacer(content.transform, 4f);

            // === Two-column layout: preview left, info right ===
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

            // --- Left column: character preview with cycling arrows ---
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

            // Preview frame with gothic border
            var previewFrame = MakeGothicFrame(leftCol.transform, 280f, 360f);
            ctrl.classLayeredPreview = previewFrame.AddComponent<UILayeredSpritePreview>();

            // Fallback static preview image (behind layered preview)
            var imgGo = MakeUIObject("Preview", previewFrame.transform);
            var img = imgGo.AddComponent<Image>();
            img.preserveAspect = true;
            img.color = Color.clear; // Start transparent; layered preview renders on top
            var imgRect = imgGo.GetComponent<RectTransform>();
            imgRect.anchorMin = Vector2.zero;
            imgRect.anchorMax = Vector2.one;
            imgRect.offsetMin = Vector2.zero;
            imgRect.offsetMax = Vector2.zero;
            ctrl.classPreviewImage = img;

            // Cycling row: < ClassName >
            var cycleRow = MakeHRow(leftCol.transform, 10f, 50f);
            ctrl.classPrevButton = MakeArrowButton(cycleRow.transform, "<");
            ctrl.classNameText = MakeLabel(cycleRow.transform, "Warrior", 28f);
            ctrl.classNameText.color = FrameGold;
            ctrl.classNameText.fontStyle = FontStyles.Bold;
            ctrl.classNameText.GetComponent<LayoutElement>().preferredWidth = 200f;
            ctrl.classNextButton = MakeArrowButton(cycleRow.transform, ">");

            // --- Right column: description, stats, equipment ---
            var rightCol = MakeUIObject("RightColumn", columnsRow.transform);
            var rightVlg = rightCol.AddComponent<VerticalLayoutGroup>();
            rightVlg.childAlignment = TextAnchor.UpperCenter;
            rightVlg.childControlWidth = true;
            rightVlg.childControlHeight = true;
            rightVlg.childForceExpandWidth = true;
            rightVlg.childForceExpandHeight = false;
            rightVlg.spacing = 5f;
            rightVlg.padding = new RectOffset(5, 5, 0, 5);
            var rightLayout = rightCol.AddComponent<LayoutElement>();
            rightLayout.flexibleWidth = 1f;

            // Description
            ctrl.classDescriptionText = MakeLabel(rightCol.transform, "", 19f);
            ctrl.classDescriptionText.color = TextSecondary;
            ctrl.classDescriptionText.alignment = TextAlignmentOptions.TopLeft;
            ctrl.classDescriptionText.textWrappingMode = TextWrappingModes.Normal;
            MakeAutoHeight(ctrl.classDescriptionText.gameObject);

            // Stats section
            MakeSectionHeader(rightCol.transform, "STATS");

            // Two-column stats row: base modifiers left, per-level right
            var statsRow = MakeUIObject("StatsRow", rightCol.transform);
            var statsHlg = statsRow.AddComponent<HorizontalLayoutGroup>();
            statsHlg.childAlignment = TextAnchor.UpperLeft;
            statsHlg.childControlWidth = true;
            statsHlg.childControlHeight = true;
            statsHlg.childForceExpandWidth = true;
            statsHlg.childForceExpandHeight = false;
            statsHlg.spacing = 12f;

            ctrl.classStatsPreviewText = MakeLabel(statsRow.transform, "", 17f);
            ctrl.classStatsPreviewText.color = TextCol;
            ctrl.classStatsPreviewText.alignment = TextAlignmentOptions.TopLeft;
            ctrl.classStatsPreviewText.richText = true;
            ctrl.classStatsPreviewText.textWrappingMode = TextWrappingModes.Normal;
            ctrl.classStatsPreviewText.GetComponent<LayoutElement>().preferredWidth = -1f;
            MakeAutoHeight(ctrl.classStatsPreviewText.gameObject);

            ctrl.classPerLevelText = MakeLabel(statsRow.transform, "", 17f);
            ctrl.classPerLevelText.color = TextCol;
            ctrl.classPerLevelText.alignment = TextAlignmentOptions.TopLeft;
            ctrl.classPerLevelText.richText = true;
            ctrl.classPerLevelText.textWrappingMode = TextWrappingModes.Normal;
            ctrl.classPerLevelText.GetComponent<LayoutElement>().preferredWidth = -1f;
            MakeAutoHeight(ctrl.classPerLevelText.gameObject);

            // Equipment section
            MakeSectionHeader(rightCol.transform, "EQUIPMENT");
            ctrl.classEquipmentText = MakeLabel(rightCol.transform, "", 17f);
            ctrl.classEquipmentText.color = TextCol;
            ctrl.classEquipmentText.alignment = TextAlignmentOptions.TopLeft;
            ctrl.classEquipmentText.textWrappingMode = TextWrappingModes.Normal;
            MakeAutoHeight(ctrl.classEquipmentText.gameObject);

            MakeSpacer(content.transform, 10f);

            // Nav buttons
            var navRow = MakeHRow(content.transform, 20f, 55f);
            ctrl.classBackButton = MakeButton(navRow.transform, "Back", 200f, 50f);
            var backTmp = ctrl.classBackButton.GetComponentInChildren<TextMeshProUGUI>();
            if (backTmp != null) backTmp.fontSize = 26f;

            ctrl.classConfirmButton = MakeButton(navRow.transform, "Begin", 240f, 50f);
            var confirmTmp = ctrl.classConfirmButton.GetComponentInChildren<TextMeshProUGUI>();
            if (confirmTmp != null) confirmTmp.fontSize = 26f;
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
            // Hide body type until female variants exist
            bodyTypeRow.SetActive(false);

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

            // === APPEARANCE section header ===
            MakeSectionHeader(scrollContent.transform, "APPEARANCE");

            // Skin tone (always available — uses pre-baked ULPC variants)
            MakeColorRow(scrollContent.transform, "Skin Tone", SkinTonePresets[0], SkinToneNames[0],
                out ctrl.skinPrevButton, out ctrl.skinNextButton, out ctrl.skinColorPreview, out ctrl.skinToneNameText);

            // Hair style (hidden until hair assets exist)
            MakeOptionRow(scrollContent.transform, "Hair Style", out ctrl.hairPrevButton, out ctrl.hairNextButton, out ctrl.hairNameText);
            ctrl.hairPrevButton.transform.parent.gameObject.SetActive(false);

            // Beard (hidden until beard assets exist)
            MakeOptionRow(scrollContent.transform, "Beard", out ctrl.beardPrevButton, out ctrl.beardNextButton, out ctrl.beardNameText);
            ctrl.beardPrevButton.transform.parent.gameObject.SetActive(false);

            // Eye color (hidden until eye assets exist)
            MakeColorRow(scrollContent.transform, "Eye Color", EyeColorPresets[0], EyeColorNames[0],
                out ctrl.eyeColorPrevButton, out ctrl.eyeColorNextButton, out ctrl.eyeColorPreview, out ctrl.eyeColorNameText);
            ctrl.eyeColorPrevButton.transform.parent.gameObject.SetActive(false);

            // Hair color (hidden until hair assets exist)
            MakeColorRow(scrollContent.transform, "Hair Color", HairColorPresets[0], HairColorNames[0],
                out ctrl.hairColorPrevButton, out ctrl.hairColorNextButton, out ctrl.hairColorPreview, out ctrl.hairColorNameText);
            ctrl.hairColorPrevButton.transform.parent.gameObject.SetActive(false);

            MakeSpacer(content.transform, 10f);

            // Nav buttons
            var navRow = MakeHRow(content.transform, 20f, 55f);
            ctrl.appearanceBackButton = MakeButton(navRow.transform, "Back", 200f, 50f);
            var backTmp = ctrl.appearanceBackButton.GetComponentInChildren<TextMeshProUGUI>();
            if (backTmp != null) backTmp.fontSize = 26f;

            ctrl.appearanceConfirmButton = MakeButton(navRow.transform, "Next", 240f, 50f);
            var appConfirmTmp = ctrl.appearanceConfirmButton.GetComponentInChildren<TextMeshProUGUI>();
            if (appConfirmTmp != null) appConfirmTmp.fontSize = 26f;
        }

        private static void FindBodyPartRegistry(CharacterCreationController ctrl)
        {
            if (ctrl.bodyPartRegistry != null) return;

            ctrl.bodyPartRegistry = Resources.Load<BodyPartRegistry>("BodyPartRegistry");
        }

        /// <summary>
        /// Deferred sprite loading — no longer needed with cycling design,
        /// but kept as stub for CreateRuntimeUI compatibility.
        /// </summary>
        private static void LoadClassPreviewSprites(CharacterCreationController ctrl)
        {
            // Class preview is now loaded on-demand via ApplyClassSelection
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
            WireJobEquipment(ctrl.warriorData, "warrior_sword", "warrior_chainmail", "warrior_greaves", "warrior_sabatons");
            WireJobEquipment(ctrl.mageData, "mage_staff", "mage_robe", "mage_pants", "mage_sandals");
            WireJobEquipment(ctrl.rogueData, "rogue_dagger", "rogue_vest", "rogue_pants", "rogue_boots");
        }

        private static void WireJobEquipment(JobClassData job, params string[] equipmentIds)
        {
            if (job == null) return;

            var equipment = new System.Collections.Generic.List<EquipmentData>();
            foreach (var id in equipmentIds)
            {
                var equip = Resources.Load<EquipmentData>($"Equipment/{id}");
                if (equip != null) equipment.Add(equip);
            }

            if (equipment.Count > 0)
                job.starterEquipment = equipment.ToArray();
        }

        /// <summary>
        /// Ensures every starter equipment item has a valid visualPart reference.
        /// Serialized GUID references in .asset files can go stale when BodyPartData
        /// assets are reimported. This force-resolves them at runtime via the
        /// BodyPartRegistry using known universal (body-type-agnostic) partIds.
        /// </summary>
        private static void WireEquipmentVisualParts(CharacterCreationController ctrl)
        {
            if (ctrl.bodyPartRegistry == null) return;

            // Map equipmentId → BodyPartData partId for runtime visual resolution
            var visualMap = new Dictionary<string, string>
            {
                { "warrior_sword",      "weapon_longsword_male" },
                { "warrior_chainmail",  "torso_chainmail" },
                { "warrior_greaves",    "legs_armour" },
                { "warrior_sabatons",   "shoes_armour_male" },
                { "mage_staff",         "weapon_simple_staff_male" },
                { "mage_robe",          "jacket_trim_frock_coat_lace_male" },
                { "mage_pants",         "legs_pants" },
                { "mage_sandals",       "shoes_basic_shoes_male" },
                { "rogue_dagger",       "weapon_dagger_male" },
                { "rogue_vest",         "torso_leather" },
                { "rogue_pants",        "legs_pants" },
                { "rogue_boots",        "shoes_basic_boots_male" },
            };

            var jobs = new[] { ctrl.warriorData, ctrl.mageData, ctrl.rogueData };
            foreach (var job in jobs)
            {
                if (job == null || job.starterEquipment == null) continue;
                foreach (var equip in job.starterEquipment)
                {
                    if (equip == null) continue;

                    if (visualMap.TryGetValue(equip.equipmentId, out var partId))
                    {
                        var resolved = ctrl.bodyPartRegistry.GetPartById(partId);
                        if (resolved != null)
                            equip.visualPart = resolved;
                    }
                }
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
            rt.anchoredPosition = Vector2.zero;
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
            colors.highlightedColor = BtnSelected;
            colors.pressedColor = BtnPress;
            colors.selectedColor = BtnSelected;
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
            leftDiv.AddComponent<Image>().color = FrameGold;
            var ldLayout = leftDiv.AddComponent<LayoutElement>();
            ldLayout.flexibleWidth = 1f;
            ldLayout.preferredHeight = 1f;
            ldLayout.minWidth = 20f;

            // Title text — fits content
            var label = MakeLabel(go.transform, title, 16f);
            label.color = FrameGold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.fontStyle = FontStyles.SmallCaps;
            var labelLayout = label.GetComponent<LayoutElement>();
            labelLayout.preferredWidth = -1f;
            labelLayout.flexibleWidth = 0f;

            // Right divider — flexible width to fill available space
            var rightDiv = MakeUIObject("DivR", go.transform);
            rightDiv.AddComponent<Image>().color = FrameGold;
            var rdLayout = rightDiv.AddComponent<LayoutElement>();
            rdLayout.flexibleWidth = 1f;
            rdLayout.preferredHeight = 1f;
            rdLayout.minWidth = 20f;
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

            prevBtn = MakeArrowButton(row.transform, "<");
            nameText = MakeLabel(row.transform, "None", 19f);
            nameText.GetComponent<LayoutElement>().preferredWidth = 180f;
            nextBtn = MakeArrowButton(row.transform, ">");
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

            prevBtn = MakeArrowButton(row.transform, "<");

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

            nextBtn = MakeArrowButton(row.transform, ">");
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
            if (classPrevButton != null) classPrevButton.onClick.RemoveAllListeners();
            if (classNextButton != null) classNextButton.onClick.RemoveAllListeners();
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
