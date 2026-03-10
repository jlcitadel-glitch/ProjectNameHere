# Architectural Patterns

> Canonical code patterns for this project. When reviewing or designing new systems, ensure they follow these conventions.

## Component Architecture

Single-responsibility components, not monolithic controllers.

```csharp
// PREFERRED: Single-responsibility components
public class PlayerMovement : MonoBehaviour { }
public class PlayerHealth : MonoBehaviour { }
public class PlayerAbilities : MonoBehaviour { }

// AVOID: Monolithic controllers
public class PlayerController : MonoBehaviour { /* everything */ }
```

**Why:** Monolithic controllers become merge-conflict magnets and make it impossible for multiple agents to work on the same GameObject in parallel. Single-responsibility components also improve testability and make it easier to disable/enable specific behaviors.

## State Machine Pattern

Used by PlayerControllerScript, EnemyController, GameManager, and BossController.

```csharp
public enum PlayerState { Idle, Running, Jumping, Falling, Dashing }

private PlayerState currentState;

private void UpdateStateMachine()
{
    var newState = DetermineState();
    if (newState != currentState)
    {
        ExitState(currentState);
        currentState = newState;
        EnterState(currentState);
    }
}
```

**Why:** Explicit state transitions prevent conflicting behaviors (e.g., dashing while parrying) and make debugging easier -- you can always log state changes. The Exit/Enter pattern ensures cleanup happens reliably.

## Ability System Pattern

Abilities are MonoBehaviour components checked via interface.

```csharp
public interface IAbility
{
    bool CanActivate { get; }
    void Activate();
    void Reset();
}

// Abilities as components, checked dynamically
if (TryGetComponent<IAbility>(out var ability) && ability.CanActivate)
    ability.Activate();
```

**Why:** Adding new abilities requires only adding a new component -- no modification to existing code. `TryGetComponent` avoids null-ref crashes if the ability component is removed or not yet unlocked.

## Singleton Pattern

Used by GameManager, SaveManager, UIManager, MusicManager.

```csharp
public static GameManager Instance { get; private set; }

private void Awake()
{
    if (Instance != null) { Destroy(gameObject); return; }
    Instance = this;
    DontDestroyOnLoad(gameObject);
}

private void OnDestroy()
{
    if (Instance == this) Instance = null;
}
```

**Why:** The `OnDestroy` null-clear prevents stale references after scene transitions. `DontDestroyOnLoad` ensures managers survive scene loads. The duplicate check in Awake prevents multiple instances when returning to a scene.

## ScriptableObject Data Pattern

Configuration data lives in ScriptableObjects, not hardcoded in components.

```csharp
[CreateAssetMenu(menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public float maxHealth;
    public float moveSpeed;
    public float attackDamage;
}
```

**Why:** Designers can tune values without touching code. Data assets can be swapped at runtime (e.g., difficulty scaling). Changes to data don't trigger recompilation.
