# Architect — Eval Prompts

## Prompt 1: "We need a dialogue system. Design it."
**Tests:** System design that integrates with existing architecture, cross-system awareness
**Assertions:**
- Should propose ScriptableObject-based dialogue data
- Should identify integration points (UI, SaveManager, GameManager state)
- Should follow RPI (plan before code)
- Should NOT propose modifying existing systems without impact assessment
- Should file cross-agent beads for UI and systems work

## Prompt 2: "The Triple Hardcode Pattern for equipment is fragile. How should we fix it?"
**Tests:** Knowledge of real project gotchas, refactoring guidance
**Assertions:**
- Should reference the three locations (SkillManager, CharacterCreationController x2)
- Should propose a single source of truth (likely on JobClassData SO)
- Should assess cross-system impact before proposing changes
- Should mention the ScriptableObject Lookup gotcha (Resources/ folder requirement)
- Should NOT propose a fix that introduces new hardcodes

## Prompt 3: "Review this code: `FindObjectOfType<HealthSystem>()` in an Update loop"
**Tests:** Standards enforcement, performance rules, Unity 6 API awareness
**Assertions:**
- Should flag deprecated API (FindObjectOfType → FindAnyObjectByType)
- Should flag performance issue (Find in Update loop)
- Should recommend caching in Awake/Start
- Should reference STANDARDS.md performance rules
- Should suggest event-based alternative

## Prompt 4: "Should we switch from our current singleton pattern to a service locator?"
**Tests:** Architectural judgment, pattern knowledge, scope awareness
**Assertions:**
- Should evaluate tradeoffs (singletons vs service locator vs DI)
- Should reference existing singleton pattern in codebase
- Should consider migration cost and risk
- Should NOT recommend a change without clear benefit justification
- Should reference SystemsBootstrap and manager init order

## Prompt 5: "The enemy system and player system both need buff/debuff tracking. Where should it live?"
**Tests:** Cross-system design, boundary awareness, DRY judgment
**Assertions:**
- Should propose a shared system (likely under Systems/)
- Should reference existing ActiveBuffTracker in Skills/Execution
- Should consider whether to extend existing or create new
- Should identify affected agents (player, enemy-behavior, systems)
- Should file beads for cross-agent implementation
