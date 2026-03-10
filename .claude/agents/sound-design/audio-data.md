# Audio Data and Configuration

## UISoundBank (ScriptableObject)

`Assets/_Project/Scripts/UI/Core/UISoundBank.cs`

```csharp
[CreateAssetMenu(fileName = "UISoundBank", menuName = "Audio/UI Sound Bank")]
public class UISoundBank : ScriptableObject
```

**Namespace:** `ProjectName.UI`

### Fields

| Header | Fields |
|--------|--------|
| Navigation | `navigate`, `select`, `cancel`, `tabSwitch` |
| Actions | `confirm`, `error`, `itemPickup`, `equipItem` |
| Menu Transitions | `menuOpen`, `menuClose`, `pause`, `resume` |
| Gothic Ambience | `backgroundDrone`, `candleFlicker`, `windAmbience` |
| Feedback | `healthGain`, `healthLoss`, `soulFill`, `abilityReady` |
| Volume Settings | `navigationVolume` (0.5), `actionVolume` (0.7), `ambienceVolume` (0.3) |

### Playback Methods

UISoundBank has built-in convenience methods that route through `SFXManager.GetVolume()`:

```csharp
public void PlaySound(AudioClip clip, AudioSource source, float volumeMultiplier = 1f);
public void PlayNavigate(AudioSource source);   // uses navigationVolume
public void PlaySelect(AudioSource source);     // uses navigationVolume
public void PlayCancel(AudioSource source);     // uses navigationVolume
public void PlayTabSwitch(AudioSource source);  // uses actionVolume
public void PlayConfirm(AudioSource source);    // uses actionVolume
public void PlayError(AudioSource source);      // uses actionVolume
public void PlayMenuOpen(AudioSource source);   // uses actionVolume
public void PlayMenuClose(AudioSource source);  // uses actionVolume
```

### Loading

UIManager loads UISoundBank via `Resources.Load<UISoundBank>("UISoundBank")` as a fallback. The asset must exist at `Assets/_Project/Resources/UISoundBank.asset` for this to work.

---

## Audio Fields in Other ScriptableObjects

| ScriptableObject | Audio Fields | Owner Agent |
|-----------------|--------------|-------------|
| `EnemyData` | `idleSound`, `alertSound`, `attackSound`, `hurtSound`, `deathSound` | `enemy-behavior` |
| `AttackData` | `attackSound`, `hitSound` | `player` |
| `EnemyAttackData` | `attackSound` | `enemy-behavior` |

These fields are all `AudioClip` type, nullable. The owning agent manages the ScriptableObject structure; the sound-design agent advises on audio content and ensures playback uses SFXManager.

---

## PlayerPrefs Keys

| Key | Default | Read By | Written By |
|-----|---------|---------|------------|
| `Audio_Master` | `1.0` | SFXManager, MusicManager | OptionsMenuController |
| `Audio_Music` | `1.0` | MusicManager | OptionsMenuController |
| `Audio_SFX` | `1.0` | SFXManager | OptionsMenuController |

### OptionsMenuController Integration

`Assets/_Project/Scripts/UI/Menus/OptionsMenuController.cs` owns the volume sliders. When a slider changes:
1. The new value is written to the corresponding PlayerPrefs key.
2. `PlayerPrefs.Save()` persists across sessions.
3. SFXManager picks up the new value on the next `GetVolume()` call (no caching).
4. MusicManager requires an explicit `SetVolume()` call or `ApplyVolume()` to update, since it caches `musicVolume`.

---

## Common Issues

### Sound Not Playing

| Check | Why |
|-------|-----|
| AudioSource exists on the GameObject | `SFXManager.PlayOneShot` null-checks the source and silently returns if missing |
| AudioClip is assigned in the ScriptableObject | A null clip field means "no sound configured" — this is by design, not a bug |
| Playback uses `SFXManager.PlayOneShot()` | Raw `audioSource.PlayOneShot()` bypasses the volume chain entirely |
| PlayerPrefs volume is not 0 | Check `Audio_Master` and `Audio_SFX` — if either is 0, final volume is 0 |

**Root cause:** Most "no sound" bugs are a missing AudioSource component or an unassigned clip field in the ScriptableObject Inspector.

### Volume Not Responding to Slider

| Check | Why |
|-------|-----|
| PlayerPrefs key names match exactly | SFXManager reads `Audio_Master` and `Audio_SFX` — a typo means it reads the default (1.0) |
| MusicManager.SetVolume() is called | MusicManager caches `musicVolume`; changing the PlayerPrefs key alone does not update it until `ApplyVolume()` runs |

**Root cause:** SFX volume is self-correcting (read each call), but music volume requires the OptionsMenuController to call `MusicManager.Instance.SetVolume()` explicitly.

### Multiple Sounds Overlapping

| Check | Why |
|-------|-----|
| Use `audioSource.isPlaying` guard for loops | Starting a loop while one is already playing creates layered audio |
| Add cooldown timers for rapid-fire sounds | Footsteps, impacts, and other high-frequency triggers can stack if fired every frame |

**Root cause:** `PlayOneShot` is fire-and-forget by design — it does not stop previous sounds. For sounds that should not overlap, guard with `isPlaying` or a cooldown timer.
