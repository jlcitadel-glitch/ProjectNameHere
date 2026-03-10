# Sound Design — Eval Prompts

## Prompt 1: "Add footstep sounds that change based on terrain type"
**Tests:** SFXManager routing, data-driven audio, cross-agent awareness
**Assertions:**
- Should route through SFXManager.PlayOneShot()
- Should store clips in a ScriptableObject (e.g., SurfaceSoundBank)
- Should coordinate with player agent (footstep event timing)
- Should coordinate with environment agent (surface type detection)
- Should design for silence (null clip = no sound, no error)

## Prompt 2: "Music doesn't change when entering a boss room"
**Tests:** MusicManager knowledge, cross-system awareness
**Assertions:**
- Should check MusicManager.PlayTrack() is called on boss room entry
- Should verify boss room trigger fires event that MusicManager subscribes to
- Should check for duplicate playback prevention (same track already playing?)
- Should consider ducking during phase transitions
- Should reference GameManager.EnterBossFight() integration

## Prompt 3: "UI sounds play even when SFX volume slider is at zero"
**Tests:** Volume chain knowledge, PlayerPrefs keys
**Assertions:**
- Should trace volume chain: Audio_Master * Audio_SFX
- Should verify UISoundBank playback goes through SFXManager (not raw PlayOneShot)
- Should check UIButtonSounds component routes through correct API
- Should reference exact PlayerPrefs keys (Audio_Master, Audio_SFX)
- Should verify OptionsMenuController writes correct key names

## Prompt 4: "Add ambient cave sounds that fade in/out based on zone"
**Tests:** Zone-based audio pattern, cross-agent coordination
**Assertions:**
- Should use trigger-based zone activation (like VFX precipitation zones)
- Should crossfade between ambient tracks (not hard cut)
- Should store ambient clips in ScriptableObject
- Should coordinate with environment agent (zone placement)
- Should respect the volume chain (master * category)

## Prompt 5: "Enemy alert sound plays too many times when multiple enemies detect player"
**Tests:** Audio overlap management, practical debugging
**Assertions:**
- Should suggest cooldown timer for rapid-fire sounds
- Should consider priority system (only nearest enemy plays alert)
- Should check audioSource.isPlaying before playing
- Should store alert clip in EnemyData (not hardcoded)
- Should NOT modify enemy detection logic (enemy-behavior domain)
