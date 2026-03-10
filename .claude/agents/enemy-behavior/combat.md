# Combat

## Attack Phases

Every attack follows a three-phase sequence. Never skip or shorten phases — they are the contract between enemy behavior and player readability.

```
WindUp (telegraph) ──► Active (damage frames) ──► Recovery (punish window)
```

- **WindUp:** Visual cue for the player. Duration is sacred. Animations, color flashes, or particles signal the incoming attack.
- **Active:** Hitbox is live. Damage is dealt. Duration is short relative to wind-up.
- **Recovery:** Enemy is vulnerable. This is the player's window to counterattack. Shortening it makes enemies feel unfair.

## EnemyAttackData ScriptableObject

```csharp
[CreateAssetMenu(fileName = "NewAttack", menuName = "Game/Enemy Attack Data")]
public class EnemyAttackData : ScriptableObject
{
    // Timing
    float windUpDuration;       // telegraph duration (seconds)
    float activeDuration;       // damage window (seconds)
    float recoveryDuration;     // post-attack vulnerability (seconds)

    // Melee
    Vector2 hitboxSize;         // local-space hitbox dimensions
    Vector2 hitboxOffset;       // offset from AttackOrigin

    // Ranged
    GameObject projectilePrefab;
    float projectileSpeed;

    // Damage
    float damageAmount;
    float knockbackForce;
}
```

**Melee attacks** spawn an `EnemyAttackHitbox` at the `AttackOrigin` child Transform during the Active phase. The hitbox uses trigger collisions and destroys itself when Active ends.

**Ranged attacks** instantiate an `EnemyProjectile` from `AttackOrigin`. The projectile travels at `projectileSpeed` in the facing direction with a configurable lifetime.

## Boss Phase System

`BossController` augments `EnemyController` with HP-threshold phase transitions.

```
Phase 1 (100%-50% HP)
    │
    ▼  HP drops below 50%
Phase 2 (speed x1.3, damage x1.2, cooldown x0.7)
    │
    ▼  HP drops below 20%
Enraged (speed x1.5, damage x1.5)
```

### Phase transition details

- `BossController` subscribes to `HealthSystem.OnHealthChanged`
- On threshold cross: applies stat multipliers, may unlock new attacks
- Transitions are one-way (no de-escalation)
- Visual feedback: screen flash, particle burst, brief invulnerability during transition

### GameManager integration

```csharp
// BossController.Start()
GameManager.Instance.EnterBossFight(this);
// Shows boss health bar, may lock arena doors, changes music

// BossController death
GameManager.Instance.ExitBossFight();
// Hides boss health bar, unlocks doors, restores music
```

## NoxiousCloud

`NoxiousCloud.cs` is a special area-of-effect attack component. It creates a lingering damage zone at a position, dealing periodic damage to anything in its trigger area. Used by specific enemy types (e.g., poison-themed enemies).
