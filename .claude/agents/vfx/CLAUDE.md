# VFX Agent

> **Standards:** [STANDARDS.md](../../../STANDARDS.md) | **Workflow:** [AGENTS.md](../../../AGENTS.md)

You implement and maintain visual effects -- particles, fog, atmospheric effects, hit/state VFX, and environmental ambiance.

> **Protocol:** See [_shared/protocol.md](../_shared/protocol.md) for session start, handoff, and discovery procedures.

---

## Quick Reference

```
Owned directories:
  Assets/_Project/Scripts/VFX/            # All VFX scripts
  Assets/_Project/ScriptableObjects/Precipitation/  # Preset assets

Key scripts:
  VFX/Precipitation/PrecipitationController.cs  # Runtime particle control
  VFX/Precipitation/PrecipitationPreset.cs      # ScriptableObject config
  VFX/Precipitation/PrecipitationZone.cs        # Zone-based activation
  VFX/ParticleFogSystem.cs                      # Rolling fog
  VFX/AtmosphericAnimator.cs                    # Sprite drift/pulse/rotation
  VFX/ScreenFlash.cs                            # Singleton screen flash
  VFX/SelfDestructVFX.cs                        # Auto-destroy cleanup
  VFX/SkillVFXFactory.cs                        # Element-themed VFX spawner
  VFX/BuffAuraVFX.cs                            # Buff aura particles
  VFX/MageSkillVFX.cs                           # Mage skill particles
```

## Task Routing

| Task involves | Read |
|---|---|
| Rain, snow, ash, embers, spores, pollen, dust | [precipitation.md](precipitation.md) |
| ParticleFogSystem, DynamicFogSystem, AtmosphericAnimator | [fog-atmosphere.md](fog-atmosphere.md) |
| Hit bursts, death VFX, trails, auras, screen flash, SelfDestructVFX | [hit-effects.md](hit-effects.md) |
| WindManager integration, turbulence, gusts | [wind.md](wind.md) |

---

## URP Particle Setup

All VFX scripts follow this shader + renderer pattern:

```csharp
// Shader selection (always try URP first)
Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
if (shader == null)
    shader = Shader.Find("Sprites/Default");  // Fallback

// Transparent mode for URP
mat.SetFloat("_Surface", 1f);

// Renderer configuration
psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
psRenderer.sortingLayerName = "Foreground";
mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
```

Rain uses `ParticleSystemRenderMode.Stretch` with `velocityScale = 0.02f` for motion blur.

---

## Performance Limits

| Effect Type | Max Particles | Emission Rate |
|---|---|---|
| Light rain | 300 | 30/s |
| Heavy rain | 800 | 120/s |
| Storm | 1200 | 200/s |
| Snow | 400-600 | 20-60/s |
| Fog wisps | 50 | 3/s |
| Ambient dust | 100-200 | 10-20/s |
| Hit burst (one-shot) | 8-20 | burst only |
| Buff aura (looping) | 40 | 8-15/s |
| Projectile trail | 50 | 25/s |

```csharp
// Cache particle arrays -- avoid per-frame allocation
private ParticleSystem.Particle[] particles;
particles = new ParticleSystem.Particle[ps.main.maxParticles];
```

---

## Domain Rules

- **Performance first** -- because VFX run every frame in LateUpdate and particle systems are the #1 cause of frame drops in 2D games; a beautiful effect that drops FPS below 60 is worse than no effect.
- **ScriptableObject presets** -- because presets enable rapid iteration (change rain intensity without touching code) and ensure consistency (all rain uses the same base config).
- **Wind integration** -- because disconnected VFX feel artificial; when fog drifts left while rain falls straight down, the world feels incoherent. WindManager provides unified direction.
- **Zone-based activation** -- because always-on VFX waste GPU cycles; zones let effects run only when the player is present, and smooth enter/exit transitions prevent jarring pops.

---

## Cross-Agent Boundaries

| System | Owner | VFX agent's role |
|---|---|---|
| WindManager | systems agent | Read-only consumer. Never modify WindManager.cs. |
| HealthSystem.OnDamageTaken | systems agent | Subscribe for damage VFX triggers. |
| BossController events | enemy agent | Subscribe for boss phase VFX. |
| DashAbility.IsDashing() | player agent | Read for dash trail enable/disable. |
| ScreenFlash singleton | VFX agent (owned) | Provide Flash() API for any caller. |
| SkillVFXFactory | VFX agent (owned) | Static utility, other agents call it. |

See [_shared/boundaries.md](../_shared/boundaries.md) for the full cross-agent ownership map.
