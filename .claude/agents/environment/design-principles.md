# Design Principles

> **Unity 6 2D** - All environment objects use Rigidbody2D physics and 2D colliders.

## Domain Scope

| Owns | Does NOT Own |
|------|-------------|
| Tilemaps, terrain rules, surface types | Camera behavior (camera agent) |
| Moving/crumbling/one-way platforms | Weather/fog/particles (vfx agent) |
| Hazards: spikes, pits, traps | Enemy placement/AI (enemy-behavior agent) |
| Doors, gates, levers, switches | Player movement/abilities (player agent) |
| Breakable/destructible walls | Save/load checkpoint logic (systems agent) |
| Area transition triggers | Background parallax (camera agent) |
| Collectible pedestals, item holders | Audio playback (sound-design agent) |

## Owned Scripts

```
Assets/_Project/Scripts/Environment/
├── Platforms/           # Moving, one-way, crumbling, weighted platforms
├── Hazards/             # Spikes, pits, projectile traps, environmental damage
├── Interactables/       # Doors, gates, levers, switches, breakable walls
├── Terrain/             # Tilemap rules, terrain data, surface types
└── Zones/               # Trigger zones for area transitions, checkpoints

Assets/_Project/Prefabs/Environment/   # All environment prefabs
Assets/_Project/ScriptableObjects/Environment/  # Config data assets
```

## Collaboration Points

| System | How Environment Integrates |
|--------|---------------------------|
| **Player** | Platform parenting, hazard damage, interact input |
| **VFX** | Request effects for breakables, hazards, transitions |
| **Sound** | Surface types drive footstep SFX, interactable feedback |
| **Camera** | Area transitions can trigger camera bound changes |
| **Systems** | Destructible state saved/loaded via SaveManager |

## Metroidvania Environment Patterns

Environments in a Metroidvania serve as both obstacles and progression gates:

- **Ability gates** — areas blocked until the player unlocks a specific ability (dash through crumbling walls, double-jump to high ledges)
- **Shortcut loops** — one-way doors/gates that open from the far side, creating fast travel within a region
- **Environmental storytelling** — destructible/interactive objects hint at lore or hidden paths
- **Risk/reward placement** — hazards guard valuable pickups or secret areas
