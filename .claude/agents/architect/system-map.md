# System Map

> Full system hierarchy for the project. Use this to understand what exists, who owns what, and where boundaries fall.
> Cross-agent ownership details: [_shared/boundaries.md](../_shared/boundaries.md)

## Current Systems

```
Player                                    [owner: player]
    ├── PlayerControllerScript (Input, movement, jumping)
    ├── PlayerAppearance (visual customization)
    └── Abilities
        ├── DashAbility (component)
        ├── DoubleJumpAbility (component)
        ├── PowerUpManager (state tracker)
        └── PowerUpPickup (collectible trigger)

Combat System                             [owner: player, collab: enemy-behavior]
    ├── CombatController (attack execution, combos)
    ├── AttackHitbox (trigger-based melee damage)
    ├── Projectile + ProjectileVisual (ranged attacks)
    ├── ParrySystem + ParryType (parry mechanics)
    └── Data: AttackData, WeaponData, ParryData, CombatEnums

Skills System                             [owner: player, collab: systems, ui-ux]
    ├── Core: SkillManager, PlayerSkillController, SkillInstance, SkillCooldownTracker
    ├── Data: SkillData, SkillTreeData, SkillEffectData, JobClassData
    ├── Effects: BaseSkillEffect → Damage, Buff, Heal, Projectile
    ├── Execution: SkillExecutor, SkillProjectile, ActiveBuffTracker, PassiveSkillTracker
    └── Enums: SkillType, JobTier, DamageType

Camera System                             [owner: camera]
    ├── AdvancedCameraController (follow, look-ahead, bounds)
    ├── ParallaxBackgroundManager + ParallaxLayer (Z-depth layers)
    ├── BossRoomTrigger
    └── CameraBoundsTrigger

VFX System                                [owner: vfx]
    ├── ParticleFogSystem, DynamicFogSystem
    ├── AtmosphericAnimator
    ├── Precipitation (zone-based, preset-driven)
    ├── ScreenFlash, SelfDestructVFX
    └── Hit/State VFX: BossVFX, LevelUpVFX, PowerUpVFX, EnemyDeathVFX,
        DashTrailVFX, PlayerHurtVFX, SkillHitVFX, EnemySpawnVFX, KnockbackVFX

Enemy System                              [owner: enemy-behavior]
    ├── EnemyController (state machine coordinator)
    ├── BaseEnemyMovement → GroundPatrolMovement, FlyingMovement, HoppingMovement
    ├── EnemyCombat + EnemyAttackHitbox + EnemyProjectile
    ├── EnemySensors (Radius, Cone, LineOfSight)
    ├── EnemyHitFlash, EnemyDiagnostic
    ├── BossController (phase system)
    ├── Data: EnemyData, EnemyAttackData
    └── Spawning: WaveManager, WaveConfig, EnemySpawnManager,
        WaveScaler, EnemyStatModifier, SurvivalArena, Wave100Controller

Systems                                   [owner: systems]
    ├── GameManager (state machine, time control)
    ├── HealthSystem, ManaSystem, StatSystem, LevelSystem
    ├── SaveManager (versioned JSON, save slots)
    ├── ExperienceOrb, HighscoreManager
    ├── WindManager (global wind for VFX/physics)
    ├── SceneLoader, SystemsBootstrap
    └── Cutscene: CutsceneManager, CutsceneData, CutsceneUI

Audio                                     [owner: sound-design]
    ├── SFXManager (static, volume-scaled)
    ├── MusicManager (singleton, ducking)
    └── UISoundBank (ScriptableObject)

UI                                        [owner: ui-ux]
    ├── Core: UIManager, UIStyleGuide, GothicFrameStyle, FontManager, FocusManager
    ├── Menus: MainMenu, PauseMenu, OptionsMenu, CharacterCreation,
        Credits, Highscores, SaveSlotUI
    ├── HUD: HealthDisplay, ManaDisplay, PlayerStatBars, ResourceBarDisplay,
        ExpBarDisplay, LevelDisplay, BossHealthBar, NotificationSystem,
        DamageNumberSpawner, CastBar, WeaponIndicator, ComboCounter,
        LowHealthVignette, GameFrameHUD, DeathScreen
    ├── Skills: SkillTreePanel, SkillTreeController, SkillNodeUI,
        SkillConnectionLine, SkillTooltip, SkillHotbar, JobAdvancementPopup
    ├── Stats: StatMenuController
    ├── Components: TabbedMenuController, UIButtonSounds, UIAnimatedSprite,
        DisplaySettingsPanel
    └── Systems: DisplaySettings, SafeAreaHandler, AdaptiveCanvasScaler
```

## Recommended Future Systems

```
Audio System v2 (proposed)                [owner: sound-design, collab: architect]
    ├── AudioManager (unified)
    ├── SoundBank (ScriptableObject)
    └── AudioPoolManager
```

## High-Risk Boundary Zones

These areas frequently cause cross-agent issues:

- **Skills + UI + Systems** -- SkillManager, CharacterCreationController, and SkillTreePanel all touch JobClassData. Changes here require coordination across player, ui-ux, and systems agents.
- **Equipment pipeline** -- Spans SkillManager, CharacterCreationController, EquipmentManager, and BodyPartData assets. See the Triple Hardcode Pattern in `review-checklist.md`.
- **Manager initialization** -- SystemsBootstrap is NOT placed in any scene; MainMenuController bootstraps via `EnsureXManager()` methods. New singletons must follow this pattern or break main menu startup.
