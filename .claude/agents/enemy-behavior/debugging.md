# Debugging (Proven Feb 2026)

## Strategy

### 1. Add Runtime Diagnostics First — Don't Guess
Use `EnemyDiagnostic.cs` (in `Enemies/Core/`). Use `Debug.LogWarning` so output shows regardless of console filters. Log state transitions, sensor hits, attack phases, and health changes.

### 2. Check Configuration Before Code
Most issues are configuration, not logic:
- **Layer names** in TagManager — unnamed layers silently break LayerMask filtering
- **Layer assignments** on prefabs AND scene overrides
- **Tags** on cameras (`MainCamera`), players (`Player`), enemies (`Enemy`)
- **Serialized fields** — `targetLayers`, `groundLayer`, `attacks[]`

### 3. Compare Position Over Time, Not Just Velocity
`rb.linearVelocity` can be non-zero while enemy is visually frozen (Animator overriding transform). Log `transform.position` across multiple frames to confirm actual movement.

### 4. Fix One Thing at a Time
Isolated changes with clear before/after verification. Multi-change fixes make it impossible to know what actually solved the problem.

---

## Common Issues — Root Cause Analysis

### Enemy Invisible (Sprite Not Rendering)

**Root cause:** SpriteRenderer using wrong material and/or sorting layer. New prefabs default to URP `Sprite-Lit-Default` material and `Default` sorting layer — both cause invisibility in this project's rendering setup.

**Fix — required SpriteRenderer values:**
| Property       | Value                                                                    |
|----------------|--------------------------------------------------------------------------|
| Material       | Built-in `Sprites-Default` (`fileID: 10754, guid: 0000...f000...0, type: 0`) |
| Sorting Layer  | `Ground` (index 5, ID `1790128183`)                                     |
| Sorting Order  | `10`                                                                     |

**Verification:** Open Bat.prefab or Slime.prefab in Prefab Mode. Compare SpriteRenderer component values field-by-field against the new prefab.

---

### Enemy Not Moving (Frozen Despite Non-Zero Velocity)

**Root cause:** Animator overrides physics position every frame. Animation clips containing `Transform.localPosition` keyframes snap the enemy back to animated position, counteracting Rigidbody2D movement.

**Fix:** `EnemyController.LateUpdate()` re-syncs `transform.position` from `rb.position` after the Animator has run. This ensures physics-driven movement wins over animation position.

**Verification:**
1. Add diagnostic log in `LateUpdate`: `Debug.LogWarning($"Pos: {transform.position}, RBPos: {rb.position}");`
2. If they diverge, the Animator is overriding. The LateUpdate re-sync fixes it.
3. If both are the same but enemy is still frozen, check if movement component is calling `rb.MovePosition()` or setting `rb.linearVelocity`.

---

### Sensors Not Detecting Player

**Root cause:** `targetLayers` bitmask pointed to an unnamed layer in TagManager. `Physics2D.OverlapCircleAll` with an unnamed layer bitmask returns zero results — no error, no warning, just silence.

**Fix:**
1. Open `Edit > Project Settings > Tags and Layers`
2. Ensure the layer used for Player is **named** (not blank)
3. Re-assign `targetLayers` on the enemy prefab to point to the named layer

**Fallback:** `EnemySensors.Start()` detects `targetLayers == 0` and switches to tag-based detection (`CompareTag("Player")`). This works but is slower than LayerMask-based detection.

**Verification:** In Play Mode, select the enemy and watch `EnemySensors` in the Inspector. The `hasTarget` field should toggle when the player enters/exits detection range.

---

### Enemy Not Attacking

**Checklist (in order):**
1. `EnemyCombat` component exists on the prefab
2. `EnemyData.attacks[]` array is not empty (most common miss)
3. `EnemyData.attackRange` > 0
4. `EnemyData.attackCooldown` is not unreasonably high
5. Enemy is actually reaching Chase state (check sensors first — see above)
6. Player is within `attackRange` (add diagnostic log for distance-to-target)
7. `AttackOrigin` child Transform exists (auto-created if missing, but verify position)

---

### EnemyAttackHitbox Invalid Layer

**Root cause:** `LayerMask.NameToLayer("EnemyAttack")` returns `-1` if the layer is not defined in TagManager. Assigning layer `-1` to a GameObject silently sets it to layer 0 (Default), which means the hitbox collides with everything or nothing depending on collision matrix.

**Fix:**
1. Define `EnemyAttack` layer in `Edit > Project Settings > Tags and Layers`
2. Update Physics2D collision matrix so `EnemyAttack` collides with `Player` but not `Enemy`
3. In code, always guard: `int layer = LayerMask.NameToLayer("EnemyAttack"); if (layer >= 0) gameObject.layer = layer;`

---

### Boss Phase Not Triggering

**Verification checklist:**
1. `BossController` component exists on the boss prefab
2. `HealthSystem` component exists and has correct `maxHealth`
3. `BossController` subscribes to `HealthSystem.OnHealthChanged` (check `Start()` or `OnEnable()`)
4. Phase HP thresholds are set correctly (50% and 20% of maxHealth)
5. Boss is actually taking damage (check `HealthSystem` events fire)
6. Wave stat modifier isn't pushing HP so high that 50% threshold is never visually reached in testing

---

## Prefab Structure Reference

```
EnemyPrefab
├── SpriteRenderer          # Material: Sprites-Default, Layer: Ground, Order: 10
├── Animator                # Controller: matching enemy type
├── Rigidbody2D             # Gravity: per movement type
├── Collider2D              # BoxCollider2D or CapsuleCollider2D
├── AudioSource             # For enemy SFX
├── EnemyController         # References EnemyData asset
├── HealthSystem
├── [Movement Component]    # GroundPatrolMovement OR FlyingMovement OR HoppingMovement
├── EnemyCombat
├── EnemySensors
├── GroundCheck     (child Transform — auto-created if missing)
├── WallCheck       (child Transform — auto-created if missing)
├── LedgeCheck      (child Transform — auto-created if missing)
└── AttackOrigin    (child Transform — auto-created if missing)
```
