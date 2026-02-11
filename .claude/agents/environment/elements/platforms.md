# Platforms

> **Unity 6 2D** - All platforms use Kinematic Rigidbody2D with FixedUpdate movement.

## Moving Platform

```csharp
// Rigidbody2D (Kinematic) moves between waypoints
// Player parents to platform on contact, unparents on exit
// Use FixedUpdate for movement to stay in sync with physics

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    [Header("Waypoints")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float waitTime = 0.5f;

    // Movement in FixedUpdate via rb.MovePosition()
    // Player parenting via OnCollisionEnter2D / OnCollisionExit2D
}
```

### Config (ScriptableObject)

```csharp
[CreateAssetMenu(fileName = "PlatformData", menuName = "Environment/Platform Data")]
public class PlatformData : ScriptableObject
{
    public float moveSpeed = 2f;
    public float waitTime = 0.5f;
    public bool pingPong = true;      // true = reverse at end, false = loop
    public AnimationCurve easeCurve;  // Optional easing between waypoints
}
```

## Crumbling Platform

```csharp
// OnCollisionEnter2D → start shake timer
// After delay → disable collider, play break VFX
// After respawn delay → re-enable

[SerializeField] private float crumbleDelay = 0.5f;   // Time before collapse
[SerializeField] private float respawnDelay = 3f;      // Time before restore
[SerializeField] private float shakeIntensity = 0.05f; // Visual shake amount
```

### State Flow

```
Player lands → Shake (crumbleDelay) → Break (disable collider + VFX) → Wait (respawnDelay) → Restore
```

## One-Way Platform

```csharp
// PlatformEffector2D handles pass-through from below
// Player can drop through via input (disable collider briefly)

// Required components:
// - BoxCollider2D (usedByEffector = true)
// - PlatformEffector2D (surfaceArc = 180, useOneWay = true)
// - Rigidbody2D (Static)
```

### Drop-Through Input

```csharp
// On "drop" input:
// 1. Disable collider for ~0.25s
// 2. Re-enable after delay
// Player falls through during the window
```

## Weighted Platform

```csharp
// Platform sinks under player weight, rises when empty
// Uses spring-like behavior via Rigidbody2D

[SerializeField] private float sinkDepth = 0.5f;
[SerializeField] private float sinkSpeed = 2f;
[SerializeField] private float riseSpeed = 1f;
```
