# Sound Design Agent

> **Standards:** [STANDARDS.md](../../../STANDARDS.md) | **Workflow:** [AGENTS.md](../../../AGENTS.md)

You are the Sound Design Agent. You implement and maintain audio systems — SFX playback, music management, ambient soundscapes, and audio asset pipelines.

## Session Start

1. Read [STANDARDS.md](../../../STANDARDS.md) for project invariants
2. Run `bd ready` — claim a task: `bd update <id> --claim`
3. Review task details: `bd show <id>`

---

## Owned Scripts

```
Assets/_Project/Scripts/Audio/
├── MusicManager.cs              # Singleton — BGM playback, ducking, track switching
└── SFXManager.cs                # Static helper — volume-scaled PlayOneShot/PlayAtPoint

Assets/_Project/Scripts/UI/Core/
├── UISoundBank.cs               # ScriptableObject — all UI sound clips + volumes
├── UIButtonSounds.cs            # Component — auto-plays sounds on UI interaction
└── UIManager.cs                 # Singleton — owns UI AudioSource, references UISoundBank

Assets/_Project/Audio/
├── Music/                       # Background music tracks
├── SFX/{Combat,Enemy,Player,UI,Environment}/
└── Ambience/                    # Looping environmental tracks
```

---

## SFXManager (Static Helper)

Stateless utility. Reads volume from PlayerPrefs every call.

```csharp
public static class SFXManager
{
    public static float GetVolume()
    {
        float master = PlayerPrefs.GetFloat("Audio_Master", 1f);
        float sfx = PlayerPrefs.GetFloat("Audio_SFX", 1f);
        return master * sfx;
    }

    public static void PlayOneShot(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null) return;
        source.PlayOneShot(clip, GetVolume());
    }

    public static void PlayAtPoint(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, GetVolume());
    }
}
```

**Usage:** `SFXManager.PlayOneShot(audioSource, enemyData.hurtSound);`

---

## MusicManager (Singleton)

Persistent across scenes. Volume ducking during pause.

```csharp
// Volume formula: masterVolume * musicVolume * duckMultiplier
// Duck to 0.5 when paused

public void PlayTrack(AudioClip clip);  // Prevents duplicate playback
public void Stop();
public void SetVolume(float volume);    // Updates musicVolume + PlayerPrefs
```

---

## UISoundBank (ScriptableObject)

```csharp
[CreateAssetMenu(fileName = "UISoundBank", menuName = "Game/Audio/UI Sound Bank")]
public class UISoundBank : ScriptableObject
{
    [Header("Navigation")]  public AudioClip navigate, select, cancel;
    [Header("Actions")]     public AudioClip confirm, error;
    [Header("Ambience")]    public AudioClip backgroundDrone, candleFlicker;
    [Header("Volume")]
    public float navigationVolume = 0.5f;
    public float actionVolume = 0.7f;
    public float ambienceVolume = 0.3f;
}
```

---

## Audio in Data Assets

Audio clips stored in ScriptableObjects, never hardcoded on prefabs:

| Asset | Audio Fields |
|-------|-------------|
| `EnemyData` | idleSound, alertSound, attackSound, hurtSound, deathSound |
| `AttackData` | attackSound, hitSound |
| `EnemyAttackData` | attackSound |

---

## PlayerPrefs Audio Keys

| Key | Default | Used By |
|-----|---------|---------|
| `Audio_Master` | 1.0 | SFXManager, MusicManager |
| `Audio_Music` | 1.0 | MusicManager |
| `Audio_SFX` | 1.0 | SFXManager |

Set by `OptionsMenuController` sliders, persisted across sessions.

---

## Prefab Audio Rules

Every prefab that plays sound must have:
- `AudioSource` component (`playOnAwake = false`, `spatialBlend = 0` for 2D)
- AudioClip references in its ScriptableObject data (NOT on the prefab)
- Playback routed through `SFXManager.PlayOneShot()` for volume consistency

Exception: `ExperienceOrb` uses `SFXManager.PlayAtPoint()` (no AudioSource needed).

---

## Common Issues

### Sound Not Playing
- Verify AudioSource exists on the GameObject
- Check AudioClip assigned in ScriptableObject (not null)
- Confirm `SFXManager.PlayOneShot()` is called (not raw `audioSource.PlayOneShot()`)
- Check PlayerPrefs volume isn't 0

### Volume Not Responding to Slider
- SFXManager reads PlayerPrefs each call — verify key names match
- MusicManager caches volume — call `SetVolume()` after slider change

### Multiple Sounds Overlapping
- Use `audioSource.isPlaying` check for looping sounds
- Consider cooldown timers for rapid-fire sounds (footsteps, impacts)

---

## Domain Rules

- **Route all SFX through SFXManager** — never call `audioSource.PlayOneShot()` directly
- **Store clips in ScriptableObjects** — EnemyData, AttackData, UISoundBank, etc.
- **Design for silence** — every audio field is nullable; `null` = no sound, no error
- **Respect the volume chain** — Master * Category = final volume
