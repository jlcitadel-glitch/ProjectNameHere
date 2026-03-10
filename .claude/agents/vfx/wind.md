# Wind Integration

WindManager is the global wind system that provides consistent wind values for all VFX. It is **owned by the systems agent** -- the VFX agent is a read-only consumer.

**Script:** `Assets/_Project/Scripts/Systems/Core/WindManager.cs`

## WindManager API (read-only for VFX)

```csharp
// Singleton access
WindManager.Instance

// Properties VFX systems use:
WindManager.Instance.CurrentWindVector    // Vector2: direction * (baseStrength + gustValue)
WindManager.Instance.WindDirection         // Vector2: normalized direction
WindManager.Instance.CurrentStrength       // float: baseStrength + currentGustValue
WindManager.Instance.TurbulenceStrength    // float: 0 if disabled, else turbulenceStrength
WindManager.Instance.TurbulenceScale       // float: Perlin noise scale

// Per-position turbulence (for per-particle variation)
WindManager.Instance.GetTurbulenceAt(Vector2 position)  // Returns Vector2 offset

// Trigger a gust (used by gameplay events, not typically VFX)
WindManager.Instance.TriggerGust(float strength = -1f)
```

## How WindManager Works

- **Singleton** with `DontDestroyOnLoad`
- **Base wind**: configurable direction + strength
- **Gusts**: sinusoidal ramp-up/hold/ramp-down cycle. Frequency and strength configurable. Random cooldown between gusts.
- **Turbulence**: Perlin noise at configurable scale, sampled per-position. Returns a 2D offset vector.

## windInfluenceMultiplier Pattern

Every VFX system that reads wind should multiply by a preset-specific influence value:

```csharp
// In PrecipitationPreset:
[Range(0f, 3f)]
public float windInfluenceMultiplier = 1f;

[Range(0f, 2f)]
public float turbulenceInfluenceMultiplier = 1f;

// Usage in PrecipitationController:
Vector2 windVector = WindManager.Instance.CurrentWindVector * currentPreset.windInfluenceMultiplier;
```

This lets each effect type respond to wind differently:
- **Heavy rain**: `windInfluenceMultiplier = 1.5` (strongly affected)
- **Snow**: `windInfluenceMultiplier = 1.0` (moderately affected)
- **Embers**: `windInfluenceMultiplier = 0.5` (lightly affected, already has upward motion)

## VFX Systems That Read Wind

### PrecipitationController

Two wind integration paths:

**GPU path** (`useGPUMotion = true`, default):
- Updates velocity module curves every 100ms (`WIND_UPDATE_INTERVAL`)
- Sets `velocityModule.x` to wind range, `velocityModule.y` to fall speed + wind
- Uses noise module for drift + turbulence influence

**CPU path** (`useGPUMotion = false`):
- `LateUpdate()` iterates every particle via `GetParticles`/`SetParticles`
- Per-particle: sinusoidal drift + wind vector + Perlin noise turbulence
- Lerps velocity toward target for smooth transitions
- More precise but more expensive

### ParticleFogSystem

Currently uses its **own** `windDirection` and `windStrength` fields, NOT WindManager. This is a known inconsistency. A future improvement should read from `WindManager.Instance.CurrentWindVector` instead.

The fog system's `LateUpdate()` applies wind + Perlin turbulence per-particle, similar to the precipitation CPU path.

### AtmosphericAnimator

Does NOT read wind. Uses Perlin noise for self-contained drift. Could be enhanced to add `WindManager.Instance.CurrentWindVector * windInfluenceMultiplier` to the drift offset.

## Cross-Agent Reference

| What | Who |
|---|---|
| WindManager.cs ownership, bug fixes, API changes | **systems agent** |
| Reading wind values in VFX scripts | **vfx agent** |
| Triggering gusts from gameplay events | **player/enemy/systems agent** |
| Adding windInfluenceMultiplier to new VFX presets | **vfx agent** |

**Rule:** Never modify `WindManager.cs` from the VFX agent. If you need a new API (e.g., wind zone overrides), file a bead for the systems agent: `bd create "WindManager: add zone-local wind override API" -p 3 -l agent:systems`.
