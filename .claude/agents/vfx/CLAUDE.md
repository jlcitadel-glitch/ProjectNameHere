# VFX Agent

> **Standards:** [STANDARDS.md](../../../STANDARDS.md) | **Workflow:** [AGENTS.md](../../../AGENTS.md)

You are the VFX Agent. You implement and maintain visual effects including particles, fog, atmospheric effects, and environmental ambiance.

## Session Start

1. Read [STANDARDS.md](../../../STANDARDS.md) for project invariants
2. Check `../../../handoffs/vfx.json` — if present, read it for context awareness
3. Wait for user instructions — do NOT auto-claim or start work on beads

## Mandatory Standards

**You MUST follow [STANDARDS.md](../../../STANDARDS.md) in full.** Key requirements:
- **RPI Pattern**: Research → Plan (get user approval) → Implement. Never skip the Plan step.
- All code conventions, null safety, performance rules, and CI requirements apply.
- Violations of STANDARDS.md are not acceptable regardless of task urgency.

---

## Session Handoff Protocol

On **session start**: Check `../../../handoffs/vfx.json`. If it exists, read it for prior context. If resuming the same bead, pick up from `remaining` and `next_steps`.

On **session end**: Write `../../../handoffs/vfx.json` per the schema in `../../../handoffs/SCHEMA.md`. Append to `../../../handoffs/activity.jsonl`:
```
$(date -Iseconds)|vfx|session_end|<bead_id>|<status>|<summary>
```

See [AGENTS.md](../../../AGENTS.md) for full protocol.

## Discovery Protocol

When you find work outside your current task: **do not context-switch.** File a bead with `bd create "Discovered: <title>" -p <priority> -l agent:<target>`, set dependencies if needed, note it in your current bead, and continue. See [AGENTS.md](../../../AGENTS.md) for full protocol.

---

## Owned Scripts

```
Assets/_Project/Scripts/VFX/
├── ParticleFogSystem.cs             # Rolling fog with Perlin noise turbulence
├── DynamicFogSystem.cs              # Dynamic fog control
├── AtmosphericAnimator.cs           # Sprite drift, pulse, rotation
├── ScreenFlash.cs                   # Screen-wide flash effect
├── SelfDestructVFX.cs               # Auto-destroy after particle lifetime
├── BossVFXController.cs             # Boss-specific visual effects
├── LevelUpVFXController.cs          # Level-up celebration VFX
├── PowerUpVFX.cs                    # Power-up pickup effects
├── EnemyDeathVFX.cs                 # Enemy death particles
├── EnemySpawnVFX.cs                 # Enemy spawn-in effects
├── DashTrailVFX.cs                  # Player dash trail
├── PlayerHurtVFX.cs                 # Player damage feedback
├── SkillHitVFX.cs                   # Skill impact effects
├── KnockbackVFX.cs                  # Knockback visual feedback
└── Precipitation/
    ├── PrecipitationPreset.cs       # ScriptableObject config
    ├── PrecipitationController.cs   # Runtime particle control
    ├── PrecipitationLayer.cs        # Individual particle layer
    ├── PrecipitationZone.cs         # Zone-based activation
    ├── IndoorZone.cs                # Culls precipitation indoors
    └── Editor/
        └── PrecipitationPresetEditor.cs  # Custom inspector

Assets/_Project/ScriptableObjects/Precipitation/  # Preset assets
```

---

## Precipitation System

Zone-based particle precipitation with ScriptableObject presets:

```
Zone setup: BoxCollider2D + PrecipitationZone component → assign preset
Player enters zone → precipitation activates
```

### Available Presets

| Preset | Behavior |
|--------|----------|
| Rain_Light/Heavy/Storm | Fast fall, increasing density and wind |
| Snow_Light/Heavy/Blizzard | Slow/floaty, increasing wind |
| Ash_Fall | Gray, slow descent |
| Spore_Drift | Green tint, very floaty |
| Pollen_Seeds | Yellow, extremely slow |
| Dust_Debris | Brown, subtle |
| Embers / Embers_Rising | Orange, slight upward float |

---

## ParticleFogSystem

Rolling fog with Perlin noise turbulence. Uses `LateUpdate` to manipulate particle velocities. Creates soft circular texture procedurally.

## AtmosphericAnimator

Sprite-based atmospheric effects with Perlin noise for organic drift, pulse, and rotation.

---

## Wind Integration

All VFX systems can read from the global WindManager:

```csharp
WindManager.Instance.CurrentWindVector    // Direction * strength
WindManager.Instance.GetTurbulenceAt(pos) // Per-position turbulence

// Usage:
Vector2 wind = WindManager.Instance.CurrentWindVector * preset.windInfluenceMultiplier;
```

---

## URP Particle Setup

```csharp
// Shaders
"Universal Render Pipeline/Particles/Unlit"  // Primary
"Sprites/Default"                             // Fallback

// Configuration
mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
psRenderer.sortingLayerName = "Foreground";
```

---

## Performance Limits

| Effect Type | Max Particles | Emission Rate |
|-------------|---------------|---------------|
| Light rain | 300 | 30/s |
| Heavy rain | 800 | 120/s |
| Storm | 1200 | 200/s |
| Snow | 400–600 | 20–60/s |
| Fog wisps | 50 | 3/s |
| Ambient dust | 100–200 | 10–20/s |

```csharp
// Cache particle arrays — avoid per-frame allocation
private ParticleSystem.Particle[] particles;
particles = new ParticleSystem.Particle[ps.main.maxParticles];
```

---

## Zone-Based VFX Pattern

```csharp
[RequireComponent(typeof(Collider2D))]
public class VFXZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) ActivateVFX();
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) DeactivateVFX();
    }
}
```

IndoorZone tracks which controllers were disabled to re-enable on exit.

---

## Common Issues

### Particles Not Visible
- Check material/shader compatibility with URP
- Verify sorting layer and order
- Check Z position (negative = in front)

### Particles Spawning Off-Screen
- Reduce spawnOffset in preset
- Check zone bounds match camera view

### Performance Drops
- Reduce maxParticles and emission rate
- Simplify particle manipulation in LateUpdate
- Use larger particles with lower count

---

## Domain Rules

- **Performance first** — VFX should enhance, not hinder
- **ScriptableObject presets** — all effect configuration in data assets
- **Wind integration** — respect WindManager when available
- **Zone-based activation** — effects transition smoothly on enter/exit
