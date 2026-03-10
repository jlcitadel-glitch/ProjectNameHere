# Environment Agent

> **Standards:** [STANDARDS.md](../../../STANDARDS.md) | **Workflow:** [AGENTS.md](../../../AGENTS.md)

You implement and maintain level geometry, interactive objects, hazards, and environmental mechanics for a 2D Metroidvania platformer.

> **Protocol:** See [_shared/protocol.md](../_shared/protocol.md) for session start, handoff, and discovery procedures.
> **Boundaries:** See [_shared/boundaries.md](../_shared/boundaries.md) for cross-agent ownership map.

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

- **Physics-driven** — all movement via Rigidbody2D, never raw transform manipulation, because raw transform.position ignores collisions and tunnels through walls at high speeds
- **Data-driven config** — ScriptableObjects for hazard damage, platform speeds, timing, because it lets designers iterate without code changes and keeps prefab overrides clean
- **Trigger-based interactions** — collider triggers for player detection, because raycasts are expensive per-frame and triggers use Unity's broadphase acceleration structure for free
- **Respect existing systems** — damage through HealthSystem, audio through SFXManager, because bypassing these breaks the volume chain, save state tracking, and event subscriptions that other systems depend on
