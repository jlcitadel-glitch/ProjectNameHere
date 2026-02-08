# Project Standards

> Universal invariants for all agents. These are **always true** — not tasks to complete, but rules to follow. If you violate one, file a bead to fix it.

## Unity 6 API Rules

**Target:** Unity 6000.3.4f1. Use only Unity 6 supported APIs.

### Mandatory API Changes

```csharp
// Rigidbody2D — use linearVelocity, NOT velocity
rb.linearVelocity = new Vector2(x, rb.linearVelocity.y);  // CORRECT
rb.velocity = new Vector2(x, rb.velocity.y);               // DEPRECATED

// Object finding — use FindObjectsByType, NOT FindObjectOfType
FindAnyObjectByType<T>();                                   // CORRECT (single)
FindObjectsByType<T>(FindObjectsSortMode.None);             // CORRECT (multiple)
FindObjectOfType<T>();                                      // DEPRECATED

// Tags — use CompareTag, NOT string equality
other.CompareTag("Player");                                 // CORRECT
other.tag == "Player";                                      // WRONG

// Async — use Awaitable, NOT Task
await Awaitable.NextFrameAsync();                           // CORRECT
await Awaitable.WaitForSecondsAsync(1f);                    // CORRECT
```

### Input System

- Unity InputSystem 1.17.0+ (not legacy `Input` class)
- Callbacks via `InputAction.CallbackContext`
- `context.performed` for press, `context.canceled` for release

### Rendering

- Universal Render Pipeline (URP) 17.3.0
- Particle shaders: `Universal Render Pipeline/Particles/Unlit`
- 2D project — no 3D components unless explicitly justified

---

## Code Organization

### Naming

- Classes: `PascalCase` (e.g., `PlayerControllerScript`)
- Fields/Methods: `camelCase` (e.g., `jumpBufferCounter`)
- Private fields: explicit `private` keyword, no prefix

### Inspector Organization

```csharp
[Header("Movement")]
[SerializeField] private float moveSpeed = 5f;

[Header("References")]
[SerializeField] private Rigidbody2D rb;

[Header("Debug")]
[SerializeField] private bool showGizmos = true;
```

- `[SerializeField]` for tweakable values — never `public` fields
- `[Header("Section")]` to group related fields
- Extract magic numbers to serialized fields

### Component Lifecycle

```csharp
private void Awake()    // Cache own components (GetComponent)
private void Start()    // Cross-component references, initialization order
private void OnEnable() // Subscribe to events
private void OnDisable()// Unsubscribe from events
```

### Update Loop Discipline

| Loop | Use For |
|------|---------|
| `Update()` | Input polling, timers, state machines, animation triggers |
| `FixedUpdate()` | Physics: Rigidbody velocity, forces, movement |
| `LateUpdate()` | Camera follow, UI updates after movement, transform sync |

**Never** put physics in Update. **Never** put input in FixedUpdate.

---

## Null Safety

```csharp
// Defensive GetComponent — log errors for required dependencies
rb = GetComponent<Rigidbody2D>();
if (rb == null)
    Debug.LogError($"[{gameObject.name}] Missing Rigidbody2D");

// TryGetComponent for optional dependencies
if (TryGetComponent<DashAbility>(out var dash))
    dash.Execute();

// Null-conditional for event invocation
OnPlayerDied?.Invoke();
```

---

## ScriptableObject Conventions

```csharp
[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item Name")]
public class ItemData : ScriptableObject
{
    [Header("Section")]
    public float someValue = 5f;
}
```

- Store configuration in ScriptableObjects, not on prefab components
- Audio clips, stats, timing values — all in data assets
- Nullable fields are OK: `null` = "not configured", never an error

---

## Prefab Workflow

- All reusable GameObjects **must** be prefabs
- **Edit in Prefab Mode** — never modify scene instances directly
- Scene contains prefab instances, not embedded objects
- Component setup belongs on the prefab, not per-instance overrides

---

## RPI Pattern (Research, Plan, Implement)

1. **Research** — Explore codebase, understand existing patterns before changes. Run `bd ready` to check for related open tasks.
2. **Plan** — Design approach, identify impacts, get user approval for non-trivial work. Create bd tasks for multi-step plans.
3. **Implement** — Write code following these standards. Update bd task status as you go.

---

## Continuous Integration

CI runs on every push/PR via GitHub Actions. **No Unity Editor required.**

```bash
python ci/run_all.py    # Run all checks locally
```

| Check | What It Validates |
|-------|-------------------|
| `check_meta_files.py` | Every asset has a `.meta`; no orphaned metas |
| `check_guid_references.py` | No broken GUID references in `_Project/` assets |
| `check_layer_consistency.py` | Layer/tag names in C# match `TagManager.asset` |
| `check_scene_build_settings.py` | Build scenes exist with correct GUIDs |

**Rule:** CI must pass before merge. If CI catches a violation, file a bead to fix it.

---

## Performance Rules

### Avoid

- `Find()`, `FindObjectOfType()` in Update loops
- `GetComponent()` every frame — cache in Awake
- String comparisons for tags — use `CompareTag()`
- Allocations in hot paths (Update/FixedUpdate)
- Complex LINQ in gameplay code

### Prefer

- Cached references set in Awake/Start
- Object pooling for frequently spawned objects
- Physics layers for collision filtering (serialized LayerMask, not hardcoded)
- Gizmos for spatial debugging (`OnDrawGizmosSelected`)

---

## Events & Communication

```csharp
// UnityEvents for Inspector-assignable callbacks
[SerializeField] private UnityEvent onLanded;

// C# events for code-only subscriptions
public event System.Action OnJumped;

// Static events for global systems (use sparingly)
public static event System.Action<Player> OnPlayerSpawned;
```

Always unsubscribe in `OnDisable()` / `OnDestroy()`.

---

## Session Protocol

Every agent session follows this sequence:

1. Read `STANDARDS.md` (this file) for project invariants
2. Read your agent CLAUDE.md for domain expertise
3. Run `bd ready` — claim a task before starting work
4. Follow RPI pattern during implementation
5. End session per AGENTS.md landing protocol (`bd sync` + `git push`)

See [AGENTS.md](AGENTS.md) for the full beads workflow reference.
