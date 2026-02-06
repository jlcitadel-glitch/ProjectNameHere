# Player Agent — Sprint Tasks

> Owner: Player Agent
> Scope: Enemy behavior, combat, boss, player-side integrations

---

## PLR-1: Fix Enemy Gravity Bug

**Priority:** P0 — Enemies floating in air
**Status:** DONE
**Dependencies:** None
**Files to investigate:**
- Enemy prefabs in `Assets/_Project/Prefabs/`
- `Assets/_Project/Scripts/Enemies/Movement/BaseEnemyMovement.cs`
- `Assets/_Project/Scripts/Enemies/Movement/GroundPatrolMovement.cs`

### Problem

Slime and Turret enemies are reported to float instead of falling to ground. Likely causes:
1. Rigidbody2D `bodyType` set to Kinematic or Static on prefab
2. `gravityScale` set to 0 on prefab
3. Something overriding gravity in movement scripts

### Investigation Steps

1. Check prefab Rigidbody2D settings (bodyType, gravityScale)
2. Check `BaseEnemyMovement.Awake()` — currently just caches rb reference (line 36), does NOT enforce settings
3. Check `GroundPatrolMovement` — patrol stops if `!IsGrounded` (line 28), which would prevent movement but not gravity

### Fix

In `BaseEnemyMovement.cs`, add gravity enforcement in `Awake()` after caching rb (line 36):

```csharp
protected virtual void Awake()
{
    rb = GetComponent<Rigidbody2D>();
    controller = GetComponent<EnemyController>();

    // Ensure dynamic body type for physics
    if (rb != null && rb.bodyType != RigidbodyType2D.Dynamic)
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        Debug.LogWarning($"[BaseEnemyMovement] {gameObject.name}: Rigidbody2D was not Dynamic, fixed.");
    }

    // Ensure gravity is enabled for ground enemies
    if (rb != null && rb.gravityScale <= 0f)
    {
        rb.gravityScale = 1f;
        Debug.LogWarning($"[BaseEnemyMovement] {gameObject.name}: gravityScale was 0, set to 1.");
    }

    SetupCheckPoints();
}
```

**Important:** FlyingMovement.cs should override Awake to NOT enforce gravity. Check `FlyingMovement.cs` and ensure it sets `gravityScale = 0` after base.Awake().

### Acceptance Criteria

- Slimes fall to the ground when spawned
- Turrets fall to the ground when spawned
- Flying enemies (if any) still work correctly
- No Rigidbody2D warnings in console during normal play
- Ground detection works after gravity fix

---

## PLR-2: Add Combat SFX Fields + Playback

**Priority:** P1
**Status:** DONE
**Dependencies:** SYS-4 (SFXManager)
**Files to modify:**
- `Assets/_Project/Scripts/Combat/Data/AttackData.cs`
- `Assets/_Project/Scripts/Combat/CombatController.cs`

### AttackData.cs Changes

Add after `[Header("VFX")]` section (line 45):

```csharp
[Header("Audio")]
[Tooltip("Sound played when attack starts")]
public AudioClip attackSound;
[Tooltip("Sound played when attack hits a target")]
public AudioClip hitSound;
```

### CombatController.cs Changes

1. **Add AudioSource field** — cache or create in Awake:

```csharp
private AudioSource audioSource;

// In Awake(), after existing cache lines:
audioSource = GetComponent<AudioSource>();
if (audioSource == null)
{
    audioSource = gameObject.AddComponent<AudioSource>();
    audioSource.playOnAwake = false;
    audioSource.spatialBlend = 0f;
}
```

2. **Play attack sound** — in `EnterAttackingState()` (line 178), after `SpawnHitbox()`:

```csharp
private void EnterAttackingState()
{
    currentState = CombatState.Attacking;
    stateTimer = currentAttack.activeDuration;

    SpawnHitbox();

    // Play attack sound
    if (currentAttack.attackSound != null)
    {
        SFXManager.PlayOneShot(audioSource, currentAttack.attackSound);
    }

    OnAttackStarted?.Invoke(currentAttack);
}
```

3. **Play hit sound** — in `ReportHit()` (line 654), after `OnAttackHit`:

```csharp
public void ReportHit(AttackData attack, Collider2D target)
{
    OnAttackHit?.Invoke(attack);

    // Play hit sound
    if (attack.hitSound != null)
    {
        SFXManager.PlayOneShot(audioSource, attack.hitSound);
    }

    // Handle pogo bounce for down attacks
    // ... existing code
}
```

### Acceptance Criteria

- AttackData assets can have attack and hit sounds assigned in Inspector
- Attack sound plays when attack becomes active
- Hit sound plays when attack connects with enemy
- Sounds respect SFX volume slider
- No errors when sounds are null (optional fields)

