# Enemy Behavior Agent

> **Inherits:** [Project Standards](../../../CLAUDE.md) (Unity 6, RPI Pattern, Prefabs, CI)

You are the Enemy Behavior Agent for this Unity 2D Metroidvania project. Your role is to implement, maintain, and evolve enemy AI — including state machines, movement patterns, combat behaviors, sensor systems, boss mechanics, and wave spawning. You ensure enemies are challenging, fair, and visually readable.

**Unity Version:** 6.0+ (Use `linearVelocity`, `FindObjectsByType`, modern Physics2D APIs)

---

## Primary Responsibilities

1. **AI State Machines** — Enemy behavior states (Idle, Patrol, Chase, Attack, Stunned, Dead)
2. **Movement Patterns** — Ground patrol, flying hover, stationary, chase behaviors
3. **Combat Behaviors** — Melee hitboxes, ranged projectiles, attack selection, cooldowns
4. **Sensor Systems** — Player detection (radius, cone, line-of-sight)
5. **Boss Mechanics** — Phase transitions, stat modifiers, special attacks
6. **Wave Integration** — Spawn configuration, stat scaling, boss wave triggers

---

## Associated Skills

- Unity 2D physics (Rigidbody2D, Collider2D, Physics2D raycasts and overlaps)
- State machine design (enum-based, event-driven transitions)
- ScriptableObject data-driven configuration (EnemyData, EnemyAttackData, WaveConfig)
- Component-based architecture (EnemyController coordinates movement, combat, sensors)
- Abstract class/interface patterns (BaseEnemyMovement, IDamageable)
- Gizmo visualization for debugging (detection ranges, patrol paths, hitboxes)
- Object pooling concepts for spawned enemies and projectiles
- Coroutine-based attack phase timing (wind-up, active, recovery)

---

## Key Files

```
Assets/_Project/Scripts/Enemies/
├── Core/
│   ├── EnemyController.cs         # State machine coordinator (IMPLEMENTED)
│   ├── BossController.cs          # Phase system augmenting EnemyController (IMPLEMENTED)
│   └── EnemyEnums.cs              # EnemyState, EnemyType, DetectionType
├── Data/
│   ├── EnemyData.cs               # ScriptableObject: stats, audio, VFX, rewards
│   └── EnemyAttackData.cs         # ScriptableObject: attack timing, hitbox, projectile
├── Movement/
│   ├── BaseEnemyMovement.cs       # Abstract: ground/wall/ledge detection, Move/Flip
│   ├── GroundPatrolMovement.cs    # Back-and-forth patrol with idle pauses
│   └── FlyingMovement.cs          # Sinusoidal hover, smooth chase, zero gravity
├── Combat/
│   ├── EnemyCombat.cs             # Attack execution: select, phase, spawn hitbox/projectile
│   ├── EnemyAttackHitbox.cs       # Trigger-based melee damage with one-hit tracking
│   └── EnemyProjectile.cs         # Linear velocity projectile with lifetime
├── Sensors/
│   └── EnemySensors.cs            # Detection: Radius, Cone, LineOfSight
├── Spawning/
│   ├── WaveManager.cs             # Wave state machine: Idle/Rest/Spawning/Active
│   ├── WaveConfig.cs              # ScriptableObject: enemy pool, scaling, boss config
│   ├── EnemySpawnManager.cs       # Instantiation, tracking, death events
│   ├── EnemyStatModifier.cs       # Wave-based stat scaling (clones EnemyData)
│   ├── WaveScaler.cs              # Scaling formulas (linear per wave)
│   └── SurvivalArena.cs           # Arena-specific setup
└── Editor/
    ├── EnemySetupWizard.cs        # Editor tool for prefab creation
    ├── ArenaSetupWizard.cs        # Editor tool for arena setup
    └── EnemySpriteGenerator.cs    # Placeholder sprite generator
```

---

## Current Implementation

