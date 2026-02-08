# ProjectNameHere

2D Metroidvania-style platformer built in Unity 6.

> **Standards:** [STANDARDS.md](STANDARDS.md) — Universal invariants (Unity 6 APIs, coding conventions, CI, patterns)
> **Workflow:** [AGENTS.md](AGENTS.md) — Beads task tracking protocol
> **Task Tracking:** Run `bd ready` to find current work. If beads and a legacy markdown file conflict, **trust beads**.

## Quick Reference

```bash
# Unity Version
Unity 6000.3.4f1

# Build
Unity Editor > File > Build Settings > Build

# Play
Unity Editor > Play button (Ctrl+P)

# CI (local)
python ci/run_all.py
```

## Architecture Overview

```
Assets/_Project/
├── Scripts/
│   ├── Player/          # PlayerControllerScript - core movement
│   ├── Abilities/       # Dash, DoubleJump, PowerUp system
│   ├── Camera/          # Camera controller, parallax, bounds
│   ├── Enemies/         # AI state machines, combat, sensors, spawning
│   ├── VFX/             # Fog, particles, precipitation, atmosphere
│   ├── Audio/           # SFXManager, MusicManager
│   ├── Systems/         # GameManager, SaveManager, WindManager
│   └── UI/              # UIManager, menus, HUD
├── Art/                 # Sprites, Animations, Materials, Textures
├── Audio/               # Music, Ambience, SFX
├── Prefabs/             # Player, Abilities, Effects, Enemies, UI
└── Settings/            # Input, Physics, Rendering
```

## Key Documents

| Document | Purpose |
|----------|---------|
| [STANDARDS.md](STANDARDS.md) | Unity 6 API rules, code organization, null safety, prefab workflow, RPI pattern, CI, performance |
| [AGENTS.md](AGENTS.md) | Beads workflow, session protocol, priority levels, landing protocol |
| `.claude/agents/*/CLAUDE.md` | Per-agent domain expertise, owned scripts, common issues |

## Packages

- **Input System** 1.17.0
- **Universal RP** 17.3.0
- **Cinemachine** 3.1.5
- 2D: Sprites, Tilemap, Animation, SpriteShape, Aseprite

## Scene Structure

- Main menu: `Assets/Scenes/MainMenu.unity`
- Gameplay: `Assets/Scenes/SampleScene.unity`

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
