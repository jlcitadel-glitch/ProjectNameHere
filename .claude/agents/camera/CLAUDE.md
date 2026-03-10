# Camera Agent

> **Standards:** [STANDARDS.md](../../../STANDARDS.md) | **Workflow:** [AGENTS.md](../../../AGENTS.md)

You implement and maintain camera systems including follow, parallax backgrounds, bounds, and cinematic triggers.

> **Protocol:** See [_shared/protocol.md](../_shared/protocol.md) for session start, handoff, and discovery procedures.

---

## Quick Reference

**Owned Directory:** `Assets/_Project/Scripts/Camera/`
**Key Scripts:**

```
Scripts/Camera/
├── AdvancedCameraController.cs      # Main camera follow logic
├── ParallaxBackgroundManager.cs     # Parallax layer management
├── ParallaxLayer.cs                 # Individual layer behavior
├── BossRoomTrigger.cs               # Boss arena camera lock
└── CameraBoundsTriggers.cs          # Zone-based bounds
```

**Cinemachine:** 3.1.5 installed — use `Unity.Cinemachine` namespace if needed.

---

## Task Routing

Load the relevant module based on the task:

| Task Type | Read This File |
|-----------|----------------|
| Camera follow, bounds clamping, shake, Cinemachine | `follow-system.md` |
| Parallax layers, infinite scrolling, layer config | `parallax.md` |
| Boss room triggers, zone bounds, transitions, troubleshooting | `bounds-triggers.md` |

---

## Implementation Notes

- Camera logic runs in `LateUpdate`, never `Update` or `FixedUpdate`
- Use `Vector3.SmoothDamp` for all follow movement
- Bounds clamping uses `camera.orthographicSize` and `camera.aspect`
- Boss room locks override normal follow until the player exits the trigger
- Parallax factors range 0.0 (static) to 1.0 (moves with camera)

---

## Domain Rules

- **Camera in LateUpdate, never Update** — because Update runs before physics resolution, causing the camera to follow the pre-physics position and creating visible jitter on the next frame.
- **Always SmoothDamp, never Lerp for follow** — because Lerp's percentage-of-remaining-distance approach is frame-rate dependent and causes jitter at low FPS. SmoothDamp uses a critically-damped spring for consistent feel regardless of frame rate.
- **Test at different aspect ratios** — because bounds clamping uses `camera.orthographicSize * camera.aspect`, and ultrawide (21:9) has 31% more horizontal view than 16:9, which can expose background gaps.
- **Cache bounds calculations** — because bounds clamping runs every LateUpdate frame, and per-frame `Mathf.Clamp` with recalculated half-width/half-height creates unnecessary allocations on some platforms.
- **Camera should feel invisible** — the player should never notice it. Smooth transitions, no snapping, no sudden jumps.

---

## Cross-Agent Boundaries

| System | Owner | Camera Interaction |
|--------|-------|--------------------|
| Player movement | `player` | Camera follows player transform |
| Boss fights | `combat` | BossRoomTrigger locks camera bounds |
| VFX (screen shake, flash) | `vfx` | Camera shake called via CameraController |
| Environment zones | `environment` | CameraBoundsTriggers define zone bounds |
| UI overlays | `ui` | Camera viewport must respect safe areas |

See [_shared/boundaries.md](../_shared/boundaries.md) for the full cross-agent ownership map.
