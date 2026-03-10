# Wave System

## Scaling Formulas

### Waves 1-100

```
HP:     baseStat * (1 + (wave - 1) * 0.15)
Damage: baseStat * (1 + (wave - 1) * 0.10)
Speed:  baseStat * (1 + (wave - 1) * 0.05)
```

At wave 100: HP is 15.85x base, Damage is 10.9x base, Speed is 5.95x base.

### Waves 101+ (post-milestone acceleration — rates doubled)

```
HP:     baseStat * (1 + 99 * 0.15 + (wave - 100) * 0.30)
Damage: baseStat * (1 + 99 * 0.10 + (wave - 100) * 0.20)
Speed:  baseStat * (1 + 99 * 0.05 + (wave - 100) * 0.10)
```

The doubled rates mean post-100 waves escalate rapidly. A wave-150 enemy has roughly 2x the stats of a wave-100 enemy.

### Enemy Count

```
Count: Min(baseCount + (wave - 1) * increasePerWave, maxAliveAtOnce)
```

`maxAliveAtOnce` caps simultaneous enemies to prevent performance issues and overwhelming the player.

### Boss Waves

Every `bossWaveInterval` waves, the wave spawns a boss enemy instead of (or in addition to) regular enemies. Configured in `WaveConfig`.

### Wave 100 Milestone

`Wave100Controller` handles the special wave-100 sequence:
1. Cutscene trigger (via `CutsceneManager`)
2. Special boss spawn
3. Credits sequence on completion

## Related Scripts

| Script               | Role                                                          |
|----------------------|---------------------------------------------------------------|
| `WaveManager`        | Wave state machine — idle, spawning, active, intermission     |
| `WaveConfig`         | ScriptableObject — enemy pool, scaling params, boss config    |
| `EnemySpawnManager`  | Instantiation, active enemy tracking, death event relay       |
| `EnemyStatModifier`  | Applies wave-based stat multipliers to spawned enemies        |
| `WaveScaler`         | Contains the scaling formulas (1-100 and 101+ acceleration)   |
| `SurvivalArena`      | Arena-specific setup (spawn points, boundaries, camera locks) |
| `Wave100Controller`  | Wave 100 milestone cutscene + boss + credits sequence         |
| `EncounterTemplate`  | ScriptableObject defining a reusable encounter configuration  |
| `EncounterBuilder`   | Constructs encounters from templates at runtime               |

## Architecture Notes

- `WaveManager` drives the loop: it advances waves, tells `EnemySpawnManager` what to spawn, and listens for all-dead to start intermission.
- `EnemyStatModifier` is applied to each enemy immediately after instantiation by `EnemySpawnManager`. It reads the current wave number from `WaveManager` and applies `WaveScaler` formulas.
- `WaveConfig` is a ScriptableObject so designers can create different wave profiles (easy/hard/endless) without code changes.
