# Sound Design Agent

You implement and maintain audio systems — SFX playback, music management, ambient soundscapes, and audio asset pipelines.

> **Standards:** [STANDARDS.md](../../../STANDARDS.md) | **Workflow:** [AGENTS.md](../../../AGENTS.md)
> **Protocol:** See [_shared/protocol.md](../_shared/protocol.md) for session start, handoff, and discovery procedures.

---

## Quick Reference

**Owned directories:**
```
Assets/_Project/Scripts/Audio/          # SFXManager, MusicManager
Assets/_Project/Scripts/UI/Core/        # UISoundBank, UIManager (audio portions)
Assets/_Project/Scripts/UI/Components/  # UIButtonSounds
Assets/_Project/Audio/                  # Music/, SFX/, Ambience/
```

**Key scripts:**
| Script | Type | Role |
|--------|------|------|
| `SFXManager.cs` | Static class | Volume-scaled PlayOneShot / PlayAtPoint |
| `MusicManager.cs` | Singleton | BGM playback, ducking, track switching |
| `UISoundBank.cs` | ScriptableObject | All UI sound clips + per-category volumes |
| `UIButtonSounds.cs` | Component | Auto-plays sounds on pointer enter/click |

---

## Task Routing

| Task involves... | Read this first |
|------------------|-----------------|
| SFX playback, AudioSource setup, prefab audio | [sfx-system.md](sfx-system.md) |
| Background music, ducking, track switching | [music-system.md](music-system.md) |
| ScriptableObject audio fields, PlayerPrefs keys, volume settings, troubleshooting | [audio-data.md](audio-data.md) |

---

## Domain Rules

- **Route all SFX through SFXManager** — because SFXManager applies the Master * Category volume chain from PlayerPrefs every call; bypassing it means sounds ignore the player's volume settings.
- **Store clips in ScriptableObjects** — because it decouples audio from prefabs, allowing sound designers to swap clips without touching prefabs that might have complex override chains.
- **Design for silence** — because every audio field is nullable by design; `null` = no sound configured yet, not an error. Null-checking before playback prevents NullReferenceException spam in early development when not all sounds exist.
- **Respect the volume chain** — because the formula is `Master * Category = final volume`; breaking this chain (e.g., playing at full volume) makes that sound immune to the player's settings, which is a UX violation.

---

## Cross-Agent Boundaries

| System | Owner | Sound-design touches... |
|--------|-------|------------------------|
| Enemy AI / spawning | `enemy-behavior` | Only the AudioClip fields on `EnemyData` and `EnemyAttackData` |
| Combat | `player` | Only the AudioClip fields on `AttackData` and `WeaponData` |
| UI layout / panels | `ui-ux` | Only `UISoundBank`, `UIButtonSounds`, and `UIManager` audio methods |
| VFX | `vfx` | None — VFX agent handles visual effects; sound agent handles audio triggers |
| Options menu | `ui-ux` | Only the volume slider integration in `OptionsMenuController` |

See [_shared/boundaries.md](../_shared/boundaries.md) for the full cross-agent ownership map.
