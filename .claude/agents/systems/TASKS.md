# Systems Agent — Sprint Tasks

> Owner: Systems Agent
> Scope: Core systems, managers, save/load, data flow

---

## SYS-1: Wire LevelUp → SkillManager (SP Fix)

**Priority:** P0 — Critical bug fix
**Status:** DONE
**Dependencies:** None
**Files to modify:** `Assets/_Project/Scripts/Systems/Core/LevelSystem.cs`

### Problem

`LevelSystem.OnLevelUp` event exists and fires correctly (line 109), but nothing subscribes to it to call `SkillManager.Instance.SetPlayerLevel()`. Players never receive SP on level-up.

### Implementation

In `LevelSystem.cs`:

1. Add a `Start()` subscription (after existing `Start()` content at line 73):

```csharp
private void Start()
{
    ApplyStatScaling(refill: false);
    OnXPChanged?.Invoke(currentXP, XPForNextLevel);

    // Wire level-up to skill manager SP awards
    OnLevelUp += HandleLevelUpForSkills;

    if (debugLogging)
    {
        LogMilestones();
    }
}
```

2. Add `OnDestroy()` to unsubscribe:

```csharp
private void OnDestroy()
{
    OnLevelUp -= HandleLevelUpForSkills;
}
```

3. Add handler method:

```csharp
private void HandleLevelUpForSkills(int newLevel)
{
    if (SkillManager.Instance != null)
    {
        SkillManager.Instance.SetPlayerLevel(newLevel);
    }
}
```

### Acceptance Criteria

- Kill enemies, gain XP, level up — SkillManager.AvailableSP increases
- SP gain amount matches `currentJob.spPerLevel` (default 3)
- No errors on level-up if SkillManager doesn't exist yet

---

## SYS-2: Create ExperienceOrb System

**Priority:** P0
**Status:** DONE
**Dependencies:** SYS-1
**Files to create:** `Assets/_Project/Scripts/Systems/Core/ExperienceOrb.cs`
**Files to modify:** `Assets/_Project/Scripts/Enemies/Core/EnemyController.cs`

### New File: ExperienceOrb.cs

Create at `Assets/_Project/Scripts/Systems/Core/ExperienceOrb.cs`.

**Behavior:**
- 3 states: `Scattering` → `Idle` → `Attracting`
- **Scattering (0.5s):** Rigidbody2D impulse (random direction + upward bias), normal gravity
- **Idle:** Low gravity (0.3), waits for player within attraction radius (~3 units)
- **Attracting:** Zero gravity, accelerate toward player (20 units/s²), collect on contact (overlap circle ~0.3 radius)
- **On collect:** Call `LevelSystem.AddXP(xpValue)`, play collect sound via SFXManager, destroy self

```csharp
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class ExperienceOrb : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float scatterDuration = 0.5f;
    [SerializeField] private float attractionRadius = 3f;
    [SerializeField] private float attractionAcceleration = 20f;
    [SerializeField] private float collectRadius = 0.3f;
    [SerializeField] private float idleGravity = 0.3f;
    [SerializeField] private float scatterForce = 5f;
    [SerializeField] private float lifetime = 30f;

    [Header("Audio")]
    [SerializeField] private AudioClip collectSound;

    private enum OrbState { Scattering, Idle, Attracting }

    private OrbState currentState = OrbState.Scattering;
    private Rigidbody2D rb;
    private int xpValue;
    private float stateTimer;
    private Transform playerTransform;
    private LevelSystem playerLevelSystem;

    public void Initialize(int xp)
    {
        xpValue = xp;
    }

    // ... state machine implementation
}
```

**Key implementation details:**
- Use `Rigidbody2D` for scatter physics, then switch to manual movement for attraction
- Find player via `GameObject.FindGameObjectWithTag("Player")` in Start (cache reference)
- CircleCollider2D as trigger for collection detection
- Self-destruct after `lifetime` seconds if not collected

### Modify: EnemyController.cs

In `EnemyController.cs`, modify the `AwardXP()` method (line 536):

```csharp
[Header("Experience Orbs")]
[SerializeField] private GameObject experienceOrbPrefab;
[SerializeField] private int orbCount = 3;

private void AwardXP()
{
    if (enemyData.experienceValue <= 0)
        return;

    // Spawn XP orbs if prefab assigned
    if (experienceOrbPrefab != null)
    {
        int xpPerOrb = Mathf.Max(1, enemyData.experienceValue / orbCount);
        int remainder = enemyData.experienceValue - (xpPerOrb * orbCount);

        for (int i = 0; i < orbCount; i++)
        {
            GameObject orbObj = Instantiate(experienceOrbPrefab, transform.position, Quaternion.identity);
            ExperienceOrb orb = orbObj.GetComponent<ExperienceOrb>();
            if (orb != null)
            {
                int thisOrbXP = xpPerOrb + (i == 0 ? remainder : 0);
                orb.Initialize(thisOrbXP);
            }
        }
        return;
    }

    // Fallback: direct XP award
    GameObject player = GameObject.FindGameObjectWithTag("Player");
    if (player != null)
    {
        LevelSystem levelSystem = player.GetComponent<LevelSystem>();
        if (levelSystem != null)
        {
            levelSystem.AddXP(enemyData.experienceValue);
        }
    }
}
```

