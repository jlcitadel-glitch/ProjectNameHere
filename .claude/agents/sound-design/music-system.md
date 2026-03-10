# Music System

## MusicManager (Singleton)

`Assets/_Project/Scripts/Audio/MusicManager.cs`

Persistent across scenes via `DontDestroyOnLoad`. Manages background music playback and volume ducking.

### Lifecycle

- **Awake:** Creates its own `AudioSource` (loop=true, playOnAwake=false, spatialBlend=0). Loads volume from PlayerPrefs. Loads `Resources/GameplayMusic` as the default gameplay track.
- **Start:** Subscribes to `GameManager.OnGameStateChanged`.
- **OnDestroy:** Unsubscribes from GameManager. Clears `Instance`.

### Volume Formula

```
finalVolume = masterVolume * musicVolume * duckMultiplier
```

- `masterVolume` — from `PlayerPrefs.GetFloat("Audio_Master", 1f)`, re-read on every `ApplyVolume()` call
- `musicVolume` — set via `SetVolume()` or from `PlayerPrefs.GetFloat("Audio_Music", 1f)` at startup
- `duckMultiplier` — `0.5` when paused, `1.0` otherwise

### API

```csharp
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; }

    // Plays a track. No-ops if the same clip is already playing.
    public void PlayTrack(AudioClip clip);

    // Stops playback.
    public void Stop();

    // Sets music volume (0-1) and re-applies the volume chain.
    public void SetVolume(float volume);
}
```

### Game State Reactions

| State transition | Behavior |
|-----------------|----------|
| Any -> `Paused` | Duck volume to 50% |
| `Paused` -> Any | Restore full volume |
| Any -> `GameOver` | Stop music entirely |
| Any -> `Playing` (not from Pause) | Start gameplay track if not already playing |

### Ducking Behavior

When `GameManager.GameState` changes to `Paused`, `isDucked` is set to true and `ApplyVolume()` multiplies by 0.5. Unpausing sets `isDucked` to false and restores volume. This gives a subtle audio cue that the game is paused without cutting music entirely.

---

## Adding a New Music Track

1. Place the audio file in `Assets/_Project/Audio/Music/`.
2. If it should play automatically in a scene, either:
   - Put it in `Resources/` and load it by name, or
   - Reference it from a scene-specific MonoBehaviour that calls `MusicManager.Instance.PlayTrack(clip)`.
3. The manager prevents duplicate playback — calling `PlayTrack` with the already-playing clip is a safe no-op.