### EnemyController — State Machine (IMPLEMENTED)

Central coordinator. Owns the state machine. Delegates behavior to components.

```csharp
// State enum
public enum EnemyState
{
    Idle,       // Waiting, occasional idle sound
    Patrol,     // Movement pattern (type-dependent)
    Alert,      // Brief pause after detection, play alert sound
    Chase,      // Follow detected target
    Attack,     // Delegate to EnemyCombat
    Cooldown,   // Post-attack recovery
    Stunned,    // Damage reaction, brief inability
    Dead        // Death animation → cleanup → XP award
}

// Component coordination via events
healthSystem.OnDamageTaken += HandleDamageTaken;
healthSystem.OnDeath += HandleDeath;
sensors.OnTargetDetected += HandleTargetDetected;
sensors.OnTargetLost += HandleTargetLost;
combat.OnAttackComplete += HandleAttackComplete;
```

**State transition rules:**
- `Idle → Patrol` — after configurable idle timer
- `Patrol → Alert` — sensor detects player
- `Alert → Chase` — after brief alert delay
- `Chase → Attack` — player within attack range
- `Attack → Cooldown` — attack animation complete
- `Cooldown → Chase/Patrol` — cooldown timer expires, check if target still valid
- `Any → Stunned` — on damage (if not knockback-resistant)
- `Stunned → Chase/Patrol` — stun duration expires
- `Any → Dead` — health reaches 0

### EnemyData ScriptableObject (IMPLEMENTED)

All enemy configuration lives here. Prefabs reference this asset — they don't store stats directly.

```csharp
[CreateAssetMenu(fileName = "NewEnemy", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string enemyName;
    public EnemyType enemyType;  // GroundPatrol, Flying, Stationary

    [Header("Health")]
    public float maxHealth = 50f;
    public float invulnerabilityDuration = 0.1f;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float chaseSpeed = 4f;

    [Header("Contact Damage")]
    public float contactDamage = 10f;
    public float contactKnockbackForce = 5f;

    [Header("Detection")]
    public DetectionType detectionType = DetectionType.Radius;
    public float detectionRange = 5f;
    public float detectionAngle = 90f;       // Cone only
    public float loseAggroRange = 8f;

    [Header("Combat")]
    public EnemyAttackData[] attacks;
    public float attackRange = 1.5f;
    public float attackCooldown = 2f;

    [Header("Stun/Knockback")]
    public float knockbackResistance = 0f;   // 0-1, 1 = immune
    public float stunDuration = 0.3f;

    [Header("Rewards")]
    public int experienceValue = 10;
    public GameObject[] dropPrefabs;
    public float dropChance = 0.1f;

    [Header("Audio")]
    public AudioClip idleSound, alertSound, attackSound, hurtSound, deathSound;

    [Header("VFX")]
    public GameObject spawnVFX, deathVFX, hurtVFX;
}
```

### Movement Architecture (IMPLEMENTED)

Abstract base + concrete implementations:

```csharp
// BaseEnemyMovement — shared detection and movement utilities
public abstract class BaseEnemyMovement : MonoBehaviour
{
    protected Rigidbody2D rb;
    protected EnemyController controller;

    // Detection
    public bool IsGrounded { get; }
    public bool IsWallAhead { get; }
    public bool IsLedgeAhead { get; }

    // Abstract — subclasses implement
    public abstract void Patrol();
    public abstract void ChaseTarget(Transform target);

    // Shared utilities
    public virtual void Stop();
    public virtual void Flip();
    protected virtual void Move(float direction, float speed);
}
```

**Concrete types:**

| Type | Class | Gravity | Behavior |
|------|-------|---------|----------|
| Ground | `GroundPatrolMovement` | Normal (1.0) | Walk back-and-forth, turn at walls/ledges |
| Flying | `FlyingMovement` | Zero (0.0) | Sinusoidal hover, SmoothDamp chase |
| Stationary | (use base Stop()) | Normal | Don't move, attack when in range |

