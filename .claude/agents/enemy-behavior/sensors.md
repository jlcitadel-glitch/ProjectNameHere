# Sensors

## DetectionType Enum

```csharp
public enum DetectionType { Radius, Cone, LineOfSight }
```

## Detection Logic Per Type

### Radius
Simplest detection. Uses `Physics2D.OverlapCircleAll` within `detectionRange`.
- Detects player regardless of facing direction
- Best for ambient enemies, flying enemies, or enemies that should always notice nearby players

### Cone
Radius check + `Vector2.Angle` between facing direction and direction-to-target.
- Only detects within `detectionAngle` degrees of facing direction
- Player can approach from behind undetected
- Best for patrol enemies where flanking should be viable

### LineOfSight
Radius check + `Physics2D.Raycast` between enemy and target.
- Blocked by `obstacleLayers` — walls and terrain break detection
- Best for ranged enemies or enemies in complex environments with cover

## Configuration Gotchas

### targetLayers must reference named layers
`Physics2D.OverlapCircleAll` with a LayerMask pointing to an unnamed layer returns nothing — silently. This is the #1 sensor bug.

**Fix:** Ensure the Player layer is named in `Edit > Project Settings > Tags and Layers`. The fallback in `EnemySensors.Start()` detects `targetLayers == 0` and switches to tag-based detection (`CompareTag("Player")`), but relying on the fallback is slower and should be avoided.

### detectionRange vs attackRange
`detectionRange` (in EnemyData) controls when sensors fire `OnTargetDetected`. `attackRange` (also in EnemyData) controls when EnemyController transitions from Chase to Attack. If `attackRange > detectionRange`, the enemy will never attack because it will never get close enough to detect the player first. Always ensure `detectionRange > attackRange`.

### obstacleLayers for LineOfSight
Must include terrain/wall layers. If empty, raycast never hits anything and LineOfSight behaves identically to Radius — defeating its purpose.

### Sensor tick rate
Sensors are ticked by EnemyController during appropriate states (Idle, Patrol, Alert, Chase). They do NOT tick during Attack, Cooldown, Stunned, or Dead — this prevents mid-attack retargeting and wasted physics queries.
