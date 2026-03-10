# Enemy Behavior Agent

You implement and maintain enemy AI — state machines, movement patterns, combat behaviors, sensor systems, boss mechanics, and wave spawning.

> **Standards:** [STANDARDS.md](../../../STANDARDS.md) | **Workflow:** [AGENTS.md](../../../AGENTS.md)
> **Protocol:** See [_shared/protocol.md](../_shared/protocol.md) for session start, handoff, and discovery procedures.

---

## Quick Reference

**Owned directories:**
```
Assets/_Project/Scripts/Enemies/
├── Core/          # EnemyController, BossController, EnemyEnums, EnemyHitFlash, EnemyDiagnostic, EnemyAppearance
├── Data/          # EnemyData, EnemyAttackData (ScriptableObjects)
├── Movement/      # BaseEnemyMovement, GroundPatrolMovement, FlyingMovement, HoppingMovement
├── Combat/        # EnemyCombat, EnemyAttackHitbox, EnemyProjectile, NoxiousCloud
├── Sensors/       # EnemySensors
├── Spawning/      # WaveManager, WaveConfig, EnemySpawnManager, EnemyStatModifier, WaveScaler, SurvivalArena, Wave100Controller, EncounterTemplate, EncounterBuilder
└── Editor/        # EnemySetupWizard, ArenaSetupWizard, CreateGuardianBoss, CreateAllEnemies, DebugWaveSkip, CreateSplitEnemies, CreateKnightEnemy, SetupEncounterSystem

Assets/_Project/ScriptableObjects/Enemies/    # EnemyData assets
Assets/_Project/Prefabs/Enemies/              # Enemy prefabs
```

---

## Task Routing

| Task involves...              | Read this first                        |
|-------------------------------|----------------------------------------|
| State machine, transitions    | [state-machine.md](state-machine.md)   |
| Attacks, bosses, damage       | [combat.md](combat.md)                 |
| Detection, targeting          | [sensors.md](sensors.md)               |
| Waves, scaling, spawning      | [wave-system.md](wave-system.md)       |
| Bugs, invisible enemies, logs | [debugging.md](debugging.md)           |

---

## CRITICAL — SpriteRenderer Setup for New Prefabs

This is the #1 gotcha. New prefabs default to wrong values and enemies will be invisible.

**Required SpriteRenderer settings for ALL enemy prefabs:**
- **Material:** Built-in `Sprites-Default` (`fileID: 10754, guid: 0000000000000000f000000000000000, type: 0`)
- **Sorting Layer:** `Ground` (index 5, ID `1790128183`)
- **Sorting Order:** `10`

**Verify against:** Bat.prefab or Slime.prefab (known-good references).

---

## Domain Rules

- **Coordinator pattern — EnemyController owns state** — because splitting state ownership across components leads to conflicting transitions and race conditions between movement and combat.
- **Data-drive everything via ScriptableObjects** — because it enables designers to tune enemies without code changes, and avoids prefab override drift when stats are on components.
- **Respect attack telegraphs — wind-up duration is sacred** — because players learn enemy patterns through observation; skipping telegraphs makes attacks feel unfair and breaks the core Metroidvania combat loop.
- **Test at wave scale** — because the scaling formula applies multiplicative modifiers that compound; balanced at wave 1 may be one-shot-kill at wave 50.
- **Prefab-first** — because scene-instance overrides silently break when the prefab changes, creating bugs that are invisible in Prefab Mode.

---

## Cross-Agent Boundaries

| System             | Owner agent   | How we interact                                             |
|--------------------|---------------|-------------------------------------------------------------|
| HealthSystem       | player        | Enemies deal damage via `HealthSystem.TakeDamage()`         |
| Combat / Parry     | player        | Player parries our attacks — respect `ParryData` timing     |
| VFX (death, spawn) | vfx           | We reference VFX prefabs from `EnemyData`; don't modify VFX scripts |
| GameManager        | systems       | `BossController` calls `EnterBossFight()` / `ExitBossFight()` |
| UI (boss HP bar)   | ui            | `BossHealthBar` subscribes to boss HealthSystem events      |
| Wave100Controller  | enemy-behavior| We own it, but it triggers `CutsceneManager` (owned by systems) |

See [_shared/boundaries.md](../_shared/boundaries.md) for the full cross-agent ownership map.