---

## PLR-3: Update EnemyController.PlaySound to Use SFXManager

**Priority:** P1
**Status:** DONE
**Dependencies:** SYS-4 (SFXManager)
**Files to modify:** `Assets/_Project/Scripts/Enemies/Core/EnemyController.cs`

### Change

Replace `PlaySound` method (line 580-586):

```csharp
// Before:
private void PlaySound(AudioClip clip)
{
    if (clip == null || audioSource == null)
        return;

    audioSource.PlayOneShot(clip);
}

// After:
private void PlaySound(AudioClip clip)
{
    if (clip == null || audioSource == null)
        return;

    SFXManager.PlayOneShot(audioSource, clip);
}
```

### Acceptance Criteria

- Enemy sounds (idle, alert, attack, hurt, death) respect SFX volume
- Volume changes via Options slider immediately affect enemy sounds
- No behavior change when SFXManager.GetVolume() returns 1.0

---

## PLR-4: Create BossController with Phases

**Priority:** P2
**Status:** DONE
**Dependencies:** PLR-1
**Files to create:** `Assets/_Project/Scripts/Enemies/Core/BossController.cs`

### Design

BossController wraps/extends EnemyController functionality with a phase system.

```csharp
using System;
using UnityEngine;

/// <summary>
/// Boss-specific controller adding phase transitions and special attacks.
/// Works alongside EnemyController, EnemyCombat, and movement components.
/// </summary>
[RequireComponent(typeof(EnemyController))]
[RequireComponent(typeof(HealthSystem))]
public class BossController : MonoBehaviour
{
    [Header("Boss Identity")]
    [SerializeField] private string bossName = "Boss";

    [Header("Phase Configuration")]
    [SerializeField] private float phase2HealthPercent = 0.5f;
    [SerializeField] private float enrageHealthPercent = 0.2f;

    [Header("Phase 2 Modifiers")]
    [SerializeField] private float phase2SpeedMultiplier = 1.3f;
    [SerializeField] private float phase2DamageMultiplier = 1.2f;
    [SerializeField] private float phase2CooldownMultiplier = 0.7f;

    [Header("Enrage Modifiers")]
    [SerializeField] private float enrageSpeedMultiplier = 1.5f;
    [SerializeField] private float enrageDamageMultiplier = 1.5f;

    [Header("VFX")]
    [SerializeField] private GameObject phaseChangeVFX;
    [SerializeField] private GameObject enrageVFX;

    [Header("Audio")]
    [SerializeField] private AudioClip entranceSound;
    [SerializeField] private AudioClip phaseChangeSound;
    [SerializeField] private AudioClip enrageSound;

    public enum BossPhase { Phase1, Phase2, Enraged, Dead }

    private EnemyController enemyController;
    private HealthSystem healthSystem;
    private AudioSource audioSource;
    private BossPhase currentPhase = BossPhase.Phase1;

    public string BossName => bossName;
    public BossPhase CurrentPhase => currentPhase;
    public HealthSystem BossHealth => healthSystem;

    public event Action<BossPhase> OnPhaseChanged;
    public event Action OnBossDefeated;

    // Subscribe to healthSystem.OnHealthChanged to check phase thresholds
    // On phase change: play VFX/SFX, modify stats, fire events
    // On death: fire OnBossDefeated, call GameManager.ExitBossFight()
}
```

**Key behaviors:**
- Subscribe to `HealthSystem.OnHealthChanged` and `HealthSystem.OnDeath`
- On entrance: call `GameManager.Instance.EnterBossFight()`, play entrance sound
- Phase transitions based on HP %
- On defeat: call `GameManager.Instance.ExitBossFight()`, fire `OnBossDefeated`

### Acceptance Criteria

- Boss enters Phase 2 at 50% HP
- Phase transition plays VFX and sound
- Boss enters Enraged state at 20% HP
- On death, BossFight state ends in GameManager
- Events fire for UI to react

---

## PLR-5: Integrate Boss into WaveManager

**Priority:** P2
**Status:** DONE
**Dependencies:** PLR-4, SYS-6
**Files to modify:**
- `Assets/_Project/Scripts/Enemies/Spawning/WaveConfig.cs`
- `Assets/_Project/Scripts/Enemies/Spawning/WaveManager.cs`

### WaveConfig.cs Changes

Add after `[Header("Timing")]` section (after line 44):

```csharp
[Header("Boss Waves")]
[Tooltip("Spawn a boss every N waves. 0 = no boss waves.")]
public int bossWaveInterval = 5;
[Tooltip("Boss prefab to spawn on boss waves")]
public GameObject bossPrefab;
```

### WaveManager.cs Changes

Modify `TransitionToSpawning()` (line 120) to check for boss wave:

