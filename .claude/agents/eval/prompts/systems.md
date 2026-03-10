# Systems — Eval Prompts

## Prompt 1: "Add an inventory system"
**Tests:** Manager design, save/load integration, cross-agent impact
**Assertions:**
- Should design as singleton or SO-based service
- Should integrate with SaveManager (new SaveData fields)
- Should consider init order relative to existing managers
- Should file cross-agent beads (UI for inventory screen, player for equipment)
- Should follow existing ScriptableObject data patterns

## Prompt 2: "SystemsBootstrap isn't in any scene — how do managers get created?"
**Tests:** Knowledge of actual boot sequence, project-specific gotcha
**Assertions:**
- Should explain that MainMenuController bootstraps via EnsureXManager() methods
- Should note SystemsBootstrap exists but is NOT placed in scene
- Should explain DontDestroyOnLoad persistence pattern
- Should identify the risk (new managers need manual bootstrap wiring)
- Should NOT assume SystemsBootstrap is active

## Prompt 3: "Players report save data corruption after the last update"
**Tests:** Save system knowledge, debugging approach, data safety
**Assertions:**
- Should check saveVersion compatibility
- Should investigate JSON deserialization failures
- Should propose migration strategy for save format changes
- Should consider backup/rollback mechanism
- Should NOT suggest deleting save data as first solution

## Prompt 4: "Add a quest system that persists across sessions"
**Tests:** Persistence design, event integration, scope management
**Assertions:**
- Should extend SaveData with quest state
- Should design quest data as ScriptableObjects
- Should integrate with existing event patterns (C# events or SO channels)
- Should consider GameState interactions (can quests advance during pause?)
- Should scope appropriately (systems owns data; UI owns display)

## Prompt 5: "HealthSystem.OnDamageTaken fires but the death screen doesn't appear"
**Tests:** Event chain debugging, cross-system awareness
**Assertions:**
- Should trace the full event chain: HealthSystem → GameManager.GameOver → UI
- Should check GameState transition (does GameOver set correct state?)
- Should verify UI subscription in OnEnable (might have unsubscribed)
- Should check Time.timeScale (GameOver sets it to 0 — does UI still work?)
- Should reference the "HealthSystem no longer fires Hurt trigger" gotcha
