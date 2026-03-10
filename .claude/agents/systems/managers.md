# Managers

## GameManager (Singleton, Persistent)

Central game state machine with time control.

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

**Key methods:** `Pause()`, `Resume()`, `TogglePause()`, `EnterBossFight()`, `ExitBossFight()`, `GameOver()`, `ReturnToMainMenu()`

---

## Singleton Pattern

### Standard (scene-scoped)

```csharp
private void Awake()
{
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
}
```

### Persistent (survives scene loads)

```csharp
private void Awake()
{
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
    DontDestroyOnLoad(gameObject);
}
```

**Existing persistent singletons:** GameManager, SaveManager, UIManager, MusicManager — all use `Instance` + `DontDestroyOnLoad` + clear Instance in `OnDestroy`.

---

## WindManager (Singleton)

Global wind providing consistent behavior for VFX, particles, and physics.

```csharp
public Vector2 WindDirection => windDirection.normalized;
public float CurrentStrength => baseStrength + currentGustValue;
public Vector2 CurrentWindVector => WindDirection * CurrentStrength;
public Vector2 GetTurbulenceAt(Vector2 position);  // Perlin-based
public void TriggerGust(float strength = -1f);
```

---

## Common Issues

### Singleton Race Conditions
**Root cause:** Two singletons access each other in Awake, but execution order is undefined.
**Fix:** Use Awake only for self-initialization. Access other singletons in Start or later. If order matters, use `[DefaultExecutionOrder]`.

### TimeScale Conflicts
**Root cause:** Multiple systems set Time.timeScale independently (pause, cutscene, slow-mo).
**Fix:** All time control goes through GameManager state transitions. Never set Time.timeScale directly.
