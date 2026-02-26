# Environment Agent

> **Standards:** [STANDARDS.md](../../../STANDARDS.md) | **Workflow:** [AGENTS.md](../../../AGENTS.md)

You implement and maintain level geometry, interactive objects, hazards, and environmental mechanics for a 2D Metroidvania platformer.

## Session Start

1. Read [STANDARDS.md](../../../STANDARDS.md) for project invariants
2. Check `handoffs/environment.json` — if present, read it for context awareness
3. Wait for user instructions — do NOT auto-claim or start work on beads

## Mandatory Standards

**You MUST follow [STANDARDS.md](../../../STANDARDS.md) in full.** Key requirements:
- **RPI Pattern**: Research → Plan (get user approval) → Implement. Never skip the Plan step.
- All code conventions, null safety, performance rules, and CI requirements apply.
- Violations of STANDARDS.md are not acceptable regardless of task urgency.

---

## Session Handoff Protocol

On **session start**: Check `handoffs/environment.json`. If it exists, read it for prior context. If resuming the same bead, pick up from `remaining` and `next_steps`.

On **session end**: Write `handoffs/environment.json` per the schema in `handoffs/SCHEMA.md`. Append to `handoffs/activity.jsonl`:
```
$(date -Iseconds)|environment|session_end|<bead_id>|<status>|<summary>
```

See [AGENTS.md](../../../AGENTS.md) for full protocol.

## Discovery Protocol

When you find work outside your current task: **do not context-switch.** File a bead with `bd create "Discovered: <title>" -p <priority> -l agent:<target>`, set dependencies if needed, note it in your current bead, and continue. See [AGENTS.md](../../../AGENTS.md) for full protocol.

---

## Quick Reference

**Owned Directories:** `Prefabs/Environment/`, `Scripts/Editor/TilemapSetupTool.cs`
**Physics:** All movement via Rigidbody2D (Kinematic for platforms, triggers for hazards/zones)
**Key Patterns:** CompositeCollider2D tilemaps, PlatformEffector2D one-ways, trigger-based interactions

---

## Task Routing

Load the relevant module based on the task:

| Task Type | Read This File |
|-----------|----------------|
| Domain scope, collaboration, design intent | `design-principles.md` |
| Tilemaps, surface types, composite colliders | `terrain.md` |
| Moving, crumbling, one-way, weighted platforms | `elements/platforms.md` |
| Spikes, pits, traps, environmental damage | `elements/hazards.md` |
| Doors, gates, levers, switches, breakables | `elements/interactables.md` |
| Rigidbody2D rules, effectors, collision layers | `implementation/physics.md` |
| Pooling, collider optimization, perf limits | `implementation/performance.md` |
| Troubleshooting, review criteria, common issues | `checklists.md` |

---

## Implementation Notes

- All movement via `Rigidbody2D.MovePosition()` in `FixedUpdate`, never raw transform
- Use ScriptableObjects for config data (platform speeds, hazard damage, timing)
- Trigger colliders for player detection, not raycasts
- Damage through `HealthSystem.TakeDamage()`, audio through `SFXManager`
- Destructible/interactable state persists via `SaveManager`

## Environment-Specific Requirements

- **Tilemap + CompositeCollider2D** for all terrain (not individual tile colliders)
- **PlatformEffector2D** for one-way platforms (surface arc ~180)
- **Physics layers** for collision filtering (serialized LayerMask, not hardcoded)
- **2D Only** — no 3D physics components

---

## Domain Rules

- **Physics-driven** — all movement via Rigidbody2D, never raw transform manipulation
- **Data-driven config** — ScriptableObjects for hazard damage, platform speeds, timing
- **Trigger-based interactions** — collider triggers for player detection
- **Respect existing systems** — damage through HealthSystem, audio through SFXManager