Add the new fields after `[SerializeField] private bool debugLogging = false;` (line 19):

### Acceptance Criteria

- Enemies spawn 3 orbs on death
- Orbs scatter outward, then hover, then fly toward player
- Player gains correct total XP (sum of all orbs = enemyData.experienceValue)
- Works with existing direct-award fallback when prefab not assigned
- Orbs self-destruct after 30s

---

## SYS-3: Create StatSystem (STR/INT/AGI)

**Priority:** P1
**Status:** DONE
**Dependencies:** SYS-1
**Files to create:** `Assets/_Project/Scripts/Systems/Core/StatSystem.cs`
**Files to modify:** `Assets/_Project/Scripts/Skills/Data/JobClassData.cs`

### New File: StatSystem.cs

Create at `Assets/_Project/Scripts/Systems/Core/StatSystem.cs`.

Component placed on Player alongside HealthSystem, ManaSystem, LevelSystem.

```csharp
using System;
using UnityEngine;

public class StatSystem : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private int statPointsPerLevel = 3;

    // Base stats (from class auto-growth)
    private int baseStrength = 1;
    private int baseIntelligence = 1;
    private int baseAgility = 1;

    // Manually allocated stats
    private int allocatedStrength;
    private int allocatedIntelligence;
    private int allocatedAgility;

    // Available points
    private int availableStatPoints;

    // Properties
    public int Strength => baseStrength + allocatedStrength;
    public int Intelligence => baseIntelligence + allocatedIntelligence;
    public int Agility => baseAgility + allocatedAgility;
    public int AvailableStatPoints => availableStatPoints;

    // Derived stats
    public float BonusMaxHP => Strength * 5f;
    public float MeleeDamageMultiplier => 1f + (Strength * 0.02f);
    public float BonusMaxMana => Intelligence * 3f;
    public float SkillDamageMultiplier => 1f + (Intelligence * 0.02f);
    public float SpeedMultiplier => 1f + (Agility * 0.01f);
    public float CritChance => Mathf.Min(0.5f, Agility * 0.005f);

    // Events
    public event Action OnStatsChanged;
    public event Action<int> OnStatPointsChanged;

    // Methods: AllocateStat(string statName), OnLevelUp subscription,
    //          ApplyAutoGrowth(JobClassData), CreateSaveData/ApplySaveData
}
```

**Key behaviors:**
- Subscribe to `LevelSystem.OnLevelUp` in Start
- On level-up: award `statPointsPerLevel` + apply class auto-growth
- `AllocateStat(string name)`: spend 1 available point, fire events
- `ResetAllocations()`: refund all manually allocated points
- Save/Load via `StatSaveData` serializable class

### Modify: JobClassData.cs

Add after line 78 (`public float defenseModifier = 1f;`):

```csharp
[Header("Auto-Growth Per Level")]
[Tooltip("STR gained per level from class growth")]
public int strPerLevel = 1;
[Tooltip("INT gained per level from class growth")]
public int intPerLevel = 1;
[Tooltip("AGI gained per level from class growth")]
public int agiPerLevel = 1;
```

Default values by class (set in ScriptableObject assets):
- Warrior: 3/1/1
- Mage: 1/3/1
- Rogue: 1/1/3
- Beginner: 1/1/1

### Acceptance Criteria

- StatSystem component on Player alongside existing systems
- Level-up awards stat points AND applies class auto-growth
- `AllocateStat()` works, `AvailableStatPoints` decreases
- Derived stats compute correctly
- Events fire on changes
- Save/Load round-trips correctly

---

## SYS-4: Create SFXManager Static Helper

**Priority:** P1
**Status:** DONE
**Dependencies:** None
**Files to create:** `Assets/_Project/Scripts/Audio/SFXManager.cs`

### New File: SFXManager.cs

```csharp
using UnityEngine;

/// <summary>
/// Static helper for playing sound effects at the correct volume.
/// Reads Audio_Master and Audio_SFX from PlayerPrefs.
/// </summary>
public static class SFXManager
{
    /// <summary>
    /// Returns the combined master * SFX volume.
    /// </summary>
    public static float GetVolume()
    {
        float master = PlayerPrefs.GetFloat("Audio_Master", 1f);
        float sfx = PlayerPrefs.GetFloat("Audio_SFX", 1f);
        return master * sfx;
    }

    /// <summary>
    /// Plays a one-shot clip through the given AudioSource at the correct SFX volume.
    /// </summary>
    public static void PlayOneShot(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null)
            return;

        source.PlayOneShot(clip, GetVolume());
    }

    /// <summary>
    /// Plays a clip at a world position using PlayClipAtPoint, scaled to SFX volume.
    /// </summary>
    public static void PlayAtPoint(AudioClip clip, Vector3 position)
    {
        if (clip == null)
            return;

        // PlayClipAtPoint creates a temporary AudioSource, volume is baked into clip
        AudioSource.PlayClipAtPoint(clip, position, GetVolume());
    }
}
```

