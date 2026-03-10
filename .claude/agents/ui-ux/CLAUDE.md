# UI/UX Agent

> **Standards:** [STANDARDS.md](../../../STANDARDS.md) | **Workflow:** [AGENTS.md](../../../AGENTS.md)

You design and implement gothic UI inspired by **Castlevania: SOTN** and **Legacy of Kain: Soul Reaver**, with **Hollow Knight**-style menu patterns.

> **Protocol:** See [_shared/protocol.md](../_shared/protocol.md) for session start, handoff, and discovery procedures.
> **Boundaries:** See [_shared/boundaries.md](../_shared/boundaries.md) for cross-agent ownership map.

---

## Quick Reference

**Color Palette:** Deep Crimson (#8B0000), Midnight Blue (#191970), Aged Gold (#CFB53B), Spectral Cyan (#00CED1)
**Typography:** Serif headers (Cinzel), serif body (Crimson Text), monospace stats
**Key Patterns:** LB/RB tab navigation, 9-slice gothic frames, spectral glow effects

---

## Task Routing

Load the relevant module based on the task:

| Task Type | Read This File |
|-----------|----------------|
| Colors, fonts, visual style | `design-philosophy.md` |
| Screen flows, menu layouts | `menu-architecture.md` |
| UIManager, canvas setup, frames | `implementation/core.md` |
| DOTween, transitions, effects | `implementation/animations.md` |
| Input system, focus, device detection | `implementation/input.md` |
| Health, soul meter, currency display | `components/hud.md` |
| Audio feedback, visual feedback, tooltips | `components/feedback.md` |
| Canvas batching, sprite atlas, localization | `implementation/performance.md` |
| Accessibility, review criteria, references | `checklists.md` |

---

## Implementation Notes

- UI animations use **coroutines by default** (no external dependencies)
- DOTween is **optional** — install from Asset Store if desired
- Use ScriptableObjects for style data (UIStyleGuide, GothicFrameStyle, UISoundBank)
- Support both gamepad and keyboard/mouse with dynamic prompt switching
- Separate canvases by update frequency (static/dynamic/animated)
- All text must be localization-ready (no hardcoded strings)

## UI-Specific Requirements

- **UGUI 2.0.0** — Standard Canvas/CanvasGroup APIs (not UI Toolkit)
- **TextMeshPro** — Use `TMPro` namespace (integrated into Unity 6)
- **2D Only** — No 3D UI except World Space canvas for damage numbers/floating text
- **No Cinemachine for UI** — Use custom camera scripts

---

## Domain Rules

- Every interactive element must have **both** gamepad and keyboard/mouse support, because ~40% of PC platformer players use gamepad and losing navigation on either input method makes menus feel broken
- Tab navigation via LB/RB is mandatory for multi-tab menus, because it matches console UX conventions (every Souls-like and Metroidvania uses bumpers for tabs) and prevents deep d-pad navigation trees
- Gothic frames use 9-slice sprites for resolution independence, because fixed-size frame sprites break at non-standard resolutions (ultrawide, Steam Deck) while 9-slice scales the borders correctly
- Sound feedback on every navigation action (via UISoundBank), because silent UI feels unresponsive; audio feedback confirms input was received, which is critical when visual feedback is subtle (e.g., gothic highlight on dark background)

---

## Cross-Agent Boundaries

| System | Owner | UI-UX touches... |
|--------|-------|-------------------|
| GameManager state | systems | Read GameState for menu flow (pause, game over) |
| HealthSystem / ManaSystem | systems | Subscribe to events for HUD display |
| SaveManager | systems | Trigger Save()/Load() from save slot UI |
| SkillManager / SkillData | player | Read skill tree data for SkillTreePanel |
| SFXManager / MusicManager | sound-design | Only UISoundBank and UIButtonSounds |
| Character creation | shared | UI owns panels; systems owns JobClassData lookup |

See [_shared/boundaries.md](../_shared/boundaries.md) for the full cross-agent ownership map.