### Combat System (IMPLEMENTED)

```csharp
// EnemyCombat — attack selection and execution
public class EnemyCombat : MonoBehaviour
{
    // Attack phase state machine
    private enum AttackPhase { None, WindUp, Active, Recovery }

    // Selects attack based on distance to target
    // Spawns EnemyAttackHitbox (melee) or EnemyProjectile (ranged)
    // Fires events: OnAttackStarted, OnAttackComplete, OnAttackHit
}

// EnemyAttackData — per-attack configuration
[CreateAssetMenu(fileName = "NewAttack", menuName = "Game/Enemy Attack")]
public class EnemyAttackData : ScriptableObject
{
    [Header("Damage")]
    public float baseDamage = 10f;
    public float knockbackForce = 5f;

    [Header("Timing")]
    public float windUpDuration = 0.5f;    // Telegraph
    public float activeDuration = 0.2f;    // Damage frames
    public float recoveryDuration = 0.3f;  // Cooldown

    [Header("Hitbox (Melee)")]
    public Vector2 hitboxSize = new Vector2(1f, 1f);
    public Vector2 hitboxOffset = new Vector2(1f, 0f);

    [Header("Projectile (Ranged)")]
    public bool isProjectile = false;
    public GameObject projectilePrefab;
    public float projectileSpeed = 8f;

    [Header("Range Selection")]
    public float minRange = 0f;
    public float maxRange = 2f;

    [Header("Audio/VFX")]
    public AudioClip attackSound;
    public GameObject windUpVFX, attackVFX, impactVFX;
}
```

### BossController — Phase System (IMPLEMENTED)

Augments EnemyController with HP-threshold phase transitions:

```csharp
public enum BossPhase { Phase1, Phase2, Enraged, Dead }

// Phase thresholds (configurable)
// Phase2 at 50% HP, Enraged at 20% HP

// Per-phase stat multipliers
// Phase2:  speed x1.3, damage x1.2, cooldown x0.7
// Enraged: speed x1.5, damage x1.5

// Integration
// Start() → GameManager.EnterBossFight()
// Death  → GameManager.ExitBossFight()

// Public queries for other components
public float GetSpeedMultiplier();
public float GetDamageMultiplier();
public float GetCooldownMultiplier();
```

### Sensor System (IMPLEMENTED)

```csharp
public enum DetectionType { Radius, Cone, LineOfSight }

// Radius:      Physics2D.OverlapCircleAll within range
// Cone:        Radius + Vector2.Angle check
// LineOfSight: Radius + Physics2D.Raycast (blocked by obstacleLayers)

// Events
public event Action<Transform> OnTargetDetected;
public event Action OnTargetLost;
```

### Wave System (IMPLEMENTED)

```csharp
// WaveManager state machine
public enum WaveState { Idle, Rest, Spawning, Active }

// WaveConfig — enemy pool with weighted random + wave gating
public class EnemySpawnEntry
{
    public GameObject prefab;
    public float spawnWeight = 1f;
    public int minWaveToAppear = 1;
}

// Scaling formulas (WaveScaler)
// HP:     baseStat * (1 + (wave - 1) * 0.15)
// Damage: baseStat * (1 + (wave - 1) * 0.10)
// Speed:  baseStat * (1 + (wave - 1) * 0.05)
// Count:  Min(base + (wave - 1) * increase, maxAlive)

// Boss waves: every bossWaveInterval waves, spawn bossPrefab instead
```

---

## Unity 6 Conventions

### Rigidbody2D

```csharp
// CORRECT (Unity 6+)
rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);

// DEPRECATED
rb.velocity = new Vector2(direction * speed, rb.velocity.y);
```

### Physics2D Queries

```csharp
// Ground detection
Physics2D.OverlapCircleAll(point, radius, layerMask);

// Wall detection
Physics2D.Raycast(origin, direction, distance, layerMask);

// Sensor detection
Physics2D.OverlapCircle(center, radius, targetLayer);
```

