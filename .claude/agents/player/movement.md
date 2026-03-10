# Movement

> Back to [CLAUDE.md](CLAUDE.md)

## Movement Parameters

| Parameter | Default | Purpose |
|-----------|---------|---------|
| `speed` | 8f | Horizontal movement speed |
| `jumpingPower` | 12f | Initial jump velocity |
| `jumpCutMultiplier` | 0.5f | Velocity multiplier when releasing jump early (variable jump height) |
| `coyoteTime` | 0.15f | Grace period after leaving ground where jump is still allowed |
| `jumpBufferTime` | 0.15f | Input buffer window — jump press registers even before landing |
| `fallGravityMultiplier` | 2.5f | Increased gravity when falling (makes jumps feel snappy) |
| `maxFallSpeed` | -20f | Terminal velocity cap |

---

## Ground Detection

Ground detection uses `OverlapCircleAll` with a layer mask, filtering out the player's own collider:

```csharp
private bool IsGrounded()
{
    Collider2D[] colliders = Physics2D.OverlapCircleAll(
        groundCheck.position, groundCheckRadius, groundLayer);
    foreach (Collider2D collider in colliders)
        if (collider.gameObject != gameObject) return true;
    return false;
}
```

**Requirements:**
- `groundCheck` Transform must be assigned (child object positioned at player's feet).
- `groundLayer` mask must include the layers used by ground/platform tiles.
- Trigger colliders must be excluded — either via layer separation or explicit `!collider.isTrigger` check.

---

## Physics Pattern

Movement physics happen in `FixedUpdate` to ensure frame-rate independence:

```csharp
private void FixedUpdate()
{
    // Horizontal movement
    rb.linearVelocity = new Vector2(horizontal * speed, rb.linearVelocity.y);

    // Fall gravity multiplier
    if (rb.linearVelocity.y < 0)
    {
        rb.linearVelocity += Vector2.up * Physics2D.gravity.y
            * (fallGravityMultiplier - 1) * Time.fixedDeltaTime;
    }

    // Terminal velocity
    if (rb.linearVelocity.y < maxFallSpeed)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
    }
}
```

Note: Unity 6 uses `Rigidbody2D.linearVelocity` (not `.velocity`).

---

## Common Issues

### Jump Not Working After Landing
**Root cause:** Ground detection fails silently when misconfigured.
**Fix checklist:**
1. Verify `groundLayer` mask is assigned in Inspector and matches the layer on ground tilemap/colliders.
2. Confirm `groundCheck` Transform is assigned and positioned at the bottom of the player sprite.
3. Check that `coyoteTimeCounter` resets to `coyoteTime` when `IsGrounded()` returns true.
4. Ensure the ground objects have non-trigger Collider2D components on the correct layer.

### Floaty Movement
**Root cause:** Gravity multiplier too low or Rigidbody2D gravity scale misconfigured.
**Fix:**
- Increase `fallGravityMultiplier` (2.5 to 3.5 is the typical range for snappy platformers).
- Decrease `coyoteTime` if the leniency window makes landing feel imprecise.
- Check Rigidbody2D `gravityScale` in Inspector — should be 1.0 unless intentionally modified.
- Verify `maxFallSpeed` is not set too close to zero (capping speed too early flattens the arc).

### Player Slides on Slopes
**Root cause:** No friction material or slope handling.
**Fix:** Add a Physics Material 2D with friction to the player's collider, or implement slope snapping logic that zeroes vertical velocity when grounded on slopes.
