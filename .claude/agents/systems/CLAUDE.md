# Systems Agent

You implement and maintain core game systems — managers, global services, save/load, events, and cross-cutting concerns.

> **Standards:** [STANDARDS.md](../../../STANDARDS.md) | **Workflow:** [AGENTS.md](../../../AGENTS.md)
> **Protocol:** See [_shared/protocol.md](../_shared/protocol.md) for session start, handoff, and discovery procedures.

---

## Owned Scripts

`Assets/_Project/Scripts/Systems/` — subdirectories: `Core/`, `Cutscene/`, `Editor/`.
See sub-files ([managers.md](managers.md), [save-system.md](save-system.md), [boot-sequence.md](boot-sequence.md)) for full script listings.

---

## Task Routing

| Topic | Sub-file |
|-------|----------|
| GameManager, singletons, WindManager | [managers.md](managers.md) |
| SaveManager, save slots, data versioning | [save-system.md](save-system.md) |
| C# events, SO event channels, subscriptions | [events.md](events.md) |
| Bootstrap, init order, DontDestroyOnLoad | [boot-sequence.md](boot-sequence.md) |

---

## CRITICAL: Boot Sequence Gotcha

**SystemsBootstrap.cs EXISTS but is NOT placed in any scene.** MainMenuController bootstraps managers via `EnsureXManager()` methods instead. See [boot-sequence.md](boot-sequence.md) for the full init order and risks.

---

## Domain Rules

- **Design for decoupling via events/interfaces** — because game systems change frequently during development; tight coupling means a change to HealthSystem ripples into every enemy, every UI element, and every VFX that reads health.
- **Handle persistence deliberately** — because DontDestroyOnLoad objects persist through scene loads but their event subscriptions to scene objects break; forgetting to re-subscribe after scene load is the #1 cause of "feature works once then stops."
- **Document initialization order** — because managers depend on each other (SaveManager needs GameManager, UI needs both); undocumented init order causes null reference races that only manifest on slow hardware or specific load orders.
- **Cross-agent impact** — because new managers affect the entire game; adding a manager without filing integration tasks via `bd create` leaves other agents unaware of new APIs they should use.

---

## Cross-Agent Boundaries

> See also: [_shared/boundaries.md](../_shared/boundaries.md) for the full cross-agent boundary map.

| System | Owner | Coordinate when... |
|--------|-------|--------------------|
| Player stats/health/mana | **systems** owns HealthSystem, ManaSystem, StatSystem | UI agent reads these for HUD display |
| Combat damage dealing | **combat** agent | Combat calls HealthSystem.TakeDamage() |
| Skill effects (buffs, heals) | **skills** agent | Skills call StatSystem, HealthSystem, ManaSystem |
| Enemy death/XP | **enemies** agent | Enemies spawn ExperienceOrbs on death |
| Save/load UI | **ui** agent | UI triggers SaveManager.Save()/Load() |
| VFX wind integration | **vfx** agent | VFX reads WindManager.CurrentWindVector |
| Scene transitions | **all agents** | SceneLoader affects everyone |
