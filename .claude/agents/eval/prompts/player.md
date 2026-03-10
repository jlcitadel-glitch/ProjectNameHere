# Player — Eval Prompts

## Prompt 1: "Add a wall-slide ability"
**Tests:** Ability pattern adherence, IAbility interface, cross-system awareness
**Assertions:**
- Should create component in Scripts/Abilities/
- Should follow the 5-step ability addition process
- Should reference IAbility interface pattern
- Should consider interaction with DashAbility (wall-slide cancel?)
- Should NOT modify environment scripts (hand off layer setup)

## Prompt 2: "Player falls through platforms when dashing"
**Tests:** Edge case reasoning, cross-agent boundary awareness
**Assertions:**
- Should investigate DashAbility physics (does it disable collider? change layer?)
- Should check PlatformEffector2D interaction with dash velocity
- Should consider coyote time state during dash
- Should reference environment agent for platform setup
- Should NOT modify environment/platform code directly

## Prompt 3: "Refactor PlayerControllerScript into a state machine"
**Tests:** RPI adherence, scope judgment, architectural awareness
**Assertions:**
- Should follow RPI (research current code first, plan before implementing)
- Should reference the state machine pattern from architect CLAUDE.md
- Should assess risk (PlayerControllerScript is the most coupled script)
- Should propose incremental migration, not big-bang rewrite
- Should identify what states map to existing code paths

## Prompt 4: "Ground detection sometimes misses when on moving platforms"
**Tests:** Physics knowledge, debugging approach
**Assertions:**
- Should check OverlapCircleAll timing (Update vs FixedUpdate mismatch?)
- Should verify groundCheck Transform position relative to moving platform
- Should check if platform movement in FixedUpdate matches detection timing
- Should reference groundLayer mask setup
- Should suggest diagnostic logging approach (compare position over time)

## Prompt 5: "Add gamepad rumble when the player takes damage"
**Tests:** Input system knowledge, cross-agent awareness
**Assertions:**
- Should use InputSystem haptics API (Gamepad.current.SetMotorSpeeds)
- Should hook into HealthSystem.OnDamageTaken event
- Should NOT own the HealthSystem event (systems domain)
- Should consider accessibility (option to disable)
- Should reference sound-design agent for audio feedback coordination
