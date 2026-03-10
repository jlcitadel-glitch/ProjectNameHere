# State Machine

## State Enum

```csharp
public enum EnemyState
    { Idle, Patrol, Alert, Chase, Attack, Cooldown, Stunned, Dead }
```

## Transition Diagram

```
         idle timer
  Idle ────────────► Patrol
    ▲                  │
    │                  │ sensor detects player
    │                  ▼
    │               Alert
    │                  │ alert delay expires
    │                  ▼
    │  cooldown +    Chase ◄──────────┐
    │  no target       │              │
    └──────────────────┤    cooldown  │
                       │    expires   │
          in range     ▼              │
                    Attack ──────► Cooldown
                       │
                       │  (attack complete)
                       ▼

  ┌─────────────────────────────────────────────┐
  │  Any State ──► Stunned   (on damage, if     │
  │                           not knockback-     │
  │                           resistant)         │
  │  Stunned ──► Chase/Patrol (stun expires)    │
  │  Any State ──► Dead       (health <= 0)     │
  └─────────────────────────────────────────────┘
```

## EnemyController Event Coordination

EnemyController is the single coordinator. Components never change state directly — they fire events, and EnemyController decides the transition.

```csharp
// Event subscriptions in EnemyController
healthSystem.OnDamageTaken += HandleDamageTaken;
sensors.OnTargetDetected   += HandleTargetDetected;
combat.OnAttackComplete    += HandleAttackComplete;
```

**Key principle:** Movement and combat components read state but never write it. EnemyController calls into them:
- `movement.Patrol()` / `movement.ChaseTarget()` / `movement.Stop()`
- `combat.StartAttack()` only when controller enters Attack state
- `sensors.Tick()` called by controller during appropriate states

## EnemyData ScriptableObject

All configuration lives here. Prefabs reference this asset — they don't store stats on components.

```csharp
[CreateAssetMenu(fileName = "NewEnemy", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    // Identity
    string enemyName;
    EnemyType enemyType;

    // Stats
    float maxHealth, moveSpeed, chaseSpeed;
    float contactDamage;

    // Detection
    DetectionType detectionType;
    float detectionRange, detectionAngle;

    // Combat
    EnemyAttackData[] attacks;
    float attackRange, attackCooldown;

    // Resilience
    float knockbackResistance, stunDuration;

    // Rewards
    int experienceValue;
    GameObject[] dropPrefabs;
    float dropChance;

    // Audio
    AudioClip idleSound, alertSound, attackSound, hurtSound, deathSound;

    // VFX
    GameObject spawnVFX, deathVFX, hurtVFX;
}
```

## Movement Architecture

| Type       | Class                  | Gravity | Behavior                         |
|------------|------------------------|---------|----------------------------------|
| Ground     | `GroundPatrolMovement` | Normal  | Walk, turn at walls/ledges       |
| Flying     | `FlyingMovement`       | Zero    | Sinusoidal hover, SmoothDamp chase |
| Hopping    | `HoppingMovement`      | Normal  | Jump-based movement pattern      |
| Stationary | (base `Stop()`)        | Normal  | Stand still, attack when in range |

All movement classes extend `BaseEnemyMovement`, which provides ground/wall/ledge detection via child Transforms (`GroundCheck`, `WallCheck`, `LedgeCheck` — auto-created if missing).
