# ProjectNameHere

2D Metroidvania-style platformer built in Unity 6.

> **Migration Notice:** This project uses **beads (`bd`)** for task tracking. Run `bd ready` to find current work. Legacy markdown task files or TODO lists may be outdated — if beads and a markdown file conflict, **trust beads**.

## Quick Reference

```bash
# Unity Version
Unity 6000.3.4f1

# Build
Unity Editor > File > Build Settings > Build

# Play
Unity Editor > Play button (Ctrl+P)
```

## Architecture Overview

```
Assets/_Project/
├── Scripts/
│   ├── Player/          # PlayerControllerScript - core movement
│   ├── Abilities/       # Dash, DoubleJump, PowerUp system
│   ├── Camera/          # Camera controller, parallax, bounds
│   ├── VFX/             # Fog, particles, atmosphere
│   ├── Audio/           # (placeholder)
│   ├── Systems/         # (placeholder)
│   └── UI/              # (placeholder)
├── Art/                 # Sprites, Animations, Materials, Textures
├── Audio/               # Music, Ambience, SFX
├── Prefabs/             # Player, Abilities, Effects
└── Settings/            # Input, Physics, Rendering
```

## Key Systems

### Player Controller (`Scripts/Player/PlayerControllerScript.cs`)
- Movement with coyote time (0.15s) and jump buffering (0.15s)
- Gravity multiplier on fall (2.5x)
- Max fall speed capped at -20
- Integrates with ability system via GetComponent

### Ability System (`Scripts/Abilities/`)
- **Component-based**: Abilities are MonoBehaviours added dynamically
- **PowerUpPickup.cs**: Handles collection, adds ability components
- **PowerUpManager.cs**: Tracks unlocked abilities via HashSet

Adding a new ability:
1. Create new script inheriting MonoBehaviour in `Scripts/Abilities/`
2. Add to `PowerUpType` enum in `PowerUpPickup.cs`
3. Add case to switch statement in `PowerUpPickup.AddAbilityComponent()`
4. Integrate with PlayerControllerScript as needed

### Camera System (`Scripts/Camera/`)
- **AdvancedCameraController**: Smooth follow, look-ahead, boss room lock
- **ParallaxBackgroundManager**: Manages parallax layers by Z-depth
- **BossRoomTrigger**: Locks camera during boss fights
- **CameraBoundsTrigger**: Progressive area reveals

### VFX System (`Scripts/VFX/`)
- **ParticleFogSystem**: Procedural fog with Perlin noise wind
- **AtmosphericAnimator**: Drift, pulse, rotation for atmosphere objects

## Continuous Integration

Lightweight asset validation runs on every push/PR via GitHub Actions. No Unity Editor or license required — Python scripts parse text-based asset files.

### Run Locally

```bash
python ci/run_all.py
```

### Individual Checks

| Script | What It Validates |
|--------|-------------------|
| `ci/check_meta_files.py` | Every asset has a `.meta`; no orphaned `.meta` files |
| `ci/check_guid_references.py` | No broken GUID references in prefabs/scenes/assets under `_Project/` |
| `ci/check_layer_consistency.py` | Layer, sorting layer, and tag names in C# match `TagManager.asset` |
| `ci/check_scene_build_settings.py` | Build scenes exist on disk with correct GUIDs |
| `ci/run_all.py` | Runs all checks, prints pass/fail summary |

### GitHub Actions

Workflow: `.github/workflows/unity-ci.yml`
- Triggers on push/PR to `main` when `Assets/`, `ProjectSettings/`, `Packages/`, or `ci/` change
- Each check runs as a separate step (shows exactly which failed)
- Zero pip dependencies — Python stdlib only

### Adding a New Check

1. Create `ci/check_<name>.py` with a `main()` that returns 0 (pass) or 1 (fail)
2. Use `os.environ.get("GITHUB_ACTIONS")` to emit `::error` / `::warning` annotations
3. Add entry to `CHECKS` list in `ci/run_all.py`
4. Add step to `.github/workflows/unity-ci.yml`

## Coding Conventions

### Naming
- Classes: `PascalCase` (e.g., `PlayerControllerScript`)
- Fields/Methods: `camelCase` (e.g., `jumpBufferCounter`)
- Private fields: explicit `private` keyword

