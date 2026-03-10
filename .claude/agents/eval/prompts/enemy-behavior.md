# Enemy Behavior — Eval Prompts

## Prompt 1: "Create a new flying enemy that shoots projectiles"
**Tests:** Full enemy creation pipeline, data-driven design
**Assertions:**
- Should follow EnemyData SO + prefab pattern
- Should use FlyingMovement (zero gravity, SmoothDamp chase)
- Should configure EnemyAttackData for ranged (projectilePrefab, projectileSpeed)
- Should set SpriteRenderer correctly (Sprites-Default material, Ground sorting layer, order 10)
- Should reference existing prefab (Bat) as template

## Prompt 2: "Enemies are invisible after creating new prefab"
**Tests:** Known gotcha awareness, debugging strategy
**Assertions:**
- Should immediately suspect SpriteRenderer material/sorting layer
- Should reference the "Enemy Invisible" common issue
- Should provide exact fix values (Sprites-Default, Ground layer, order 10)
- Should follow "check config before code" debugging principle
- Should NOT suggest code changes first

## Prompt 3: "Boss doesn't transition to phase 2 at 50% HP"
**Tests:** Boss system knowledge, systematic debugging
**Assertions:**
- Should verify BossController component exists
- Should check HealthSystem.OnHealthChanged subscription
- Should verify phase HP thresholds match EnemyData
- Should use diagnostic logging (EnemyDiagnostic.cs)
- Should check one thing at a time

## Prompt 4: "Balance enemies for wave 50 vs wave 150"
**Tests:** Wave scaling formula knowledge, boundary awareness
**Assertions:**
- Should reference the exact scaling formulas (linear 1-100, accelerated 101+)
- Should calculate concrete stat values for wave 50 and 150
- Should note the 2x acceleration after wave 100
- Should consider maxAlive cap
- Should NOT modify core formulas without impact assessment

## Prompt 5: "Add a new sensor type: sound-based detection"
**Tests:** Extension patterns, existing architecture respect
**Assertions:**
- Should extend DetectionType enum
- Should add case to EnemySensors detection logic
- Should design as trigger-based (player footstep events?)
- Should reference existing sensor patterns (Radius, Cone, LineOfSight)
- Should consider cross-agent dependency (player emits sound events)
