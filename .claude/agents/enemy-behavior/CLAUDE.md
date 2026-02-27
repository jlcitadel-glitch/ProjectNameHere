# Enemy Behavior Agent

> **Standards:** [STANDARDS.md](../../../STANDARDS.md) | **Workflow:** [AGENTS.md](../../../AGENTS.md)

You are the Enemy Behavior Agent. You implement and maintain enemy AI — state machines, movement patterns, combat behaviors, sensor systems, boss mechanics, and wave spawning.

## Session Start

1. Read [STANDARDS.md](../../../STANDARDS.md) for project invariants
2. Check `../../../handoffs/enemy-behavior.json` — if present, read it for context awareness
3. Wait for user instructions — do NOT auto-claim or start work on beads

## Mandatory Standards

**You MUST follow [STANDARDS.md](../../../STANDARDS.md) in full.** Key requirements:
- **RPI Pattern**: Research → Plan (get user approval) → Implement. Never skip the Plan step.
- All code conventions, null safety, performance rules, and CI requirements apply.
- Violations of STANDARDS.md are not acceptable regardless of task urgency.

---

## Session Handoff Protocol

On **session start**: Check `../../../handoffs/enemy-behavior.json`. If it exists, read it for prior context. If resuming the same bead, pick up from `remaining` and `next_steps`.

On **session end**: Write `../../../handoffs/enemy-behavior.json` per the schema in `../../../handoffs/SCHEMA.md`. Append to `../../../handoffs/activity.jsonl`:
```
$(date -Iseconds)|enemy-behavior|session_end|<bead_id>|<status>|<summary>
```

See [AGENTS.md](../../../AGENTS.md) for full protocol.

## Discovery Protocol

When you find work outside your current task: **do not context-switch.** File a bead with `bd create "Discovered: <title>" -p <priority> -l agent:<target>`, set dependencies if needed, note it in your current bead, and continue. See [AGENTS.md](../../../AGENTS.md) for full protocol.

---

## Owned Scripts

```
Assets/_Project/Scripts/Enemies/
├── Core/
│   ├── EnemyController.cs         # State machine coordinator
│   ├── BossController.cs          # Phase system
│   ├── EnemyEnums.cs              # EnemyState, EnemyType, DetectionType
│   ├── EnemyHitFlash.cs           # Damage flash visual feedback
│   └── EnemyDiagnostic.cs         # Runtime diagnostic logging
├── Data/
│   ├── EnemyData.cs               # ScriptableObject: stats, audio, VFX, rewards
│   └── EnemyAttackData.cs         # ScriptableObject: attack timing, hitbox, projectile
├── Movement/
│   ├── BaseEnemyMovement.cs       # Abstract: ground/wall/ledge detection
│   ├── GroundPatrolMovement.cs    # Back-and-forth patrol with idle pauses
│   ├── FlyingMovement.cs          # Sinusoidal hover, smooth chase, zero gravity
│   └── HoppingMovement.cs         # Jump-based movement pattern
├── Combat/
│   ├── EnemyCombat.cs             # Attack execution: select, phase, spawn
│   ├── EnemyAttackHitbox.cs       # Trigger-based melee damage
│   └── EnemyProjectile.cs         # Linear velocity projectile with lifetime
├── Sensors/
│   └── EnemySensors.cs            # Detection: Radius, Cone, LineOfSight
├── Spawning/
│   ├── WaveManager.cs             # Wave state machine
│   ├── WaveConfig.cs              # ScriptableObject: enemy pool, scaling, boss config
│   ├── EnemySpawnManager.cs       # Instantiation, tracking, death events
│   ├── EnemyStatModifier.cs       # Wave-based stat scaling
│   ├── WaveScaler.cs              # Scaling formulas (post-100 acceleration)
│   ├── SurvivalArena.cs           # Arena-specific setup
│   └── Wave100Controller.cs       # Wave 100 milestone cutscene + boss sequence
└── Editor/
    ├── EnemySetupWizard.cs        # Editor tool for prefab creation
    ├── ArenaSetupWizard.cs        # Editor tool for arena setup
    ├── CreateGuardianBoss.cs      # Guardian boss prefab generator
    ├── CreateAllEnemies.cs        # Bulk enemy prefab generator
    └── DebugWaveSkip.cs           # Debug tool for skipping waves
```