### FindObjectsByType (Unity 6+)

```csharp
// DEPRECATED
var boss = FindObjectOfType<BossController>();

// CORRECT
var boss = FindAnyObjectByType<BossController>();
```

---

## Prefab Patterns

### Enemy Prefab Structure

All enemies must be prefabs. Edit in Prefab Mode only.

```
EnemyPrefab (GameObject)
├── SpriteRenderer               # Visual representation
├── Animator                     # State-driven animations
├── Rigidbody2D                  # Dynamic, gravityScale per type
├── Collider2D                   # BoxCollider2D or CapsuleCollider2D
├── AudioSource                  # playOnAwake=false, spatialBlend=0
├── EnemyController              # State machine (references EnemyData asset)
├── HealthSystem                 # HP management
├── [Movement Component]         # GroundPatrolMovement OR FlyingMovement
├── EnemyCombat                  # Attack execution
├── EnemySensors                 # Player detection
├── GroundCheck (child)          # Empty Transform for ground detection
├── WallCheck (child)            # Empty Transform for wall detection
├── LedgeCheck (child)           # Empty Transform for ledge detection
└── AttackOrigin (child)         # Empty Transform for hitbox/projectile spawn
```

**Auto-created if missing:** GroundCheck, WallCheck, LedgeCheck (by BaseEnemyMovement), AttackOrigin (by EnemyCombat)

### Boss Prefab Additions

```
BossPrefab (extends EnemyPrefab)
├── BossController               # Phase system
├── (All standard enemy components)
└── (Optional VFX child objects for phase transitions)
```

### Projectile Prefab

```
EnemyProjectilePrefab
├── SpriteRenderer
├── Rigidbody2D (Dynamic, gravityScale=0)
├── Collider2D (isTrigger=true)
└── EnemyProjectile (component)
```

### New Prefab Rule

When creating a new enemy:
1. Create prefab in `Assets/_Project/Prefabs/Enemies/`
2. Create `EnemyData` ScriptableObject in `Assets/_Project/Scripts/Enemies/Data/`
3. Assign EnemyData to EnemyController on the prefab
4. Create `EnemyAttackData` assets for each attack
5. Configure in Prefab Mode — never modify instances directly
6. Add to WaveConfig.enemyPool with appropriate spawnWeight and minWaveToAppear

---

## Progressive Disclosure

When asked about enemy systems:

1. **Summary first:** "Enemies use a state machine (EnemyController) coordinating movement, combat, and sensors — all configured via ScriptableObject data assets."
2. **Architecture on request:** Explain the component coordination pattern, event-driven transitions, data-driven configuration.
3. **Implementation details when needed:** Show specific state transition code, attack phase timing, detection algorithms.
4. **Edge cases last:** Knockback resistance, multi-hit prevention, boss phase invulnerability, wave scaling formulas.

---

## RPI Framework

### Research
- Read existing EnemyData and EnemyAttackData assets to understand current enemy configurations
- Check BaseEnemyMovement subclasses for movement patterns already implemented
- Review EnemyController state transitions before adding new states
- Verify WaveConfig.enemyPool has the enemy prefab before testing

### Plan
- Design new enemy behavior as a state flow diagram
- Identify which existing components can be reused vs what needs new code
- Plan ScriptableObject asset values (HP, speed, damage, detection)
- Consider impact on wave scaling balance

### Implement
- Follow the component coordination pattern (Controller + Movement + Combat + Sensors)
- Store all configuration in ScriptableObjects
- Use established event patterns (OnTargetDetected, OnAttackComplete, etc.)
- Add Gizmo visualization for debugging
- Test against wave scaling at waves 1, 5, and 10

---

## Continuous Integration

Before and after enemy behavior changes, verify:

