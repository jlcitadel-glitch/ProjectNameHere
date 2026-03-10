# Fog and Atmosphere Systems

## ParticleFogSystem

**Script:** `Assets/_Project/Scripts/VFX/ParticleFogSystem.cs`

Rolling fog with Perlin noise turbulence. Creates particle-based fog wisps that drift organically.

### How It Works

- Requires a `ParticleSystem` component (configures it in `Start()`)
- Creates a soft circular 128x128 procedural texture with radial alpha falloff
- `LateUpdate()` iterates particles, applying Perlin noise turbulence + wind force to each velocity
- Uses `SimulationSpace.Local` (fog moves with the GameObject)

### Configuration (serialized fields)

| Field | Default | Purpose |
|---|---|---|
| `windDirection` | (1, 0.2) | Base wind direction vector |
| `windStrength` | 2 | Wind force multiplier |
| `turbulence` | 1 | Perlin noise turbulence strength |

### Particle Setup (hardcoded in Start)

| Parameter | Value |
|---|---|
| Start size | 3-8 |
| Start lifetime | 10-20s |
| Start color | (0.7, 0.75, 0.8, 0.3) -- pale blue-gray, low alpha |
| Max particles | 50 |
| Emission rate | 3/s |
| Shape | Box 30x20 |
| Rotation | -30 to 30 deg/s over lifetime |
| Alpha gradient | 0 -> 1 at 0.2 -> 1 at 0.8 -> 0 |

### Integration Notes

- Does NOT currently read from WindManager. Uses its own `windDirection`/`windStrength` fields. A future improvement could integrate with WindManager.
- Material: `Sprites/Default` shader with procedural soft circle texture
- Cleans up material and texture in `OnDestroy()`

---

## AtmosphericAnimator

**Script:** `Assets/_Project/Scripts/VFX/AtmosphericAnimator.cs`

Sprite-based atmospheric effects with Perlin noise for organic drift, pulse, and rotation. Attach to any sprite that should drift and breathe.

### How It Works

- Requires a `SpriteRenderer` sibling
- `Update()` applies three independent behaviors (each toggleable):
  - **Drift**: Perlin noise moves the sprite around its start position
  - **Pulse**: Perlin noise oscillates sprite alpha between min/max opacity
  - **Rotation**: Constant rotation speed
- Each instance gets random Perlin offsets so multiple animators don't sync

### Configuration

| Field | Default | Purpose |
|---|---|---|
| `enableDrift` | true | Enable Perlin-driven position drift |
| `driftSpeed` | 0.5 | Noise sample speed |
| `driftAmount` | 1.0 | Maximum drift distance from start |
| `enablePulse` | true | Enable alpha oscillation |
| `pulseSpeed` | 0.3 | Noise sample speed for opacity |
| `minOpacity` | 0.4 | Lowest alpha value |
| `maxOpacity` | 0.7 | Highest alpha value |
| `enableRotation` | false | Enable constant rotation |
| `rotationSpeed` | 2 | Degrees per second |

### Use Cases

- Mist/haze sprites layered behind environment
- Glowing ambient effects (mushrooms, crystals)
- Floating dust motes or particles (sprite-based alternative to ParticleSystem)

---

## DynamicFogSystem

**Note:** No `DynamicFogSystem.cs` file exists in the codebase. The original CLAUDE.md referenced it, but only `ParticleFogSystem.cs` is implemented. If a dynamic fog system is needed (e.g., density changes based on player position or time), it would be a new script.

---

## ScreenFlash

**Script:** `Assets/_Project/Scripts/VFX/ScreenFlash.cs`

Singleton full-screen color flash overlay. Used by hit VFX, boss phase changes, level-up, power-up collection.

### API

```csharp
ScreenFlash.Instance.Flash(Color color, float duration);
```

- Creates a Canvas + Image at runtime if none exists
- `sortingOrder = 999` (always on top)
- Fades linearly over the duration
- `raycastTarget = false` (does not block input)

### Integration Pattern

Many VFX scripts call ScreenFlash as a secondary feedback layer:
- `PlayerHurtVFX` -- red flash on damage
- `BossVFXController` -- red flash on phase change
- `LevelUpVFXController` -- cyan flash on level-up
- `PowerUpVFX` -- color-matched flash on collection