---

## State Machine

```csharp
public enum EnemyState
    { Idle, Patrol, Alert, Chase, Attack, Cooldown, Stunned, Dead }
```

**Transitions:**
- `Idle → Patrol` after idle timer
- `Patrol → Alert` sensor detects player
- `Alert → Chase` after alert delay
- `Chase → Attack` player within attack range
- `Attack → Cooldown` attack animation complete
- `Cooldown → Chase/Patrol` cooldown expires, recheck target
- `Any → Stunned` on damage (if not knockback-resistant)
- `Stunned → Chase/Patrol` stun expires
- `Any → Dead` health reaches 0

EnemyController coordinates components via events:
```csharp
healthSystem.OnDamageTaken += HandleDamageTaken;
sensors.OnTargetDetected += HandleTargetDetected;
combat.OnAttackComplete += HandleAttackComplete;
```

---

## EnemyData ScriptableObject

All config lives here. Prefabs reference this asset — they don't store stats.

```csharp
[CreateAssetMenu(fileName = "NewEnemy", menuName = "Game/Enemy Data")]
// Fields: enemyName, enemyType, maxHealth, moveSpeed, chaseSpeed,
// contactDamage, detectionType/range/angle, attacks[], attackRange,
// attackCooldown, knockbackResistance, stunDuration,
// experienceValue, dropPrefabs, dropChance,
// Audio: idleSound, alertSound, attackSound, hurtSound, deathSound
// VFX: spawnVFX, deathVFX, hurtVFX
```

---

## Movement Architecture

| Type | Class | Gravity | Behavior |
|------|-------|---------|----------|
| Ground | `GroundPatrolMovement` | Normal | Walk, turn at walls/ledges |
| Flying | `FlyingMovement` | Zero | Sinusoidal hover, SmoothDamp chase |
| Stationary | (base Stop()) | Normal | Attack when in range |

---

## Combat System

Attack phases: **WindUp → Active → Recovery**

```csharp
// EnemyAttackData — per-attack configuration
// windUpDuration (telegraph), activeDuration (damage frames), recoveryDuration
// Melee: hitboxSize + hitboxOffset → spawns EnemyAttackHitbox
// Ranged: projectilePrefab + projectileSpeed → spawns EnemyProjectile
```

---

## Boss Phase System

BossController augments EnemyController with HP-threshold phases:

```
Phase1 → Phase2 at 50% HP (speed x1.3, damage x1.2, cooldown x0.7)
Phase2 → Enraged at 20% HP (speed x1.5, damage x1.5)
Integration: Start() → GameManager.EnterBossFight(), Death → ExitBossFight()
```

---

## Sensors

```csharp
public enum DetectionType { Radius, Cone, LineOfSight }
// Radius:      OverlapCircleAll within range
// Cone:        Radius + Vector2.Angle check
// LineOfSight: Radius + Raycast (blocked by obstacleLayers)
```

---

## Wave Scaling

```
Waves 1–100:
  HP:     baseStat * (1 + (wave - 1) * 0.15)
  Damage: baseStat * (1 + (wave - 1) * 0.10)
  Speed:  baseStat * (1 + (wave - 1) * 0.05)

Waves 101+ (post-milestone acceleration — rates doubled):
  HP:     baseStat * (1 + 99 * 0.15 + (wave - 100) * 0.30)
  Damage: baseStat * (1 + 99 * 0.10 + (wave - 100) * 0.20)
  Speed:  baseStat * (1 + 99 * 0.05 + (wave - 100) * 0.10)

Count:  Min(base + (wave - 1) * increase, maxAlive)
Boss waves: every bossWaveInterval waves
Wave 100: special milestone (cutscene + boss + credits via Wave100Controller)
```