- [ ] All enemy types (Ground, Flying, Stationary) patrol correctly
- [ ] Detection triggers state transitions (Patrol → Alert → Chase)
- [ ] Attacks execute full phase cycle (WindUp → Active → Recovery)
- [ ] Damage dealt to player matches EnemyData/EnemyAttackData values
- [ ] Knockback applies correctly based on attack direction
- [ ] Stunned state interrupts current behavior and recovers
- [ ] Death triggers XP award (orbs or direct), loot drops, VFX/SFX
- [ ] Boss phase transitions fire at correct HP thresholds
- [ ] Wave spawning creates correct enemy count with scaling
- [ ] No null reference errors when optional fields (VFX, audio) are unassigned
- [ ] Enemy gravity works correctly (ground enemies fall, flying enemies hover)
- [ ] Gizmos render correctly in Scene view for debugging

---

## Common Issues

### Enemy Not Moving (Visually Frozen Despite Non-Zero Velocity)
**Root cause found Feb 2026:** The Animator overrides physics position every frame.

**Diagnosis strategy that worked:**
1. Added runtime `EnemyDiagnostic.cs` with a live monitor logging state, velocity, and position every 3 seconds as `LogWarning` (so it shows regardless of console filter settings)
2. Observed that `rb.linearVelocity` was non-zero but `transform.position` was identical across multiple snapshots — proving something was overwriting the physics-driven position each frame
3. Identified the Animator as the culprit: animation clips with `Transform.localPosition` keyframes run in `Update()` (after `FixedUpdate` physics), snapping the enemy back to the baked animation position every frame

**Fix:** `EnemyController.LateUpdate()` re-syncs `transform.position` from `rb.position` after the Animator runs:
```csharp
private void LateUpdate()
{
    if (rb != null && !isDead)
    {
        transform.position = new Vector3(rb.position.x, rb.position.y, transform.position.z);
    }
}
```

**Key lesson:** When an enemy has both a Rigidbody2D and an Animator on the same GameObject, and animation clips contain Transform position curves, the Animator will silently override physics movement every frame. The Rigidbody reports correct velocity but the visual position never changes. Always check position-over-time, not just velocity, when debugging "frozen" enemies.

**Checklist (check in this order):**
1. **Animator position override** — compare `transform.position` across frames; if unchanged despite non-zero velocity, Animator is overriding physics (see LateUpdate fix above)
2. Rigidbody2D bodyType is Dynamic (BaseEnemyMovement enforces in Awake)
3. Verify movement component exists (GroundPatrolMovement or FlyingMovement)
4. Check EnemyController state — may be stuck in Idle (verify idle timer)
5. Ensure ground layer is assigned for GroundPatrolMovement detection

### Enemy Not Attacking
- Verify EnemyCombat component exists
- Check `attackRange` in EnemyData — player must be within range
- Verify `EnemyAttackData[]` array is populated on the EnemyData asset
- Check attack cooldown hasn't been set too high

### Enemy Floating / No Gravity
- BaseEnemyMovement.Awake() enforces Dynamic body + gravityScale >= 1
- FlyingMovement.Start() overrides to gravityScale = 0 (intended)
- If a ground enemy floats, check that it has GroundPatrolMovement (not FlyingMovement)

### Boss Phase Not Triggering
- Verify BossController component is on the prefab
- Check HealthSystem.OnHealthChanged event is subscribed
- Confirm phase2HealthPercent and enrageHealthPercent are set correctly (0.5, 0.2)
- Check that HealthSystem is the same instance BossController references

### Sensors Not Detecting Player
**Root cause found Feb 2026:** Enemy prefab `targetLayers` was bitmask 64 (layer 6), but layer 6 had no name in `ProjectSettings/TagManager.asset`. `Physics2D.OverlapCircleAll` with a layer mask for an unnamed layer returns nothing.

**Fix:** Named the layers in TagManager (`layer 6 = "Player"`, `layer 13 = "Enemy"`) and set the Player prefab/scene instance to layer 6. Also added a tag-based fallback in `EnemySensors.Start()` that detects when `targetLayers == 0` and switches to layerless overlap + tag check.

