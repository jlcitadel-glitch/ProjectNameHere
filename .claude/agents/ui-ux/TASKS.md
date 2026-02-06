# UI-UX Agent — Sprint Tasks

> Owner: UI-UX Agent
> Scope: Menus, HUD, fonts, options, character creation flow

---

## UI-1: Fix SFX Volume Slider in OptionsMenuController

**Priority:** P0 — SFX slider does nothing
**Status:** DONE
**Dependencies:** SYS-4 (SFXManager)
**Files to modify:** `Assets/_Project/Scripts/UI/Menus/OptionsMenuController.cs`

### Problem

`OnSFXVolumeChanged()` at line 1104 saves the value to PlayerPrefs but has a TODO comment and never actually applies the volume. The comment reads: `// TODO: Apply to SFX audio source/mixer`.

### Fix

Replace the `OnSFXVolumeChanged` method (line 1104-1108):

```csharp
// Before:
private void OnSFXVolumeChanged(float value)
{
    PlayerPrefs.SetFloat("Audio_SFX", value);
    // TODO: Apply to SFX audio source/mixer
    UpdateVolumeTexts();
}

// After:
private void OnSFXVolumeChanged(float value)
{
    PlayerPrefs.SetFloat("Audio_SFX", value);

    // Apply to UIManager AudioSource for immediate feedback
    if (UIManager.Instance != null)
    {
        var uiAudioSource = UIManager.Instance.GetComponent<AudioSource>();
        if (uiAudioSource != null)
        {
            uiAudioSource.volume = SFXManager.GetVolume();
        }
    }

    UpdateVolumeTexts();
}
```

**Note:** SFXManager reads from PlayerPrefs each time `GetVolume()` is called, so all future `SFXManager.PlayOneShot()` calls will automatically use the new volume. The fix above also provides immediate feedback for UI sounds.

### Acceptance Criteria

- Moving SFX slider changes future sound effect volumes
- Master volume + SFX volume combine correctly
- UI click sounds respond to volume change immediately
- Value persists across sessions via PlayerPrefs

---

## UI-2: Character Name Input Screen

**Priority:** P1
**Status:** DONE
**Dependencies:** None
**Files to create:** `Assets/_Project/Scripts/UI/Menus/CharacterCreationController.cs`
**Files to modify:** `Assets/_Project/Scripts/UI/Menus/MainMenuController.cs`

### Design

Add new states to `MainMenuController.MainMenuState` enum:

```csharp
public enum MainMenuState
{
    Title,
    SaveSelection,
    Options,
    ConfirmOverwrite,
    ConfirmDelete,
    NameEntry,       // NEW
    ClassSelection,  // NEW
    AppearanceSelection  // NEW
}
```

