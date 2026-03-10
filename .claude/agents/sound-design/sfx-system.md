# SFX System

## SFXManager (Static Helper)

`Assets/_Project/Scripts/Audio/SFXManager.cs`

Stateless utility class. Reads volume from PlayerPrefs on every call — no cached state to go stale.

### API

```csharp
public static class SFXManager
{
    // Returns Master * SFX volume from PlayerPrefs
    public static float GetVolume();

    // Plays a one-shot clip through an existing AudioSource at correct volume
    public static void PlayOneShot(AudioSource source, AudioClip clip);

    // Same as above with an additional local volume multiplier
    public static void PlayOneShot(AudioSource source, AudioClip clip, float volumeMultiplier);

    // Plays a clip at a world position (creates a temporary AudioSource)
    public static void PlayAtPoint(AudioClip clip, Vector3 position);
}
```

### Usage Examples

**Standard pattern — component with AudioSource:**
```csharp
[SerializeField] private AudioSource audioSource;

// In your ScriptableObject data:
public AudioClip hurtSound;

// At playback time:
SFXManager.PlayOneShot(audioSource, enemyData.hurtSound);
```

**World-position pattern — no AudioSource needed:**
```csharp
SFXManager.PlayAtPoint(collectSound, transform.position);
```

**With local volume multiplier:**
```csharp
SFXManager.PlayOneShot(audioSource, clip, 0.5f); // half the normal SFX volume
```

---

## Prefab Audio Rules

Every prefab that plays sound must follow this setup:

1. **AudioSource component** on the GameObject:
   - `playOnAwake = false` — sounds play only when triggered by code
   - `spatialBlend = 0` — 2D game, all sounds are non-spatial
   - `loop = false` (unless it is a looping ambient/music source)

2. **AudioClip references** live in ScriptableObject data (EnemyData, AttackData, etc.), NOT serialized directly on the prefab.

3. **Playback** always goes through `SFXManager.PlayOneShot()` so the volume chain is respected.

### ExperienceOrb Exception

`ExperienceOrb` (`Assets/_Project/Scripts/Systems/Core/ExperienceOrb.cs`) uses `SFXManager.PlayAtPoint()` instead of `PlayOneShot()`. This is correct because experience orbs are pooled/destroyed on collection and may not have a persistent AudioSource. `PlayAtPoint` creates a temporary AudioSource at the world position that auto-destructs after the clip finishes.

---

## Adding Sound to a New Prefab

1. Add an `AudioSource` component (playOnAwake off, spatialBlend 0).
2. Add an `AudioClip` field to the relevant ScriptableObject (or create one if the prefab is data-driven).
3. In the MonoBehaviour, call `SFXManager.PlayOneShot(audioSource, data.clipField)` at the trigger point.
4. Null-check is built into SFXManager — no need to guard externally, but do verify the clip is assigned in the Inspector.
