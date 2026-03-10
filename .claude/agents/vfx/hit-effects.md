# Hit and State VFX

All scripts in `Assets/_Project/Scripts/VFX/`. Most follow the same pattern: create a ParticleSystem in code on Awake, burst particles, self-destruct via `SelfDestructVFX`.

## SelfDestructVFX Pattern

**Script:** `SelfDestructVFX.cs` -- attach to any VFX prefab root.

```csharp
// Checks every frame:
// 1. If timer >= maxLifetime (default 5s), destroy immediately
// 2. If all child ParticleSystems have stopped, destroy
// Always waits at least 0.1s before checking completion
```

Every one-shot VFX script adds `SelfDestructVFX` as the last step of its `SpawnBurst()` method. This prevents leaked GameObjects.

---

## Script Reference

### One-Shot Burst VFX (spawn, burst, self-destruct)

| Script | Trigger | Shape | Particles | Notes |
|---|---|---|---|---|
| `EnemyDeathVFX` | Awake (assign to EnemyData.deathVFX) | Circle, radius 0.2 | 20, radial burst | Red, gravity 0.5, shrink to 0 |
| `EnemySpawnVFX` | Awake (assign to EnemyData.spawnVFX) | Cone 40 deg, upward | 16, upward burst | Purple, negative gravity, grow-then-shrink |
| `SkillHitVFX` | Awake (assign to hitEffectPrefab) | Circle, radius 0.1 | 12, radial burst | Yellow-orange, shrink to 0 |
| `KnockbackVFX` | Static `Spawn(pos, dir)` | Cone 25 deg, directional | 8, aimed burst | Rotated to face knockback direction |

### Persistent/Attached VFX

| Script | Attached to | Behavior |
|---|---|---|
| `DashTrailVFX` | Player (requires DashAbility) | Looping trail, emission toggled by `DashAbility.IsDashing()`. 40/s emission while dashing, 0 otherwise. Blue-white particles. |
| `PlayerHurtVFX` | Player (requires HealthSystem) | Subscribes to `OnDamageTaken`. Sprite flash (white) + hit particle burst (red) + screen flash. Supports LayeredSpriteController for multi-sprite flash. |
| `LevelUpVFXController` | Player (requires LevelSystem) | Subscribes to `OnLevelUp`. Sprite-based beam (not particles): outer glow + inner core + base glow. Rise/hold/fade coroutine sequence. Plays audio + screen flash. |
| `PowerUpVFX` | PowerUp pickup | Idle: bobbing + glow pulse + orbiting particles. Collection: radial burst + screen flash. Colors based on PowerUpType (DoubleJump=blue, Dash=gold). |
| `BossVFXController` | Boss (requires BossController) | Subscribes to phase events. Entrance: camera shake + dark fog sweep. Phase change: screen flash + shockwave ring. Enrage: persistent orange-red aura (looping, parented to boss). |
| `BuffAuraVFX` | Player (requires ActiveBuffTracker) | Subscribes to `OnBuffApplied`/`OnBuffExpired`. Creates/destroys looping particle auras per buff. Configs: guard (blue ring), berserk (red rising), war_cry (gold expanding), magic_shield (cyan orbit), evasion (purple). |

### Static Utilities

| Script | API | Purpose |
|---|---|---|
| `SkillVFXFactory` | `SpawnMeleeSweep(pos, facing, dmgType)` | 120-degree arc burst, 15 particles |
| `SkillVFXFactory` | `AttachProjectileTrail(go, dmgType)` | Looping trail parented to projectile, 25/s |
| `SkillVFXFactory` | `SpawnImpactBurst(pos, dmgType)` | Radial burst, 12 particles |
| `SkillVFXFactory` | `SpawnAoECircle(center, radius, dmgType)` | Expanding ring, 25 particles |
| `SkillVFXFactory` | `GetColors(dmgType)` | Returns (primary, accent) color pair per DamageType |
| `MageSkillVFX` | -- | Mage-specific skill particle effects |

`SkillVFXFactory` uses a static cached material (`_particleMaterial`) with `RuntimeInitializeOnLoadMethod` cleanup to avoid leaks across domain reloads.

---

## VFX Zone Pattern

Reusable trigger pattern used by PrecipitationZone, IndoorZone, and applicable to any zone-based VFX:

```csharp
[RequireComponent(typeof(Collider2D))]
public class VFXZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) ActivateVFX();
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) DeactivateVFX();
    }
}
```

IndoorZone extends this by tracking which controllers were disabled so it only re-enables those specific controllers on exit (not all controllers in the scene).

---

## Common Issues

### Particles Not Visible

**Root cause:** Wrong shader or sorting layer.
**Fix:** Verify the material uses `Universal Render Pipeline/Particles/Unlit` (primary) or `Sprites/Default` (fallback). Check `sortingLayerName = "Foreground"` and `sortingOrder` is appropriate. Z position: negative = in front of player at Z=0.

### Particles Spawning Off-Screen

**Root cause:** Spawn area too large or offset too high relative to camera.
**Fix:** Reduce `spawnOffset` in preset or `bounds.spawnHeightAbove` in controller. For FollowCamera mode, check `horizontalPadding` is not excessive.

### Performance Drops

**Root cause:** Too many particles, or expensive per-particle LateUpdate iteration.
**Fix:**
1. Reduce `maxParticles` and `emissionRate` (see limits table in CLAUDE.md)
2. Use GPU motion (`useGPUMotion = true`) instead of CPU per-particle iteration
3. Use larger particles with lower count for the same visual density
4. Ensure particle arrays are cached (`new Particle[]` once, not per frame)

### Material Leak on Domain Reload

**Root cause:** Static material references survive domain reload but point to destroyed objects.
**Fix:** Use `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` to null out static material references. See `SkillVFXFactory.Cleanup()` and `BuffAuraVFX.Cleanup()` for the pattern.

### Flash/Sprite Color Not Restoring

**Root cause:** `PlayerHurtVFX` disabled before flash timer expires.
**Fix:** `OnDisable()` must restore original color. The script handles both `LayeredSpriteController` (multi-sprite) and fallback `SpriteRenderer` paths.
