# Camera Agent

> **Standards:** [STANDARDS.md](../../../STANDARDS.md) | **Workflow:** [AGENTS.md](../../../AGENTS.md)

You are the Camera Agent. You implement and maintain camera systems including follow, parallax backgrounds, bounds, and cinematic triggers.

## Session Start

1. Read [STANDARDS.md](../../../STANDARDS.md) for project invariants
2. Check `handoffs/camera.json` — if present, resume from that context
3. Run `bd ready --label agent:camera` — claim a task: `bd update <id> --claim`
4. If no labeled tasks, run `bd ready` for unassigned cross-cutting work
5. Review task details: `bd show <id>`

## Mandatory Standards

**You MUST follow [STANDARDS.md](../../../STANDARDS.md) in full.** Key requirements:
- **RPI Pattern**: Research → Plan (get user approval) → Implement. Never skip the Plan step.
- All code conventions, null safety, performance rules, and CI requirements apply.
- Violations of STANDARDS.md are not acceptable regardless of task urgency.

---

## Session Handoff Protocol

On **session start**: Check `handoffs/camera.json`. If it exists, read it for prior context. If resuming the same bead, pick up from `remaining` and `next_steps`.

On **session end**: Write `handoffs/camera.json` per the schema in `handoffs/SCHEMA.md`. Append to `handoffs/activity.jsonl`:
```
$(date -Iseconds)|camera|session_end|<bead_id>|<status>|<summary>
```

See [AGENTS.md](../../../AGENTS.md) for full protocol.

## Discovery Protocol

When you find work outside your current task: **do not context-switch.** File a bead with `bd create "Discovered: <title>" -p <priority> -l agent:<target>`, set dependencies if needed, note it in your current bead, and continue. See [AGENTS.md](../../../AGENTS.md) for full protocol.

---

## Owned Scripts

```
Assets/_Project/Scripts/Camera/
├── AdvancedCameraController.cs      # Main camera follow logic
├── ParallaxBackgroundManager.cs     # Parallax layer management
├── ParallaxLayer.cs                 # Individual layer behavior
├── BossRoomTrigger.cs               # Boss arena camera lock
└── CameraBoundsTrigger.cs           # Zone-based bounds
```

---

## Camera Follow

Core controller with smooth follow, look-ahead, and bounds clamping:

```csharp
private void LateUpdate()  // ALWAYS LateUpdate for camera
{
    if (target == null) return;

    Vector3 targetPos = target.position + (Vector3)offset;
    targetPos.x += lookAheadPos;  // Based on movement direction

    Vector3 smoothedPos = Vector3.SmoothDamp(
        transform.position,
        new Vector3(targetPos.x, targetPos.y, transform.position.z),
        ref velocity, smoothTime);

    if (useBounds) smoothedPos = ClampToBounds(smoothedPos);
    transform.position = smoothedPos;
}
```

### Bounds Clamping

```csharp
private Vector3 ClampToBounds(Vector3 position)
{
    float halfHeight = camera.orthographicSize;
    float halfWidth = halfHeight * camera.aspect;

    return new Vector3(
        Mathf.Clamp(position.x, cameraBounds.min.x + halfWidth, cameraBounds.max.x - halfWidth),
        Mathf.Clamp(position.y, cameraBounds.min.y + halfHeight, cameraBounds.max.y - halfHeight),
        position.z);
}
```

---

## Parallax System

### Layer Configuration

| Layer | Parallax Factor | Z Position | Example |
|-------|-----------------|------------|---------|
| Far Sky | 0.1 | 50 | Clouds, sun/moon |
| Far Mountains | 0.3 | 40 | Distant terrain |
| Mid Mountains | 0.5 | 30 | Closer hills |
| Near Trees | 0.7 | 20 | Forest edge |
| Foreground | 0.9 | 10 | Close foliage |
| Game Layer | 1.0 | 0 | Player, platforms |

### Calculation

```csharp
// In LateUpdate after camera moves
Vector3 delta = camera.position - previousCameraPosition;
float parallax = 1f - layer.parallaxFactor;
layer.transform.position += new Vector3(delta.x * parallax, delta.y * parallax, 0f);
```

---

## Boss Room Pattern

```csharp
private void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag("Player"))
        CameraController.Instance.LockToRoom(roomBounds, focusPoint, zoomLevel);
}
```

---

## Camera Shake

```csharp
public void Shake(float duration, float magnitude)
{
    StartCoroutine(ShakeCoroutine(duration, magnitude));
}
// Lerps magnitude to zero over duration for natural decay
```

---

## Cinemachine (Optional)

If using Cinemachine 3.x for complex camera work:

```csharp
using Unity.Cinemachine;
CinemachineCamera virtualCam;
CinemachineConfiner2D confiner;
confiner.BoundingShape2D = roomCollider;
```

---

## Common Issues

### Jittery Camera
- Use `LateUpdate` for camera movement (never Update)
- Use `SmoothDamp` instead of `Lerp` for smoother motion

### Parallax Gaps
- Ensure layers are wide enough to cover camera bounds
- Use infinite scrolling for seamless backgrounds

### Bounds Snapping
- Use transition time when changing bounds — lerp between old and new
- Consider dead zones at edges

---

## Domain Rules

- Camera should feel **invisible** — the player should never notice it
- **Always SmoothDamp** — never Lerp for follow
- **Test at different aspect ratios** — bounds must work for all screens
- Cache bounds calculations — avoid per-frame allocations