Create `CharacterCreationController.cs` at `Assets/_Project/Scripts/UI/Menus/CharacterCreationController.cs`:

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Handles the character creation flow: Name → Class → Appearance.
    /// Works with MainMenuController to manage state transitions.
    /// </summary>
    public class CharacterCreationController : MonoBehaviour
    {
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
        [SerializeField] private TMP_Text classDescriptionText;
        [SerializeField] private TMP_Text classStatsPreviewText;

        [Header("Appearance Selection")]
        [SerializeField] private GameObject appearancePanel;
        [SerializeField] private Button[] appearanceButtons;
        [SerializeField] private Button appearanceBackButton;
        [SerializeField] private Button appearanceConfirmButton;
        [SerializeField] private Image appearancePreview;

        [Header("Job Data References")]
        [SerializeField] private JobClassData warriorData;
        [SerializeField] private JobClassData mageData;
        [SerializeField] private JobClassData rogueData;

        // Creation data
        private string characterName;
        private JobClassData selectedClass;
        private int selectedAppearanceIndex;
        private int targetSlotIndex;

        public event Action OnCreationComplete;
        public event Action OnCreationCancelled;

        // State management methods:
        // ShowNameEntry(int slotIndex)
        // ShowClassSelection()
        // ShowAppearanceSelection()
        // FinalizeCreation() -> fires OnCreationComplete with data
    }
}
```

### MainMenuController.cs Integration

When empty slot is clicked, instead of `StartNewGame(slot.SlotIndex)`, show character creation:

```csharp
// In OnSaveSlotClicked:
if (slot.IsEmpty)
{
    // Show character creation instead of starting immediately
    characterCreation.ShowNameEntry(slot.SlotIndex);
}
```

After creation complete, start the game with creation data.

### Flow

1. **Name Entry:** TMP_InputField, max 16 chars, validate non-empty, Confirm + Back buttons
2. **Class Selection:** 3 buttons with stat preview from JobClassData (show strPerLevel/intPerLevel/agiPerLevel)
3. **Appearance:** 3-5 sprite variants, left/right arrows or grid, preview image
4. **Finalize:** Store in SaveManager, start game

### Acceptance Criteria

- Name field limits to 16 characters
- Empty/whitespace names rejected with error text
- Class selection shows stat growth preview
- Back buttons work at each step
- Creation data flows into StartNewGame
- Cinzel font used throughout

---

## UI-3: Class Selection Screen

**Priority:** P1
**Status:** DONE
**Dependencies:** UI-2, SYS-3 (StatSystem for previews)

### Note

This is part of the CharacterCreationController created in UI-2. The class selection step should:

1. Show 3 class buttons: Warrior, Mage, Rogue
2. On hover/select, show description from `JobClassData.description`
3. Show stat growth preview: `STR +{strPerLevel}/level, INT +{intPerLevel}/level, AGI +{agiPerLevel}/level`
4. Show stat modifiers: `ATK x{attackModifier}, MAG x{magicModifier}, DEF x{defenseModifier}`
5. Selecting a class highlights it and enables Next/Confirm button

### Implementation Detail

```csharp
private void UpdateClassPreview(JobClassData classData)
{
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
```

### Acceptance Criteria

- 3 class buttons visible with icons (if JobClassData.jobIcon assigned)
- Description updates on selection
- Stat preview shows growth rates
- Back returns to name entry
- Selected class stored for game start

---

## UI-4: Appearance Selection Screen

**Priority:** P1
**Status:** DONE
**Dependencies:** UI-3

### Note

Part of CharacterCreationController. The appearance step should:

1. Show 3-5 sprite variants (can be color/palette swaps of Hero Knight sprite)
2. Arrow buttons or clickable grid to browse
3. Preview image updates in real-time
4. Confirm + Back buttons
5. Store `appearanceIndex` for use in save data

### Implementation

```csharp
[Header("Appearance Options")]
[SerializeField] private Sprite[] appearanceSprites;

private void UpdateAppearancePreview()
{
    if (appearancePreview != null && appearanceSprites != null
        && selectedAppearanceIndex < appearanceSprites.Length)
    {
        appearancePreview.sprite = appearanceSprites[selectedAppearanceIndex];
    }
}
```

### Acceptance Criteria

- At least 3 appearance options visible
- Preview updates on selection
- Back returns to class selection
- Confirm starts the game with all creation data
- Appearance index saved to SaveData.appearanceIndex

---

## UI-5: Fix Font Inconsistencies (Audit + Enforce Cinzel)

**Priority:** P1
**Status:** DONE
**Dependencies:** None
**Files to audit:**
- `Assets/_Project/Scripts/UI/Menus/OptionsMenuController.cs`
- `Assets/_Project/Scripts/UI/Menus/MainMenuController.cs`
- `Assets/_Project/Scripts/UI/Menus/PauseMenuController.cs`
- `Assets/_Project/Scripts/UI/Menus/SaveSlotUI.cs`
- `Assets/_Project/Scripts/UI/Skills/*.cs`
- `Assets/_Project/Scripts/UI/HUD/*.cs`
- `Assets/_Project/Scripts/UI/Core/FontManager.cs`

### Task

1. **Audit** all scripts that create TMPro text at runtime (look for `AddComponent<TextMeshProUGUI>()` and `new TextMeshProUGUI()`)
2. **Verify** that `FontManager.EnsureFont()` is called on ALL dynamically created text
3. **Check** that `FontManager.cs` has the correct Cinzel font reference
4. **Fix** any scripts that create text without calling `FontManager.EnsureFont()`

### Known Areas

- `OptionsMenuController.cs` already calls `ApplyDefaultFont()` which uses `FontManager.EnsureFont()` — verify this is complete
- `SaveSlotUI.cs` — check if it creates runtime text
- Skill tree UI — check `SkillNodeUI.cs`, `SkillTooltip.cs`, `SkillTreePanel.cs`
- HUD elements — check `DamageNumberSpawner.cs`, `NotificationSystem.cs`

### Acceptance Criteria

- All runtime-created text uses Cinzel font
- No default Arial/system font visible anywhere in UI
- FontManager.EnsureFont() called consistently
- Verify by visual inspection in Unity

---

## UI-6: Credits Screen

**Priority:** P2
**Status:** DONE
**Dependencies:** None
**Files to create:** `Assets/_Project/Scripts/UI/Menus/CreditsController.cs`
**Files to modify:** `Assets/_Project/Scripts/UI/Menus/MainMenuController.cs`

### New File: CreditsController.cs

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Scrolling credits screen with configurable text and speed.
    /// </summary>
    public class CreditsController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform creditsContent;
        [SerializeField] private TMP_Text creditsText;
        [SerializeField] private Button backButton;

        [Header("Settings")]
        [SerializeField] private float scrollSpeed = 50f;
        [SerializeField] private bool autoScroll = true;

        [Header("Credits Content")]
        [TextArea(10, 30)]
        [SerializeField] private string creditsString =
            "ProjectNameHere\n\n" +
            "--- Design & Programming ---\n\n" +
            "[Your Name]\n\n" +
            "--- Art ---\n\n" +
            "Hero Knight - Pixel Art\n\n" +
            "--- Music ---\n\n" +
            "[Music Credits]\n\n" +
            "--- Tools ---\n\n" +
            "Unity 6\nDOTween\nTextMesh Pro\n\n" +
            "--- Special Thanks ---\n\n" +
            "Thank you for playing!";

        private float scrollPosition;
        private bool isScrolling;

        private void OnEnable()
        {
            scrollPosition = 0f;
            isScrolling = autoScroll;
            if (creditsContent != null)
                creditsContent.anchoredPosition = Vector2.zero;
        }

        private void Update()
        {
            if (!isScrolling || creditsContent == null) return;

            scrollPosition += scrollSpeed * Time.unscaledDeltaTime;
            creditsContent.anchoredPosition = new Vector2(0, scrollPosition);
        }

        // Back button returns to main menu
    }
}
```

### MainMenuController.cs Integration

1. Add new state to enum: (already part of the flow, or add `Credits` state)
2. Add `[SerializeField] private Button creditsButton;` field
3. Add `[SerializeField] private GameObject creditsPanel;` field
4. Wire button in `SetupButtons()`
5. Add `ShowCredits()` and `OnCreditsBackClicked()` methods

In `MainMenuPanel`, add a "Credits" button below "Quit":

```csharp
// In AutoFindReferences:
if (creditsButton == null)
{
    var found = mainMenuPanel.transform.Find("CreditsButton");
    if (found != null) creditsButton = found.GetComponent<Button>();
}
```

### Acceptance Criteria

- Credits button visible on main menu
- Credits text scrolls upward automatically
- Back button returns to main menu
- Scroll speed configurable in Inspector
- Uses Cinzel font

---

## UI-7: Boss Health Bar UI

**Priority:** P2
**Status:** DONE
**Dependencies:** PLR-4 (BossController)
**Files to create:** `Assets/_Project/Scripts/UI/HUD/BossHealthBar.cs`

### New File: BossHealthBar.cs

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Large health bar displayed at top of screen during boss fights.
    /// Subscribes to BossController events for phase changes.
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject bossBarRoot;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image healthFillBackground;
        [SerializeField] private TMP_Text bossNameText;
        [SerializeField] private TMP_Text healthText;

        [Header("Phase Indicators")]
        [SerializeField] private Image[] phaseIndicators;
        [SerializeField] private Color activePhaseColor = new Color(0.545f, 0f, 0f, 1f);
        [SerializeField] private Color inactivePhaseColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        [Header("Animation")]
        [SerializeField] private float fillLerpSpeed = 3f;
        [SerializeField] private float damageFlashDuration = 0.1f;

        private BossController activeBoss;
        private HealthSystem bossHealth;
        private float targetFillAmount = 1f;
        private float currentFillAmount = 1f;

        // Show/Hide based on GameManager.OnGameStateChanged
        // Subscribe to boss HealthSystem.OnHealthChanged
        // Smooth lerp fill amount
        // Show phase indicators based on BossController.OnPhaseChanged
    }
}
```

**Key behaviors:**
- Hidden by default (`bossBarRoot.SetActive(false)`)
- Show when `GameManager` enters `BossFight` state
- Subscribe to boss `HealthSystem.OnHealthChanged` for fill updates
- Smooth HP bar lerp (not instant)
- Show boss name from `BossController.BossName`
- Phase indicators light up as phases are reached
- Hide on boss death or `ExitBossFight`

### Acceptance Criteria

- Health bar appears at top of screen during boss fight
- Shows boss name
- HP smoothly decreases as boss takes damage
- Phase indicators update on phase transitions
- Bar hides when boss dies
- Uses Cinzel font for text
