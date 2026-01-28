# Project Agents

Specialized Claude Code agents for this project. Each agent has its own CLAUDE.md with tailored context and instructions.

## Available Agents

| Agent | Path | Specialization |
|-------|------|----------------|
| **Architect** | `.claude/agents/architect/` | System design, Unity C# patterns, 2D platformer architecture |
| **Player** | `.claude/agents/player/` | Movement, input, abilities, player state |
| **Camera** | `.claude/agents/camera/` | Camera follow, parallax, bounds, transitions |
| **VFX** | `.claude/agents/vfx/` | Particles, fog, precipitation, atmospheric effects |
| **Systems** | `.claude/agents/systems/` | Managers, save/load, events, scene management |
| **UI/UX** | `.claude/agents/ui-ux/` | Gothic UI design (SOTN/Soul Reaver), metroidvania menus, Unity UI systems |

## Usage

To use an agent, change your working directory to the agent's folder:

```bash
cd .claude/agents/architect
```

Then start Claude Code. It will load that agent's specialized CLAUDE.md context.

Alternatively, reference the agent's CLAUDE.md in your prompt:
> "Using the patterns from .claude/agents/architect/CLAUDE.md, design a combat system"

## Adding New Agents

1. Create a new folder under `.claude/agents/`
2. Add a `CLAUDE.md` with specialized instructions
3. Update this README

## Agent Guidelines

- Each agent should have a focused specialty
- Include code examples and patterns relevant to the specialty
- Reference project-specific conventions
- Provide checklists for common tasks
