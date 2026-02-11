# Physics Rules

> **Unity 6 2D** - Rigidbody2D and 2D colliders only. No 3D physics components.

## Rigidbody2D Body Types

| Body Type | Use For |
|-----------|---------|
| **Static** | Terrain, walls, static hazards — never moves |
| **Kinematic** | Moving platforms, doors — moves via `MovePosition()` |
| **Dynamic** | Projectiles, debris, physics-driven objects |

## Movement Rules

```csharp
// CORRECT — Kinematic platforms in FixedUpdate
private void FixedUpdate()
{
    Vector2 target = CalculateNextPosition();
    rb.MovePosition(target);
}

// WRONG — never move via transform
transform.position = newPosition; // Causes physics desync
```

## Platform Effectors

### PlatformEffector2D (One-Way Platforms)

```csharp
// Required components:
// - BoxCollider2D or EdgeCollider2D
// - PlatformEffector2D
// - Rigidbody2D (Static)

// Collider must have: usedByEffector = true
// Effector settings:
//   surfaceArc = 180      (pass through from below)
//   useOneWay = true
//   useOneWayGrouping = true
```

### SurfaceEffector2D (Conveyor Belts)

```csharp
// Moves objects along the surface
// surfaceSpeed: positive = right, negative = left
// speedVariation: randomness in speed
```

## Collision Layers

```csharp
// Use serialized LayerMask for collision filtering
[SerializeField] private LayerMask groundLayer;
[SerializeField] private LayerMask playerLayer;
[SerializeField] private LayerMask hazardLayer;

// NEVER hardcode layer numbers
// ALWAYS use CompareTag() for tag checks
```

## Trigger vs Collision

| Use Trigger When | Use Collision When |
|------------------|--------------------|
| Detecting player proximity (interactables) | Player should stand on it (platforms) |
| Damage zones (hazards) | Physical blocking (walls, doors) |
| Area transitions (zones) | Pushable objects |
| Pickup collection | Kinematic platform parenting |

## Physics Material 2D

```csharp
// Use PhysicsMaterial2D for surface properties
// Ice: friction = 0.05, bounciness = 0
// Bouncy: friction = 0.4, bounciness = 0.8
// Default: friction = 0.4, bounciness = 0

// Apply via Collider2D.sharedMaterial (not material — avoids copies)
```
