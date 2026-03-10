# Precipitation System

Zone-based particle precipitation with ScriptableObject presets. Create presets via **Assets > Create > Game > Precipitation Preset**.

## Architecture

```
PrecipitationPreset (ScriptableObject)
    Defines type, intensity, size, speed, drift, wind influence, fade, collision
    |
PrecipitationController (MonoBehaviour, requires ParticleSystem)
    Configures ParticleSystem modules from preset at runtime
    Handles bounds (FollowCamera / WorldFixed / UseCollider)
    Wind integration via velocity curves (GPU) or per-particle iteration (CPU)
    |
PrecipitationLayer (MonoBehaviour, requires PrecipitationController)
    Creates back/mid/front parallax layers with size/speed/alpha multipliers
    |
PrecipitationZone (MonoBehaviour, requires Collider2D)
    Trigger-based enable/disable with smooth transitions
    Supports UseGlobalController or UseLocalController modes
    |
IndoorZone (MonoBehaviour, requires Collider2D)
    Disables precipitation when player enters (caves, buildings, overhangs)
    Tracks which controllers it disabled, re-enables only those on exit

Editor:
    PrecipitationPresetEditor -- tiered inspector (Essential / Advanced / Expert)
```

## Scripts

| Script | Path | Purpose |
|---|---|---|
| PrecipitationPreset | `VFX/Precipitation/PrecipitationPreset.cs` | SO config: type, intensity, size, speed, drift, wind, fade, collision |
| PrecipitationController | `VFX/Precipitation/PrecipitationController.cs` | Runtime particle control, bounds modes, wind curves |
| PrecipitationLayer | `VFX/Precipitation/PrecipitationLayer.cs` | Multi-depth parallax (back/mid/front child ParticleSystems) |
| PrecipitationZone | `VFX/Precipitation/PrecipitationZone.cs` | Zone trigger, global/local controller mode, transitions |
| IndoorZone | `VFX/Precipitation/IndoorZone.cs` | Disables precipitation indoors, re-enables on exit |
| PrecipitationPresetEditor | `VFX/Precipitation/Editor/PrecipitationPresetEditor.cs` | Custom inspector with foldout tiers |

Assets: `Assets/_Project/ScriptableObjects/Precipitation/`

## Available Presets

| Preset | Type | Behavior |
|---|---|---|
| Rain_Light / Heavy / Storm | Rain | Fast fall, stretched billboard, increasing density and wind |
| Snow_Light / Heavy / Blizzard | Snow | Slow/floaty, billboard, increasing wind |
| Ash_Fall | Ash | Gray, slow descent |
| Spore_Drift | Spores | Green tint, very floaty |
| Pollen_Seeds | Pollen | Yellow, extremely slow |
| Dust_Debris | Dust | Brown, subtle |
| Embers / Embers_Rising | Embers | Orange, slight upward float |

## Zone Setup Pattern

1. Create an empty GameObject in the scene
2. Add a `BoxCollider2D` (set isTrigger = true)
3. Size the collider to cover the area where precipitation should appear
4. Add `PrecipitationZone` component
5. Set **Control Mode**: `UseLocalController` for area-specific, `UseGlobalController` for shared
6. Assign a `PrecipitationController` reference (or set up a global one)
7. Optionally assign a **Preset Override** to change weather when entering this zone

For indoor areas:
1. Add `BoxCollider2D` + `IndoorZone` on the covered area
2. IndoorZone will auto-disable any active precipitation controllers when the player enters

## Bounds Modes

| Mode | Use case |
|---|---|
| `FollowCamera` | Precipitation follows the camera viewport. Best for outdoor areas. |
| `WorldFixed` | Fixed world-space bounds. Best for specific rooms or areas. |
| `UseCollider` | Uses an attached Collider2D. Best for irregular shapes. |

## Configuration Tips

**Intensity** (0-1) scales both emission rate and max particles via multiplier curves:
- `GetEffectiveEmissionRate()` = `emissionRate * Lerp(0.1, 2.0, intensity)`
- `GetEffectiveMaxParticles()` = `maxParticles * Lerp(0.2, 2.0, intensity)`

**Wind influence**: Each preset has `windInfluenceMultiplier` (0-3) and `turbulenceInfluenceMultiplier` (0-2). Heavy rain should have high wind influence (~1.5), snow moderate (~1.0), embers low (~0.5).

**GPU vs CPU motion**: `useGPUMotion = true` (default) uses the noise module for drift and updates velocity curves every 100ms. Set `false` for CPU per-particle iteration (more precise turbulence, higher cost).

**Transitions**: `PrecipitationController.TransitionDuration` controls how fast emission ramps up/down. Zone transitions default to 1.5s. Use `immediate: true` for instant changes (scene loads).

**Parallax layers**: Add `PrecipitationLayer` alongside `PrecipitationController` for depth. Back layer uses 0.5x size, 0.6x speed, 0.4 alpha. Front layer uses 1.5x size, 1.4x speed, 0.9 alpha.
