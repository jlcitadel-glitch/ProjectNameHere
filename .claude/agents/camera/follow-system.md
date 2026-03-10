# Camera Follow System

Reference for camera follow logic, bounds clamping, camera shake, and Cinemachine integration.

---

## Camera Follow Pattern

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

**Key fields:**
- `smoothTime` — Lower = snappier (0.1), higher = floatier (0.3). Typical: 0.15
- `lookAheadPos` — Offset based on player facing direction, gives anticipation
- `offset` — Fixed vertical offset so the camera sits slightly above the player
- `velocity` — Internal ref for SmoothDamp, do not set manually

---

## Bounds Clamping

Prevents camera from showing areas outside the level:

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

**Performance note:** Cache `halfHeight` and `halfWidth` if `orthographicSize` and aspect ratio are not changing per frame. Recalculate only on resolution change.

**Aspect ratio caution:** At 21:9, `halfWidth` is 31% larger than at 16:9. Always test bounds at extreme ratios to prevent background gaps.

---

## Camera Shake

Screen shake for impacts, boss attacks, and environmental events:

```csharp
public void Shake(float duration, float magnitude)
{
    StartCoroutine(ShakeCoroutine(duration, magnitude));
}

private IEnumerator ShakeCoroutine(float duration, float magnitude)
{
    float elapsed = 0f;
    while (elapsed < duration)
    {
        float currentMagnitude = Mathf.Lerp(magnitude, 0f, elapsed / duration);
        Vector2 offset = Random.insideUnitCircle * currentMagnitude;
        // Apply offset to camera position (restored each frame by follow logic)
        elapsed += Time.deltaTime;
        yield return null;
    }
}
```

- Magnitude lerps to zero over duration for natural decay
- Shake offset is additive — applied after follow position, before final assignment
- Typical values: hit = (0.1s, 0.15), boss slam = (0.3s, 0.4)

---

## Cinemachine Integration (Optional)

The project includes Cinemachine 3.1.5. Use for complex camera work (cutscenes, multi-target follow):

```csharp
using Unity.Cinemachine;

CinemachineCamera virtualCam;
CinemachineConfiner2D confiner;
confiner.BoundingShape2D = roomCollider;
```

**When to use Cinemachine vs custom:**
- **Custom (AdvancedCameraController):** Standard gameplay follow, simple bounds, shake
- **Cinemachine:** Cutscenes, multi-target blending, complex zone transitions, dolly tracks

If mixing both, disable one when the other is active to prevent fighting.
