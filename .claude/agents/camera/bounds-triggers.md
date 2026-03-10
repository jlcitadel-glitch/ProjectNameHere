# Bounds and Triggers

Reference for boss room triggers, camera bounds zones, transitions, and common issues.

---

## Boss Room Pattern

Locks the camera to a fixed area when the player enters a boss arena:

```csharp
private void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag("Player"))
        CameraController.Instance.LockToRoom(roomBounds, focusPoint, zoomLevel);
}

private void OnTriggerExit2D(Collider2D other)
{
    if (other.CompareTag("Player"))
        CameraController.Instance.UnlockFromRoom();
}
```

**Setup requirements:**
- BoxCollider2D set as **trigger**, sized to cover the boss arena
- `roomBounds` defines the camera clamp area (can differ from trigger size)
- `focusPoint` optionally overrides the follow target (e.g., center of arena)
- `zoomLevel` can tighten the camera for boss fights (lower orthographicSize)

---

## Zone-Based Bounds (CameraBoundsTriggers)

Defines rectangular camera bounds for different level sections:

- Each zone has a trigger collider and associated `Bounds` data
- When the player enters a zone, the camera transitions to that zone's bounds
- Overlapping zones use priority ordering to resolve conflicts

**Trigger setup:**
- BoxCollider2D as trigger, covering the entire zone area
- Bounds should be larger than the zone trigger to prevent edge clamping issues
- Set via Inspector: min corner and max corner of the camera area

---

## Zone Transitions

When switching between bounds zones, avoid hard cuts:

```csharp
public void TransitionToBounds(Bounds newBounds, float transitionTime = 0.5f)
{
    StartCoroutine(LerpBounds(cameraBounds, newBounds, transitionTime));
}

private IEnumerator LerpBounds(Bounds from, Bounds to, float duration)
{
    float elapsed = 0f;
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
        cameraBounds.min = Vector3.Lerp(from.min, to.min, t);
        cameraBounds.max = Vector3.Lerp(from.max, to.max, t);
        yield return null;
    }
    cameraBounds = to;
}
```

- Use `SmoothStep` for eased transitions, not linear lerp
- Typical transition time: 0.3s - 0.5s
- During transition, camera may temporarily show out-of-bounds areas — ensure backgrounds cover this

---

## Common Issues

### Jittery Camera

**Root cause:** Camera follow running in `Update` instead of `LateUpdate`. Physics resolves positions during FixedUpdate, and Update reads the pre-resolution position, so the camera oscillates between pre- and post-physics positions each frame.

**Fix:** Move all camera position updates to `LateUpdate`. If using Rigidbody2D interpolation on the player, `LateUpdate` gives the interpolated position automatically.

### Parallax Gaps

**Root cause:** Background sprites are not wide enough to cover the camera viewport at extreme aspect ratios. At 21:9 the horizontal view is 31% wider than 16:9.

**Fix:** Size all parallax sprites to cover at least 21:9 aspect ratio. For the widest layers (parallax factor near 0), the sprite barely moves, so standard width is usually fine. For layers near 1.0, ensure coverage matches the full camera bounds plus margin.

### Bounds Snapping

**Root cause:** Switching camera bounds instantly when entering a new zone trigger. The camera jumps from one clamp region to another in a single frame.

**Fix:** Use `TransitionToBounds()` with a lerp duration of 0.3-0.5s. Apply `SmoothStep` for ease-in/ease-out. Also add a dead zone at zone edges so small player movements don't rapidly toggle between zones.

### Camera Stuck at Bounds Edge

**Root cause:** Camera bounds area is smaller than the camera viewport. When `halfWidth > (bounds.max.x - bounds.min.x) / 2`, clamping produces NaN or snaps to center.

**Fix:** Validate that bounds are always larger than the camera viewport. Add a runtime check: if bounds are too small, center the camera in the available space instead of clamping.