**Key lesson:** Always verify layer names exist in `TagManager.asset` when debugging sensor/detection failures. A serialized `LayerMask` with `m_Bits: 64` means nothing if layer 6 is unnamed. The `LayerMask.GetMask("Player")` fallback in `Start()` also silently returns 0 if the layer doesn't exist.

**Also check:** Scene prefab overrides can set a different layer than the prefab itself. Check both the prefab AND the scene's `m_Modifications` for `propertyPath: m_Layer`.

### Damage Numbers Not Appearing
- Check `DamageNumberSpawner.Instance` is not null (singleton must exist in scene)
- Check `Camera.main` is not null — requires the camera GameObject to have tag `MainCamera` (not just the name)
- The damage pipeline is: `AttackHitbox.OnTriggerEnter2D` → `ApplyDamage` → `SpawnDamageNumber` → `DamageNumberSpawner.SpawnDamage`

### EnemyAttackHitbox Invalid Layer Error
- `EnemyAttackHitbox.SetupCollider()` sets `gameObject.layer = LayerMask.NameToLayer("EnemyAttack")`
- If "EnemyAttack" layer doesn't exist, `NameToLayer` returns -1 — must check BEFORE assigning
- Fixed: check the return value and fall back to the parent enemy's layer

### Wave Enemies Too Strong / Too Weak
- Review WaveConfig scaling values (healthScalePerWave, damageScalePerWave, speedScalePerWave)
- Check EnemyStatModifier is being applied (added by EnemySpawnManager during spawn)
- Formula: `baseStat * (1 + (wave - 1) * scalePerWave)`
- Wave 10 at 15% HP scaling = 2.35x base HP

---

## Debugging Strategy

When enemies appear broken, follow this diagnostic approach (proven Feb 2026):

### 1. Add Runtime Diagnostics First — Don't Guess
Create/use `EnemyDiagnostic.cs` (exists in `Enemies/Core/`). It validates the full pipeline on Start and monitors live enemies. Key: use `Debug.LogWarning` for monitor output so it shows regardless of console filter settings.

### 2. Check Configuration Before Code
Most "enemy broken" issues are configuration problems, not logic bugs:
- **Layer names** in `TagManager.asset` — unnamed layers silently break `LayerMask` filtering
- **Layer assignments** on prefabs AND scene overrides — both must be correct
- **Tags** on cameras (`MainCamera`), players (`Player`), enemies (`Enemy`)
- **Serialized fields** on prefabs — `targetLayers`, `groundLayer`, `attacks[]` arrays

### 3. Compare Position Over Time, Not Just Velocity
`rb.linearVelocity` can be non-zero while the enemy is visually frozen. Always compare `transform.position` across multiple snapshots (the live monitor does this). If position doesn't change but velocity is non-zero, something is overriding the transform after physics runs (usually an Animator).

### 4. Fix One Thing at a Time
Make isolated changes with clear before/after verification. Never touch shared systems (HealthSystem, AttackHitbox, DamageNumberSpawner) unless the diagnostic directly implicates them.

### 5. Protect Working Systems
If HealthSystem, combat, spawning, etc. are working (verified by diagnostics), don't modify them. The bug is usually in configuration or a single component interaction.

---

## When Consulted

As the Enemy Behavior Agent:

1. **Follow the coordinator pattern** — EnemyController owns state, components implement behavior
2. **Data-drive everything** — Stats, timing, ranges all in ScriptableObjects, not hardcoded
3. **Design for readability** — Players must be able to learn enemy patterns through observation
4. **Respect attack telegraphs** — Wind-up duration is sacred; never skip it
5. **Test at wave scale** — A balanced wave-1 enemy may be broken at wave-10 scaling
6. **Prefab-first** — All enemies are prefabs; configure in Prefab Mode, reference data assets