```csharp
private void TransitionToSpawning()
{
    currentState = WaveState.Spawning;

    // Check if this is a boss wave
    bool isBossWave = waveConfig.bossWaveInterval > 0
        && waveConfig.bossPrefab != null
        && currentWave % waveConfig.bossWaveInterval == 0;

    if (isBossWave)
    {
        SpawnBoss();
        return;
    }

    // Normal wave spawning (existing code)
    totalEnemiesThisWave = WaveScaler.GetEnemyCount(
        currentWave,
        waveConfig.baseEnemyCount,
        waveConfig.enemiesPerWaveIncrease,
        waveConfig.maxEnemiesAlive);
    spawnedThisWave = 0;

    if (debugLogging)
        Debug.Log($"[WaveManager] Wave {currentWave} — spawning {totalEnemiesThisWave} enemies");

    OnWaveStarted?.Invoke(currentWave);
    activeCoroutine = StartCoroutine(SpawnCoroutine());
}

private void SpawnBoss()
{
    totalEnemiesThisWave = 1;
    spawnedThisWave = 1;

    if (debugLogging)
        Debug.Log($"[WaveManager] Wave {currentWave} — BOSS WAVE!");

    OnWaveStarted?.Invoke(currentWave);

    spawnManager.SpawnEnemy(waveConfig.bossPrefab, currentWave, waveConfig);

    currentState = WaveState.Active;
    activeCoroutine = null;
}
```

### Acceptance Criteria

- Boss spawns every `bossWaveInterval` waves (default 5)
- Boss wave only spawns 1 enemy (the boss)
- Normal waves work unchanged when `bossWaveInterval = 0`
- Wave clear triggers after boss death
- Boss gets wave scaling like normal enemies

---

## PLR-6: Integrate StatSystem into PlayerControllerScript

**Priority:** P1
**Status:** DONE
**Dependencies:** SYS-3 (StatSystem)
**Files to modify:** `Assets/_Project/Scripts/Player/PlayerControllerScript.cs`

### Changes

1. **Cache StatSystem** in Awake:

```csharp
private StatSystem statSystem;

// In Awake, add:
statSystem = GetComponent<StatSystem>();
```

2. **Apply AGI speed multiplier** in FixedUpdate horizontal movement (line 162):

```csharp
// Before:
rb.linearVelocity = new Vector2(horizontal * speed * moveMultiplier, rb.linearVelocity.y);

// After:
float agiMultiplier = statSystem != null ? statSystem.SpeedMultiplier : 1f;
rb.linearVelocity = new Vector2(horizontal * speed * moveMultiplier * agiMultiplier, rb.linearVelocity.y);
```

3. **Also update RefreshAbilities** to re-cache StatSystem:

```csharp
public void RefreshAbilities()
{
    doubleJumpAbility = GetComponent<DoubleJumpAbility>();
    dashAbility = GetComponent<DashAbility>();
    combatController = GetComponent<CombatController>();
    statSystem = GetComponent<StatSystem>();
}
```

### Acceptance Criteria

- Player moves faster as AGI increases
- Base speed unaffected when StatSystem not present
- Speed multiplier is 1.0 at 0 AGI, scales linearly
- Movement feels responsive at high AGI values (not too fast)

---

## PLR-7: Integrate StatSystem into CombatController

**Priority:** P1
**Status:** DONE
**Dependencies:** SYS-3 (StatSystem)
**Files to modify:** `Assets/_Project/Scripts/Combat/CombatController.cs`

### Changes

1. **Cache StatSystem** in Awake:

```csharp
private StatSystem statSystem;

// In Awake, add:
statSystem = GetComponent<StatSystem>();
```

2. **Apply STR damage multiplier** in `SpawnMeleeHitbox()` — the hitbox initialization should use modified damage. Update `AttackHitbox.Initialize()` call to pass damage multiplier, or apply it where damage is dealt.

The cleanest approach: modify `ReportHit()` or the damage calculation point. Since `AttackHitbox` deals damage using `attackData.baseDamage`, the multiplier should be applied there.

**Option A (preferred):** Add a public property that AttackHitbox reads:

```csharp
/// <summary>
/// Returns the damage multiplier from stats.
/// </summary>
public float GetDamageMultiplier()
{
    return statSystem != null ? statSystem.MeleeDamageMultiplier : 1f;
}
```

Then in `AttackHitbox.cs`, when dealing damage, multiply by `combatController.GetDamageMultiplier()`.

**Option B:** Pass multiplier into `AttackHitbox.Initialize()`.

### Acceptance Criteria

- Melee attacks deal more damage as STR increases
- Base damage unaffected when StatSystem not present
- Damage multiplier is 1.0 at 0 STR, scales linearly
- Displayed damage numbers reflect the multiplier
