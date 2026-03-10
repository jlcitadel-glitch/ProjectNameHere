# Abilities

> Back to [CLAUDE.md](CLAUDE.md)

## Architecture

Abilities are separate `MonoBehaviour` components attached to the same GameObject as `PlayerControllerScript`. The controller discovers them at startup:

```csharp
// PlayerControllerScript.Awake()
doubleJumpAbility = GetComponent<DoubleJumpAbility>();
dashAbility = GetComponent<DashAbility>();
```

This component-based pattern keeps abilities decoupled from the core movement script. Each ability manages its own state, cooldowns, and activation logic.

---

## IAbility Interface (Recommended)

Future abilities should implement this interface for consistent activation:

```csharp
public interface IAbility
{
    bool IsUnlocked { get; }
    bool CanActivate { get; }
    void Activate();
    void Reset();
}
```

Existing abilities (DashAbility, DoubleJumpAbility) predate this interface but follow its spirit with similar method signatures. The current codebase uses direct component references (e.g., `GetComponent<DashAbility>()`), not `IAbility`-based discovery.

---

## Existing Abilities

### DashAbility

| Parameter | Value | Purpose |
|-----------|-------|---------|
| `dashSpeed` | 20 | Horizontal velocity during dash |
| `dashDuration` | 0.2s | How long the dash lasts |
| `dashCooldown` | 1.0s | Time before dash can be used again |

Dash overrides horizontal velocity for its duration and typically grants invulnerability frames. Interacts with gravity (should disable fall gravity during dash).

### DoubleJumpAbility

Grants one additional jump when airborne. Resets on landing (when `IsGrounded()` returns true). Must coordinate with coyote time — a coyote-time jump should not consume the double jump.

### PowerUpManager

Tracks which abilities the player has unlocked. Used by the save system to persist ability state across sessions. Lives on the player GameObject.

### PowerUpPickup

Collectible trigger in the world. When the player enters its trigger collider, it calls `PowerUpManager` to unlock the ability and adds the corresponding component to the player GameObject at runtime.

---

## 5-Step Ability Addition Process

1. **Create the component** in `Assets/_Project/Scripts/Abilities/`. Implement activation logic, cooldown, and reset. Follow the `IAbility` interface pattern.

2. **Add to `PowerUpType` enum** in `PowerUpPickup.cs`. This enum identifies which ability a pickup grants.

3. **Add a case to `PowerUpPickup.AddAbilityComponent()`** that attaches your new component to the player GameObject when collected.

4. **Reference in `PlayerControllerScript.Awake()`** so the controller can discover and interact with the ability.

5. **Check ability state in Update/FixedUpdate** within `PlayerControllerScript`. Wire up the input trigger (e.g., a new InputAction callback) and call the ability's activation method.

After adding, verify:
- The ability works when added at runtime (via pickup) and when already present at scene start (via save load).
- The ability resets correctly on landing/state changes.
- The ability interacts correctly with dash and double jump (no stacking exploits).

---

## Common Issues

### Ability Not Detected
**Root cause:** Component not on the expected GameObject or not yet added when `Awake()` runs.
**Fix:**
1. Confirm the ability component is on the same GameObject as `PlayerControllerScript` (not a child object).
2. If added at runtime, call `RefreshAbilities()` (or re-run `GetComponent`) after adding the component.
3. Check that the ability component is enabled (`component.enabled == true`).

### Ability Activates During Invalid State
**Root cause:** No state guard in activation check.
**Fix:** Ensure `CanActivate` checks for conflicting states (e.g., don't allow dash while already dashing, don't allow abilities during death/cutscene). Check `GameManager.Instance.CurrentState` if relevant.

### Double Jump Consumed by Coyote Time Jump
**Root cause:** Coyote time jump counted as an air jump, decrementing the double jump counter.
**Fix:** Only decrement the extra jump counter when the player is truly airborne (not during coyote time window). Track whether the first jump was a grounded/coyote jump separately.
