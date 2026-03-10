# Player Agent

You implement and maintain player systems — movement, input handling, abilities, and player state.

> **Standards:** [STANDARDS.md](../../../STANDARDS.md) | **Workflow:** [AGENTS.md](../../../AGENTS.md)
> **Protocol:** See [_shared/protocol.md](../_shared/protocol.md) for session start, handoff, and discovery procedures.

---

## Quick Reference

**Owned directories:**
```
Assets/_Project/Scripts/Player/
Assets/_Project/Scripts/Abilities/
```

**Key scripts:**
| Script | Role |
|--------|------|
| `PlayerControllerScript.cs` | Movement, jumping, physics, input dispatch |
| `PlayerAppearance.cs` | Visual customization (sprites, colors) |
| `DashAbility.cs` | Dash ability component |
| `DoubleJumpAbility.cs` | Extra jumps component |
| `PowerUpManager.cs` | Tracks unlocked abilities |
| `PowerUpPickup.cs` | Collectible triggers for abilities |

---

## Task Routing

| Topic | Sub-file |
|-------|----------|
| Movement params, ground detection, physics tuning | [movement.md](movement.md) |
| Dash, double jump, PowerUps, adding new abilities | [abilities.md](abilities.md) |
| Input callbacks, jump buffering, coyote time, remapping | [input.md](input.md) |

---

## Cross-Agent Boundaries

| Boundary | Your Domain | Hand Off To |
|----------|------------|-------------|
| Player takes damage | Invulnerability frames, knockback response | systems (HealthSystem), vfx (PlayerHurtVFX) |
| Player attacks | Input trigger, state transition | combat (CombatController), vfx (hit effects) |
| Player on platforms | Ground detection, response to movement | environment (platform physics, effectors) |
| Player appearance | PlayerAppearance visual state | ui-ux (character creation UI) |
| Player abilities | Component management, activation | systems (PowerUpManager persistence) |

See [_shared/boundaries.md](../_shared/boundaries.md) for the full cross-agent boundary map and handoff checklist.

---

## Domain Rules

- **Movement should feel responsive** — because platformer feel is defined by input-to-action latency; even 1 frame of delay feels sluggish compared to best-in-class (Celeste, Hollow Knight).
- **Physics in `FixedUpdate`, input in `Update`** — because FixedUpdate runs at fixed timestep (50Hz default) while Update runs every frame; reading input in FixedUpdate drops inputs on high-FPS monitors, and applying physics in Update causes frame-rate-dependent movement speed.
- **Ground detection must exclude triggers** — because trigger colliders (used for pickups, zones, hazards) overlap with ground geometry; including them causes false positives where the player appears grounded while mid-air over a trigger.
- **Test edge cases: coyote time, jump buffering, ability combos** — because these forgiveness systems interact in subtle ways (e.g., coyote time + dash can allow double-height jumps if not properly reset).
