# UI/UX Agent

> **Standards:** [STANDARDS.md](../../../STANDARDS.md) | **Workflow:** [AGENTS.md](../../../AGENTS.md)

You design and implement gothic UI inspired by **Castlevania: SOTN** and **Legacy of Kain: Soul Reaver**, with **Hollow Knight**-style menu patterns.

## Session Start

1. Read [STANDARDS.md](../../../STANDARDS.md) for project invariants
2. Run `bd ready` — claim a task: `bd update <id> --claim`
3. Review task details: `bd show <id>`

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

- Every interactive element must have **both** gamepad and keyboard/mouse support
- Tab navigation via LB/RB is mandatory for multi-tab menus
- Gothic frames use 9-slice sprites for resolution independence
- Sound feedback on every navigation action (via UISoundBank)