### Organization
- Use `[Header("Section")]` for inspector grouping
- Use `[SerializeField]` for tweakable values
- Cache component references in `Awake()` or `Start()`

### Physics
- Movement logic in `FixedUpdate()`
- Input reading in `Update()`
- Use `Rigidbody2D.linearVelocity` (Unity 6 API)
- Ground detection via `Physics2D.OverlapCircleAll`

### Input
- Uses Unity InputSystem (not legacy)
- Callbacks via `InputAction.CallbackContext`
- Double-tap detection window: 0.3s

## Task Tracking (Beads)

This project uses [Beads](https://github.com/steveyegge/beads) (`bd`) for persistent, git-backed issue tracking across agent sessions.

Read `AGENTS.md` first for the complete workflow reference.

### Core Commands

```bash
bd ready                              # See unblocked tasks
bd show <id>                          # View task details and history
bd create "Title" -p <0-3>            # Create task (0=critical, 3=low)
bd update <id> --claim                # Claim task (sets assignee + in_progress)
bd update <id> --status in_progress   # Mark work started
bd close <id> --reason "what was done" # Complete task
bd dep add <child> <parent>           # Link dependency
bd dep tree                           # Visualize task hierarchy
bd sync                               # Flush to git
```

### Session Protocol

**Start of session:**
1. Run `bd ready` to see available work
2. Claim a task before starting: `bd update <id> --claim`
3. Review task details: `bd show <id>`

**During work:**
- Create subtasks for discovered work: `bd create "Subtask" -p 2`
- Link dependencies: `bd dep add <child> <parent>`
- Update status as work progresses

**End of session:**
1. File issues for remaining/discovered work: `bd create ...`
2. Close finished tasks: `bd close <id> --reason "..."`
3. Sync: `bd sync`

### Priority Levels

| Priority | Use For |
|----------|---------|
| 0 (Critical) | Blocking bugs, broken builds |
| 1 (High) | Current sprint features, important fixes |
| 2 (Normal) | Planned work, enhancements |
| 3 (Low) | Nice-to-have, future ideas, minor polish |

## Workflow Standards

### RPI Pattern (Research, Plan, Implement)
1. **Research** - Explore codebase, understand existing patterns before changes. Run `bd ready` to check for related open tasks.
2. **Plan** - Design approach, identify impacts, get user approval for non-trivial work. Create bd tasks for multi-step plans.
3. **Implement** - Write code following established conventions. Update bd task status as you go.

### Progressive Disclosure
- Present essential information first
- Reveal complexity only when needed
- Layer responses: summary → specifics → edge cases

### Prefabrication
- All reusable GameObjects must be prefabs
- Scene contains prefab instances, not embedded objects
- Edit in Prefab Mode to avoid scene conflicts
- See `Assets/_Project/Prefabs/` for structure

### Continuous Integration
- Verify existing functionality before and after changes
- Test edge cases (ability combos, state transitions, null refs)
- Ground detection must exclude triggers (`!collider.isTrigger`)

## Important Constants

| Constant | Value | Location |
|----------|-------|----------|
| Coyote Time | 0.15s | PlayerControllerScript |
| Jump Buffer | 0.15s | PlayerControllerScript |
| Fall Gravity Mult | 2.5x | PlayerControllerScript |
| Max Fall Speed | -20 | PlayerControllerScript |
| Dash Duration | 0.2s | DashAbility |
| Dash Cooldown | 1.0s | DashAbility |
| Dash Speed | 20 | DashAbility |

## Packages

- **Input System** 1.17.0
- **Universal RP** 17.3.0
- **Cinemachine** 3.1.5
- 2D: Sprites, Tilemap, Animation, SpriteShape, Aseprite

## Scene Structure

Main scene: `Assets/Scenes/SampleScene.unity`

## Common Tasks

### Add a new parallax layer
1. Create sprite GameObject
2. Set Z position (negative = background, positive = foreground)
3. Add to ParallaxBackgroundManager's layers array

### Create boss room
1. Add trigger collider to room entrance
2. Attach `BossRoomTrigger` component
3. Set room bounds and center position
4. Assign boss reference for auto-unlock on defeat

### Debug camera bounds
- Enable Gizmos in Scene view
- CameraBoundsTrigger draws bounds visualization