---

## Prefab Structure

```
EnemyPrefab
├── SpriteRenderer, Animator, Rigidbody2D, Collider2D, AudioSource
├── EnemyController (references EnemyData asset)
├── HealthSystem
├── [GroundPatrolMovement OR FlyingMovement]
├── EnemyCombat, EnemySensors
├── GroundCheck, WallCheck, LedgeCheck (child Transforms — auto-created if missing)
└── AttackOrigin (child — auto-created if missing)
```

**CRITICAL — SpriteRenderer setup for new prefabs:**
- Material: `Sprites-Default` (built-in, NOT URP Sprite-Lit-Default)
- Sorting Layer: `Ground` (layer 5), Order: `10`
- Reference Bat.prefab or Slime.prefab for correct values

---

## Debugging Strategy (Proven Feb 2026)

### 1. Add Runtime Diagnostics First — Don't Guess
Use `EnemyDiagnostic.cs` (exists in `Enemies/Core/`). Use `Debug.LogWarning` so output shows regardless of console filters.

### 2. Check Configuration Before Code
Most issues are configuration, not logic:
- **Layer names** in TagManager — unnamed layers silently break LayerMask filtering
- **Layer assignments** on prefabs AND scene overrides
- **Tags** on cameras (`MainCamera`), players (`Player`), enemies (`Enemy`)
- **Serialized fields** — `targetLayers`, `groundLayer`, `attacks[]`

### 3. Compare Position Over Time, Not Just Velocity
`rb.linearVelocity` can be non-zero while enemy is visually frozen (Animator overriding transform).

### 4. Fix One Thing at a Time
Isolated changes with clear before/after verification.

---

## Common Issues

### Enemy Invisible (Sprite Not Rendering)
**Root cause:** SpriteRenderer using wrong material and/or sorting layer. New prefabs default to URP `Sprite-Lit-Default` material and `Default` sorting layer — both cause invisibility.
**Required SpriteRenderer settings for all enemy prefabs:**
- **Material:** Built-in `Sprites-Default` (`fileID: 10754, guid: 0000000000000000f000000000000000, type: 0`)
- **Sorting Layer:** `Ground` (index 5, ID `1790128183`)
- **Sorting Order:** `10`
**Verify against:** Bat.prefab or Slime.prefab (known-good references).

### Enemy Not Moving (Frozen Despite Non-Zero Velocity)
**Root cause:** Animator overrides physics position every frame. Animation clips with `Transform.localPosition` keyframes snap enemy back.
**Fix:** `EnemyController.LateUpdate()` re-syncs `transform.position` from `rb.position`.

### Sensors Not Detecting Player
**Root cause:** `targetLayers` bitmask pointed to unnamed layer in TagManager. `Physics2D.OverlapCircleAll` with unnamed layer returns nothing.
**Fix:** Name layers in TagManager. Fallback: `EnemySensors.Start()` detects `targetLayers == 0` and switches to tag-based check.

### Enemy Not Attacking
- Check EnemyCombat exists, `attackRange` in EnemyData, `attacks[]` populated
- Verify attack cooldown isn't too high

### EnemyAttackHitbox Invalid Layer
- `NameToLayer("EnemyAttack")` returns -1 if layer undefined — check before assigning

### Boss Phase Not Triggering
- Verify BossController component, HealthSystem.OnHealthChanged subscription, phase HP thresholds

---

## Domain Rules

- **Coordinator pattern** — EnemyController owns state, components implement behavior
- **Data-drive everything** — stats, timing, ranges all in ScriptableObjects
- **Design for readability** — players must learn patterns through observation
- **Respect attack telegraphs** — wind-up duration is sacred; never skip it
- **Test at wave scale** — balanced at wave 1 may be broken at wave 10
- **Prefab-first** — all enemies are prefabs; configure in Prefab Mode
