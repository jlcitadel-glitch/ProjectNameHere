# Environment — Eval Prompts

## Prompt 1: "Add crumbling platforms that fall after the player stands on them"
**Tests:** Platform implementation, physics rules, data-driven config
**Assertions:**
- Should use Rigidbody2D (Kinematic → Dynamic transition)
- Should use ScriptableObject for timing config (delay before crumble, respawn time)
- Should use trigger collider for player detection
- Should reference implementation/physics.md patterns
- Should consider save state persistence (does platform reset on room re-enter?)

## Prompt 2: "Player clips through tilemap corners"
**Tests:** Physics knowledge, tilemap-specific gotcha
**Assertions:**
- Should identify CompositeCollider2D as the solution (merges tile colliders)
- Should check Tilemap Collider2D "Used by Composite" checkbox
- Should verify Rigidbody2D is set to Static on tilemap
- Should reference terrain.md for tilemap setup
- Should NOT suggest modifying player physics as first approach

## Prompt 3: "Add a lever that opens a gate in another room"
**Tests:** Interactable pattern, cross-room state, save persistence
**Assertions:**
- Should use trigger-based interaction pattern
- Should persist lever state via SaveManager
- Should design data-driven (ScriptableObject or serialized config)
- Should handle scene transitions (gate may be in different scene)
- Should reference elements/interactables.md

## Prompt 4: "Spikes should damage the player but not enemies"
**Tests:** Physics layer knowledge, collision filtering
**Assertions:**
- Should use physics layers for collision filtering
- Should use serialized LayerMask (not hardcoded layer numbers)
- Should damage through HealthSystem.TakeDamage()
- Should reference implementation/physics.md
- Should NOT modify HealthSystem (systems domain)

## Prompt 5: "Moving platform jitters when player rides it"
**Tests:** Physics debugging, known patterns
**Assertions:**
- Should check MovePosition in FixedUpdate (not transform manipulation)
- Should verify platform Rigidbody2D is Kinematic
- Should consider player-platform parenting or velocity matching
- Should check interpolation settings on both Rigidbody2Ds
- Should reference camera agent if jitter is visual-only (camera smoothing issue)
