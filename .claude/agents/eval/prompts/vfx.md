# VFX — Eval Prompts

## Prompt 1: "Add a rain-to-snow transition effect"
**Tests:** Precipitation system knowledge, preset handling
**Assertions:**
- Should use PrecipitationPreset ScriptableObjects for both rain and snow
- Should crossfade between presets (not hard swap)
- Should respect wind integration (WindManager influence)
- Should use zone-based activation pattern
- Should reference performance limits (particle counts for rain vs snow)

## Prompt 2: "Particles are invisible after upgrading to URP"
**Tests:** Known gotcha awareness, shader knowledge
**Assertions:**
- Should immediately check particle material/shader
- Should specify correct shader: "Universal Render Pipeline/Particles/Unlit"
- Should check sorting layer and order
- Should provide fallback shader: "Sprites/Default"
- Should NOT suggest non-URP shaders

## Prompt 3: "Boss phase transition needs dramatic visual feedback"
**Tests:** Cross-agent coordination, VFX system knowledge
**Assertions:**
- Should use BossVFXController for boss-specific effects
- Should combine multiple effects (screen flash + particles + camera shake)
- Should coordinate with enemy-behavior (phase transition event)
- Should coordinate with camera (shake) and sound-design (audio)
- Should file cross-agent beads, NOT implement camera shake directly

## Prompt 4: "Fog system tanks FPS in large rooms"
**Tests:** Performance awareness, optimization knowledge
**Assertions:**
- Should check particle count against limits (fog wisps: max 50, 3/s)
- Should verify LateUpdate particle manipulation isn't too complex
- Should suggest larger particles with lower count
- Should check simulation space (World vs Local)
- Should cache particle arrays to avoid per-frame allocation

## Prompt 5: "Add hit sparks when player sword hits enemy"
**Tests:** Hit effect pattern, ownership awareness
**Assertions:**
- Should create effect as VFX-owned script (e.g., MeleeHitVFX)
- Should trigger from combat event (not poll for hits)
- Should use SelfDestructVFX pattern for cleanup
- Should spawn at hit point with correct sorting layer
- Should coordinate with player/combat for trigger timing (not own the trigger)
