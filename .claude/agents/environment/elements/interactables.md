# Interactables

> **Unity 6 2D** - All interactables use trigger colliders for detection, InputSystem for activation.

## Base Interactable Pattern

```csharp
// All interactables:
// 1. Show prompt when player is in range (UI overlay)
// 2. Activate on input (interact button)
// 3. Play feedback (sound + animation)
// 4. Trigger effect (open door, toggle switch, etc.)

[RequireComponent(typeof(Collider2D))]
public abstract class BaseInteractable : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] protected float interactRadius = 1.5f;
    [SerializeField] protected GameObject promptUI; // "Press E" / "Press Y" prompt

    protected bool playerInRange;

    public abstract void Interact();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        if (promptUI != null) promptUI.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        if (promptUI != null) promptUI.SetActive(false);
    }
}
```

## Interactable Types

### Doors & Gates

```csharp
// Doors: open/close on interact or when triggered by a linked switch
// Gates: one-way shortcuts (open from one side, stay open)
// State: Open/Closed, persisted via SaveManager

[SerializeField] private bool requiresKey;
[SerializeField] private string keyItemId;
[SerializeField] private Animator doorAnimator;
```

### Levers & Switches

```csharp
// Toggle state on interact
// Can link to doors, gates, platforms, traps
// Visual: sprite swap or animation for on/off state

[SerializeField] private BaseInteractable[] linkedObjects; // Things this switch controls
[SerializeField] private bool isOneShot = false;           // Can only activate once
```

### Breakable Walls

```csharp
// Destroyed by specific abilities (dash, heavy attack)
// Reveals hidden paths or secret areas
// Can optionally require a specific ability to break

[SerializeField] private int hitsToBreak = 1;
[SerializeField] private string requiredAbility; // Empty = any attack works
[SerializeField] private GameObject debrisPrefab;
```

### Ability Gates

```csharp
// Metroidvania progression gates
// Blocked until player has a specific ability
// Examples: dash-through walls, double-jump ledges, grapple points

[SerializeField] private string requiredAbilityId;
[SerializeField] private GameObject visualBlocker; // Disable when ability acquired
```

## Save/Load Integration

```csharp
// Interactable state (opened doors, broken walls, activated switches)
// must persist across save/load via SaveManager

// Each interactable needs a unique scene ID for serialization
[SerializeField] private string persistentId; // Set in inspector, unique per scene
```
