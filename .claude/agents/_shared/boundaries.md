# Cross-Agent Boundary Map

> When a task touches two domains, this map defines who owns what. The **primary owner** makes the change; the **collaborator** reviews or provides guidance.

## Player + Combat

| Concern | Owner | Collaborator |
|---------|-------|--------------|
| Player movement, jumping, dashing | **player** | — |
| Player attack execution (CombatController) | **player** | enemy-behavior (for hit reactions) |
| Attack data (WeaponData, AttackData SOs) | **player** | systems (if save/load involved) |
| Parry system (ParrySystem, ParryData) | **player** | enemy-behavior (for parry windows) |
| Player damage reception (HealthSystem.TakeDamage) | **systems** | player (for invulnerability frames) |
| Hit effects on player (PlayerHurtVFX) | **vfx** | player (for timing/triggers) |

**Why this split:** Combat is initiated by the player agent but damage/health is a system concern. VFX are always owned by vfx agent regardless of who triggers them.

## Player + Environment

| Concern | Owner | Collaborator |
|---------|-------|--------------|
| Player ground detection | **player** | environment (layer setup) |
| Platform interactions (one-way, moving) | **environment** | player (for edge cases like dash-through) |
| Hazard damage (spikes, pits) | **environment** | systems (HealthSystem) |
| Physics layers that affect player | **environment** | player (must test after changes) |

**Why this split:** The environment defines what exists in the world; the player defines how the character interacts with it. Layer changes by environment can break player ground detection, so environment must notify player agent.

## Systems + Everyone

| Concern | Owner | Collaborator |
|---------|-------|--------------|
| GameManager state transitions | **systems** | all (everyone reads GameState) |
| HealthSystem / ManaSystem | **systems** | player, enemy-behavior (both use them) |
| SaveManager (save/load) | **systems** | player (position), abilities (unlocks) |
| Events (C# events, SO channels) | **systems** | all (consumers subscribe) |
| Manager initialization order | **systems** | — |
| New singleton managers | **systems** | architect (review cross-system impact) |

**Why this split:** Systems is the foundation layer. Other agents consume system services but never modify them without systems agent review.

## UI + Systems

| Concern | Owner | Collaborator |
|---------|-------|--------------|
| HUD displays (health bar, mana bar) | **ui-ux** | systems (data source) |
| Menu flow (main menu, pause, options) | **ui-ux** | systems (GameManager state) |
| Save slot UI | **ui-ux** | systems (SaveManager API) |
| Skill tree UI | **ui-ux** | player (SkillManager data) |
| Damage numbers | **ui-ux** | systems (damage events) |

**Why this split:** UI owns all visual presentation. Systems owns the data and events that UI subscribes to.

## VFX + Combat

| Concern | Owner | Collaborator |
|---------|-------|--------------|
| Hit impact effects (SkillHitVFX) | **vfx** | player (trigger timing) |
| Enemy death particles (EnemyDeathVFX) | **vfx** | enemy-behavior (death event) |
| Boss phase VFX (BossVFXController) | **vfx** | enemy-behavior (phase transitions) |
| Knockback visual (KnockbackVFX) | **vfx** | player, enemy-behavior (force data) |
| Screen flash on big hits | **vfx** | systems (damage threshold events) |

**Why this split:** VFX agent owns all particle/visual effects. Combat agents (player, enemy-behavior) trigger them via events but never implement the visuals.

## Sound + Everyone

| Concern | Owner | Collaborator |
|---------|-------|--------------|
| SFXManager, MusicManager | **sound-design** | — |
| UI sounds (UISoundBank) | **sound-design** | ui-ux (which events need sound) |
| Enemy audio clips (in EnemyData) | **sound-design** | enemy-behavior (which states play what) |
| Audio volume settings | **sound-design** | ui-ux (options menu sliders) |

## Camera + Player/Environment

| Concern | Owner | Collaborator |
|---------|-------|--------------|
| Camera follow behavior | **camera** | player (target transform) |
| Parallax backgrounds | **camera** | environment (layer art) |
| Camera bounds/zones | **camera** | environment (zone triggers) |
| Boss room camera lock | **camera** | enemy-behavior (boss triggers) |
| Camera shake | **camera** | vfx, systems (who triggers it) |

## Handoff Checklist

When your task crosses a boundary:
1. Check this map for ownership
2. If you're the collaborator, file a bead for the owner: `bd create "Cross-agent: <description>" -p 2 -l agent:<owner>`
3. If you're the owner and need collaborator input, note it in your bead and proceed
4. Never modify scripts outside your owned directories without explicit user approval
