# Sprint Plan — Multi-Day Implementation

## Status: CODE COMPLETE

All code-level tasks have been implemented. Remaining items require Unity Editor work (prefab creation, asset configuration, VFX particle systems, playtesting).

## Overview

~16 tasks spanning core gameplay fixes, new systems, audio, UI polish, and build verification. The codebase has robust infrastructure (enemy AI, wave spawning, combat, skills/SP, leveling, save system) — most work extends existing patterns.

**Critical bug (FIXED):** `LevelSystem.OnLevelUp` was NOT wired to `SkillManager.SetPlayerLevel()` — SP was never awarded on level-up. Now fixed in LevelSystem.cs.

## Agent Assignments

| Agent | Scope | Task Count |
|-------|-------|------------|
| Systems | Core systems, managers, save/load, data flow | 7 |
| Player | Enemy behavior, combat, boss, player integrations | 7 |
| UI-UX | Menus, HUD, fonts, options, character creation | 7 |
| VFX | Visual effects, particles, orb visuals | 3 |

## Priority Legend

- **P0** — Blocking / broken functionality
- **P1** — Core feature needed for milestone
- **P2** — Nice-to-have, polish

## Dependency Graph

```
Independent (start immediately):
  PLR-1  Enemy gravity fix
  SYS-1  LevelUp → SP wiring
  SYS-4  SFXManager
  SYS-6  Wave verification
  UI-2   Name input screen
  UI-5   Font audit
  UI-6   Credits screen

After SYS-1:
  SYS-2  XP orbs
  SYS-3  Stat system
  VFX-2  Level-up VFX

After SYS-3:
  SYS-5  SaveManager updates
  SYS-7  Class skills verification
  UI-3   Class selection (needs stat previews)
  PLR-6  AGI → player speed
  PLR-7  STR → combat damage

After SYS-4:
  UI-1   Fix SFX slider
  PLR-2  Combat SFX
  PLR-3  Enemy impact SFX

After SYS-2:
  VFX-1  XP orb visuals

After PLR-1:
  PLR-4  BossController
  PLR-5  Boss in WaveManager

After PLR-4:
  UI-7   Boss health bar
  VFX-3  Boss VFX

After UI-2:
  UI-3 → UI-4 (sequential flow)

After ALL:
  Build + verification
```

## Files to Create

| File | Agent | Purpose |
|------|-------|---------|
| `Scripts/Systems/Core/ExperienceOrb.cs` | Systems | XP orb pickup |
| `Scripts/Systems/Core/StatSystem.cs` | Systems | STR/INT/AGI system |
| `Scripts/Audio/SFXManager.cs` | Systems | SFX volume helper |
| `Scripts/UI/Menus/CreditsController.cs` | UI-UX | Credits screen |
| `Scripts/UI/Menus/CharacterCreationController.cs` | UI-UX | Character creation flow |
| `Scripts/Enemies/Core/BossController.cs` | Player | Boss behavior |
| `Scripts/UI/HUD/BossHealthBar.cs` | UI-UX | Boss HP bar |

All paths relative to `Assets/_Project/`.

## Completion Status

### Code Complete (All agents)

| ID | Task | Status |
|----|------|--------|
| SYS-1 | Wire LevelUp → SkillManager SP fix | DONE |
| SYS-2 | ExperienceOrb system | DONE |
| SYS-3 | StatSystem (STR/INT/AGI) | DONE |
| SYS-4 | SFXManager static helper | DONE |
| SYS-5 | SaveManager updates (v3) | DONE |
| SYS-6 | Wave 1-10 verification | DONE (reviewed, code is sound) |
| SYS-7 | Class skill assets | PENDING (Unity Editor — create SkillData assets) |
| PLR-1 | Enemy gravity fix | DONE |
| PLR-2 | Combat SFX fields + playback | DONE |
| PLR-3 | Enemy SFX via SFXManager | DONE |
| PLR-4 | BossController with phases | DONE |
| PLR-5 | Boss in WaveManager | DONE |
| PLR-6 | AGI → player speed | DONE |
| PLR-7 | STR → combat damage | DONE |
| UI-1 | Fix SFX volume slider | DONE |
| UI-2 | Character name input | DONE |
| UI-3 | Class selection screen | DONE |
| UI-4 | Appearance selection | DONE |
| UI-5 | Font audit + fix | DONE |
| UI-6 | Credits screen | DONE |
| UI-7 | Boss health bar | DONE |
| VFX-1 | XP orb visuals | PENDING (Unity Editor — prefab + particles) |
| VFX-2 | Level-up VFX | PENDING (Unity Editor — prefab + particles) |
| VFX-3 | Boss VFX | PENDING (Unity Editor — prefab + particles) |

### Remaining Unity Editor Work

1. **VFX Prefabs** — Create particle systems for XP orbs, level-up, boss effects
2. **SkillData Assets** — Create ScriptableObject assets for each class's starting skills
3. **JobClassData Assets** — Set strPerLevel/intPerLevel/agiPerLevel per class (Warrior: 3/1/1, Mage: 1/3/1, Rogue: 1/1/3)
4. **ExperienceOrb Prefab** — Create prefab with Rigidbody2D, CircleCollider2D, sprite
5. **Boss Prefab** — Create boss enemy prefab with BossController component
6. **UI Panels** — Create Canvas GameObjects for character creation, credits, boss health bar
7. **Audio Clips** — Source/create SFX clips and assign to AttackData assets
8. **WaveConfig Asset** — Assign bossPrefab, verify enemyPool entries

### Wave System Review Notes

- Enemy count scales 3→15 over waves 1-7, then caps
- HP scales 15%/wave (may be aggressive for late waves — consider reducing to 10%)
- Damage scales 10%/wave, speed 5%/wave — reasonable
- Boss waves every 5 waves, scaling inherits standard curve
- Recommend playtesting waves 7-10 for balance

## Verification Checklist

After all tasks complete:

- [ ] Main menu loads, credits accessible
- [ ] New game: name → class → appearance → gameplay
- [ ] Save/load works across all 5 slots with new fields
- [ ] Enemies fall with gravity, patrol correctly
- [ ] XP orbs drop from enemies, fly to player, award XP
- [ ] Level-up awards SP + stat points
- [ ] Skills learnable and usable from hotbar
- [ ] Stats allocatable via UI, affect gameplay
- [ ] Waves 1-10 progress with increasing difficulty
- [ ] Boss spawns at configured wave interval
- [ ] All SFX plays at correct volume, slider works
- [ ] Fonts consistent (Cinzel) throughout all UI
- [ ] Pause menu and options functional
- [ ] Windows build runs correctly