### Acceptance Criteria

- `SFXManager.GetVolume()` returns master * sfx from PlayerPrefs
- `SFXManager.PlayOneShot()` plays at correct volume
- No MonoBehaviour required (static class)
- Works from anywhere without scene references

---

## SYS-5: Update SaveManager for New Fields

**Priority:** P1
**Status:** DONE
**Dependencies:** SYS-3
**Files to modify:**
- `Assets/_Project/Scripts/Systems/Core/SaveManager.cs`
- `Assets/_Project/Scripts/Systems/Core/SaveSlotInfo.cs`

### SaveManager.cs Changes

1. **SaveData class** — add new fields (after `public SkillSaveData skillData;` line 58):

```csharp
// Character creation
public string startingClass = "";
public int appearanceIndex;

// Stat system
public StatSaveData statData;
```

2. **Bump version** — change `CURRENT_SAVE_VERSION` from `2` to `3` (line 14)

3. **MigrateSaveData** — add v2→v3 migration (after line 478):

```csharp
if (data.saveVersion < 3)
{
    data.startingClass = "";
    data.appearanceIndex = 0;
    data.statData = null;  // StatSystem will initialize defaults
}
```

4. **CreateSaveData** — add stat system save (after skill system save block, ~line 165):

```csharp
// Stat system save
var statSystem = player?.GetComponent<StatSystem>();
if (statSystem != null)
{
    data.statData = statSystem.CreateSaveData();
}
```

5. **ApplyLoadedData** — add stat system restore (after skill hotbar block, ~line 442):

```csharp
// Apply stat system data
var statSystem = player.GetComponent<StatSystem>();
if (statSystem != null && currentSaveData.statData != null)
{
    statSystem.ApplySaveData(currentSaveData.statData);
}
```

### SaveSlotInfo.cs Changes

Add field for display (after `public int maxWaveReached;` line 18):

```csharp
public string startingClass;
```

Update `GetSlotInfo()` in SaveManager.cs to populate it:

```csharp
info.startingClass = data.startingClass ?? "";
```

### Acceptance Criteria

- Save version bumped to 3
- v2 saves migrate cleanly to v3
- New fields serialize/deserialize correctly
- Existing save data not lost during migration
- StatSystem state round-trips through save/load

---

## SYS-6: Verify Waves 1-10 Configuration

**Priority:** P2
**Status:** DONE (Code reviewed — see notes below)
**Dependencies:** None
**Files to review:**
- `Assets/_Project/Scripts/Enemies/Spawning/WaveConfig.cs`
- `Assets/_Project/Scripts/Enemies/Spawning/WaveManager.cs`
- `Assets/_Project/Scripts/Enemies/Spawning/WaveScaler.cs`
- `Assets/_Project/Scripts/Enemies/Spawning/EnemyStatModifier.cs`

### Task

Review the wave system configuration and code to verify:

1. **WaveConfig ScriptableObject** assets exist and are properly configured
2. **Enemy pool** has entries with appropriate `minWaveToAppear` values
3. **Scaling formula** produces reasonable values for waves 1-10:
   - HP scales: `baseHP * (1 + wave * healthScalePerWave)`
   - Damage scales: `baseDmg * (1 + wave * damageScalePerWave)`
   - Speed scales: `baseSpeed * (1 + wave * speedScalePerWave)`
4. **Enemy count** progression: base 3, +2 per wave, max 15 alive
5. **Rest duration** between waves feels right (default 3s)

### What to Fix (if needed)

- If WaveConfig asset doesn't exist, document what needs to be created in Unity editor
- If WaveScaler math is wrong, fix the formulas
- If enemy pool is empty, note which prefabs need to be added

### Acceptance Criteria

- Waves 1-10 spawn progressively harder enemies
- No null reference errors during wave transitions
- Enemy count doesn't exceed maxEnemiesAlive

---

## SYS-7: Verify/Create Class Skill Assets

**Priority:** P2
**Status:** Pending
**Dependencies:** SYS-3
**Files to review:**
- `Assets/_Project/Scripts/Skills/Data/JobClassData.cs`
- `Assets/_Project/Scripts/Skills/Data/SkillData.cs`
- `Assets/_Project/Scripts/Skills/Data/SkillTreeData.cs`

### Task

Verify that SkillData ScriptableObject assets exist for each starting class. If they don't, document what needs to be created:

**Per class, need at minimum:**
- Warrior: 1 basic attack skill (e.g., "Power Strike")
- Mage: 1 basic spell skill (e.g., "Fireball")
- Rogue: 1 basic utility skill (e.g., "Quick Strike")
- Beginner: 1 universal skill (e.g., "Focus")

Each skill needs:
- Unique `skillId`
- `spCost` = 1
- `requiredPlayerLevel` = 1
- `maxLevel` = 5
- Appropriate `SkillType` and `DamageType`

### Acceptance Criteria

- Each starting class has at least 1 learnable skill
- Skills are referenced in JobClassData.availableSkills
- Skills can be learned with SP from level-ups
