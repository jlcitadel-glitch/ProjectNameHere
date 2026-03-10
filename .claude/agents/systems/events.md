# Events

## C# Events (Code-Only)

Standard pattern for tightly-scoped communication within a system.

```csharp
// Declaration
public static event Action<int, int> OnHealthChanged;

// Raising
OnHealthChanged?.Invoke(currentHealth, maxHealth);

// Subscribing (always in OnEnable/OnDisable pair)
private void OnEnable() => HealthSystem.OnHealthChanged += HandleHealthChanged;
private void OnDisable() => HealthSystem.OnHealthChanged -= HandleHealthChanged;
```

---

## ScriptableObject Event Channels (Decoupled)

For cross-system communication where publisher and subscriber should not know about each other.

```csharp
[CreateAssetMenu(menuName = "Game/Events/Void Event")]
public class VoidEventChannel : ScriptableObject
{
    public event Action OnEventRaised;
    public void RaiseEvent() => OnEventRaised?.Invoke();
}

// Typed variant
[CreateAssetMenu(menuName = "Game/Events/Int Event")]
public class IntEventChannel : ScriptableObject
{
    public event Action<int> OnEventRaised;
    public void RaiseEvent(int value) => OnEventRaised?.Invoke(value);
}
```

**Usage:** Create asset in project, reference it in both publisher and subscriber via serialized fields. Neither needs a direct reference to the other.

---

## Subscribe/Unsubscribe Lifecycle

**Rule:** Every `+=` must have a matching `-=`. Mismatched pairs cause memory leaks or missing callbacks.

| MonoBehaviour hook | Use for |
|--------------------|---------|
| `OnEnable` / `OnDisable` | Scene-scoped subscriptions (most common) |
| `Awake` / `OnDestroy` | Persistent subscriptions on DontDestroyOnLoad objects |

**Never subscribe in Start** — if the object is disabled and re-enabled, Start does not re-run but OnEnable does.

---

## Static Events Guidance

Static events (e.g., `HealthSystem.OnHealthChanged`) are convenient but carry risks:

- **Memory leak:** If a subscriber is destroyed without unsubscribing, the static delegate holds a reference to the dead object, preventing GC.
- **Scene bleed:** Static events survive scene loads. A subscriber from scene A that forgot to unsubscribe will receive events from scene B and likely null-ref.
- **Mitigation:** Always unsubscribe in OnDisable. For DontDestroyOnLoad objects, unsubscribe in OnDestroy. Consider instance events when the static pattern causes problems.

---

## Common Issues

### Memory Leaks from Event Subscriptions
**Root cause:** Subscriber destroyed without `-=`. The delegate chain holds a reference to the destroyed MonoBehaviour.
**Fix:** Enforce OnEnable/OnDisable pairing. In code review, search for `+=` and verify each has a matching `-=` in the same class.

### Events Stop Firing After Scene Load
**Root cause:** Scene objects subscribe in OnEnable but the event publisher is a DontDestroyOnLoad singleton that was not destroyed/recreated. The old subscriptions from destroyed scene objects are gone, but the new scene objects have not subscribed yet because the singleton already existed (Awake did not re-run).
**Fix:** This is correct behavior. Ensure new scene objects subscribe in their own OnEnable. The issue is usually a subscriber that subscribed in Awake or Start instead of OnEnable.
