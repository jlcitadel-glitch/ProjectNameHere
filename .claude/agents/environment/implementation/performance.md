# Performance

> **Unity 6 2D** - Environment objects are numerous; performance discipline is critical.

## Collider Optimization

- **Always** use `CompositeCollider2D` for tilemaps — individual tile colliders cause physics seams and poor performance
- Prefer `BoxCollider2D` over `PolygonCollider2D` where possible (cheaper physics)
- Use `EdgeCollider2D` for thin platforms instead of box colliders
- Disable colliders on inactive/offscreen objects

## Object Pooling

```csharp
// Pool frequently spawned objects:
// - Projectile trap projectiles
// - Breakable debris particles
// - Damage number popups

// NEVER Instantiate/Destroy per-frame for recurring objects
// Use a shared object pool or Unity's built-in ObjectPool<T>
```

## Update Loop Discipline

```csharp
// Hazards: avoid OnTriggerStay2D — use enter/exit with state tracking
// Platforms: FixedUpdate only, no Update for movement
// Interactables: event-driven, no polling in Update

// BAD — polls every physics frame
private void OnTriggerStay2D(Collider2D other) { /* damage */ }

// GOOD — tracks state, damages on timer
private void OnTriggerEnter2D(Collider2D other) { playerInZone = true; }
private void OnTriggerExit2D(Collider2D other) { playerInZone = false; }
private void Update()
{
    if (playerInZone) { /* damage on interval timer */ }
}
```

## Performance Limits

| Object Type | Guideline |
|-------------|-----------|
| Active moving platforms per scene | ~10-15 |
| Active hazard projectiles | Pool size ~20 |
| Breakable debris particles | Pool, max ~30 active |
| Trigger zones per scene | No hard limit (cheap when not overlapping player) |

## Off-Screen Optimization

```csharp
// Disable expensive behavior for off-screen objects
// Moving platforms: pause movement when far from camera
// Projectile traps: stop firing when off-screen
// Use OnBecameVisible / OnBecameInvisible or distance checks
```
