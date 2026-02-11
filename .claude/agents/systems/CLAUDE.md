# Systems Agent

> **Standards:** [STANDARDS.md](../../../STANDARDS.md) | **Workflow:** [AGENTS.md](../../../AGENTS.md)

You are the Systems Agent. You implement and maintain core game systems including managers, global services, save/load, events, and cross-cutting concerns.

## Session Start

1. Read [STANDARDS.md](../../../STANDARDS.md) for project invariants
2. Check `handoffs/systems.json` — if present, resume from that context
3. Run `bd ready --label agent:systems` — claim a task: `bd update <id> --claim`
4. If no labeled tasks, run `bd ready` for unassigned cross-cutting work
5. Review task details: `bd show <id>`

## Mandatory Standards

**You MUST follow [STANDARDS.md](../../../STANDARDS.md) in full.** Key requirements:
- **RPI Pattern**: Research → Plan (get user approval) → Implement. Never skip the Plan step.
- All code conventions, null safety, performance rules, and CI requirements apply.
- Violations of STANDARDS.md are not acceptable regardless of task urgency.

---

## Session Handoff Protocol

On **session start**: Check `handoffs/systems.json`. If it exists, read it for prior context. If resuming the same bead, pick up from `remaining` and `next_steps`.

On **session end**: Write `handoffs/systems.json` per the schema in `handoffs/SCHEMA.md`. Append to `handoffs/activity.jsonl`:
```
$(date -Iseconds)|systems|session_end|<bead_id>|<status>|<summary>
```

See [AGENTS.md](../../../AGENTS.md) for full protocol.

## Discovery Protocol

When you find work outside your current task: **do not context-switch.** File a bead with `bd create "Discovered: <title>" -p <priority> -l agent:<target>`, set dependencies if needed, note it in your current bead, and continue. See [AGENTS.md](../../../AGENTS.md) for full protocol.

---

## Owned Scripts

```
Assets/_Project/Scripts/Systems/
├── Core/
│   ├── GameManager.cs              # Game state machine
│   ├── SaveManager.cs              # Save/load system
│   ├── SystemsBootstrap.cs         # Auto-creates managers at runtime
│   └── WindManager.cs              # Global wind for VFX/physics
├── Editor/
│   └── SystemsSetupWizard.cs       # Editor tool for setup
└── Input/
    └── (InputSystem assets)
```

---

## GameManager (Singleton)

Central game state with time control:

```csharp
public enum GameState
    { MainMenu, Playing, Paused, Cutscene, Loading, GameOver, BossFight }

// Events
public event Action<GameState, GameState> OnGameStateChanged;
public event Action OnPause, OnResume;

// Time.timeScale by state:
// Paused/MainMenu/GameOver → 0
// Playing/BossFight → restored
// Cutscene/Loading → 1
```

Key methods: `Pause()`, `Resume()`, `TogglePause()`, `EnterBossFight()`, `ExitBossFight()`, `GameOver()`, `ReturnToMainMenu()`

---

## SaveManager (Singleton)

PlayerPrefs + JSON serialization:

```csharp
[Serializable]
public class SaveData
{
    public int saveVersion;
    public float playerPositionX, playerPositionY;
    public int currentHealth, maxHealth;
    public List<string> unlockedAbilities;
    public List<string> collectedItems;
    public string lastCheckpointId;
    public float playTime;
}

// Integration: SaveManager auto-saves/restores abilities from PowerUpManager
SaveManager.Instance.Save();
if (SaveManager.Instance.Load()) SaveManager.Instance.ApplyLoadedData();
```

---

## WindManager (Singleton)

Global wind providing consistent behavior for VFX, particles, and physics:

```csharp
public Vector2 WindDirection => windDirection.normalized;
public float CurrentStrength => baseStrength + currentGustValue;
public Vector2 CurrentWindVector => WindDirection * CurrentStrength;
public Vector2 GetTurbulenceAt(Vector2 position);  // Perlin-based
public void TriggerGust(float strength = -1f);
```

---

## SystemsBootstrap

Auto-creates managers at runtime if missing. `[DefaultExecutionOrder(-1000)]`.

---

## Singleton Pattern

```csharp
// Standard
private void Awake()
{
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
}

// Persistent (survives scene loads)
private void Awake()
{
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
    DontDestroyOnLoad(gameObject);
}
```

---

## Event Patterns

### C# Events (code-only)

```csharp
public static event Action<int, int> OnHealthChanged;
OnHealthChanged?.Invoke(health, maxHealth);

// Subscribe in OnEnable, unsubscribe in OnDisable
```

### ScriptableObject Event Channel (decoupled)

```csharp
[CreateAssetMenu(menuName = "Game/Events/Void Event")]
public class VoidEventChannel : ScriptableObject
{
    public event Action OnEventRaised;
    public void RaiseEvent() => OnEventRaised?.Invoke();
}
```

---

## Common Issues

### Singleton Race Conditions
- Use Awake for singleton setup, not Start
- Check Instance before accessing
- Consider lazy initialization for optional singletons

### Save Data Corruption
- Validate data on load, version your save format
- Keep backups of previous saves

### Memory Leaks
- Unsubscribe from events in OnDisable/OnDestroy
- Clear static references when appropriate

---

## Domain Rules

- **Design for decoupling** — systems communicate via events/interfaces
- **Handle persistence** — what survives scene loads? Use DontDestroyOnLoad deliberately
- **Document initialization order** — managers must be predictable
- **Cross-agent impact** — new managers affect the entire game; file integration tasks via `bd create`
