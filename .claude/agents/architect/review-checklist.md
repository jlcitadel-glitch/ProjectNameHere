# Code Review Checklist

> Use this checklist when reviewing code or PRs. Verify against [STANDARDS.md](../../../STANDARDS.md) plus the project-specific gotchas below.

## General Checklist

- [ ] Components have single responsibility
- [ ] No `Find()` calls in Update loops
- [ ] Events used for decoupled communication
- [ ] Layer masks serialized via `[SerializeField]`, not hardcoded ints
- [ ] Gizmos provided for spatial debugging (colliders, sensors, zones)
- [ ] New systems follow existing coordination patterns (see `patterns.md`)
- [ ] ScriptableObjects used for data-driven configuration
- [ ] Cross-system impact assessed (check `bd dep tree` and `system-map.md`)
- [ ] Unity 6 APIs used (`Rigidbody2D.linearVelocity`, not `.velocity`)
- [ ] Null safety: `TryGetComponent` over `GetComponent`, null checks before access

---

## Project-Specific Gotchas

### Triple Hardcode Pattern (Equipment)

When changing starter equipment, you MUST update all three locations or the equipment system breaks:

1. **`SkillManager.cs` ~line 228** -- `WireRuntimeStarterEquipment(warrior, ...)` runtime fallback IDs
2. **`CharacterCreationController.cs` ~line 1655** -- `WireStarterEquipmentIfMissing()` / `WireJobEquipment()` character creation IDs
3. **`CharacterCreationController.cs` ~line 1686** -- `WireEquipmentVisualParts()` equipmentId-to-partId visual map

Also update the class preview switch (~line 624) if new `EquipmentSlotType` values are added -- it maps EquipmentSlotType to BodyPartSlot for the preview renderer.

**How to spot it:** Any PR that touches `EquipmentData`, `JobClassData`, `starterEquipment`, or adds new equipment slots.

### ScriptableObject Lookup at Main Menu Time

`Resources.FindObjectsOfTypeAll<T>()` only finds assets **already loaded in memory**. At main menu time, most ScriptableObjects are NOT loaded unless they are:
- In a `Resources/` folder, OR
- Directly referenced by a loaded scene or another loaded asset

**Real example:** `CharacterCreationController.FindJobData()` uses `FindObjectsOfTypeAll<JobClassData>()` but the real assets live in `ScriptableObjects/Skills/Jobs/` (not a Resources folder), so they are never found. The controller falls back to blank runtime instances with no `starterEquipment`.

**How to spot it:** Any code using `FindObjectsOfTypeAll`, `Resources.FindObjectsOfTypeAll`, or similar reflection-based asset lookups. Verify the target assets are actually discoverable.

### Stale GUID Pattern

Equipment and appearance assets can have broken GUID references to BodyPartData if assets were reimported (e.g., after the ULPC import pipeline runs). The `.meta` file gets a new GUID but serialized references in other assets still point to the old one.

**How to spot it:** Visual bugs where equipment renders as invisible or uses wrong sprites. Verify `.meta` GUIDs match before trusting YAML references in ScriptableObject assets.

### Equipment Slot System Constraints

- **`EquipmentSlotType` enum:** Weapon(0), Armor(1), Legs(2), Accessory(3), Feet(4), Head(5), Hands(6)
- **`EquipmentManager.SLOT_COUNT`** must be >= max enum value + 1 (currently 7)
- **`ApplyEquipmentEffects`** maps: Armor->Torso, Legs->Legs, Feet->Feet, Weapon->WeaponFront, Head->Hat, Hands->Gloves

**How to spot it:** Any PR adding new equipment slot types. Must update the enum, SLOT_COUNT, and the visual mapping.

### CharacterCreationController Dual Paths

This controller has two initialization paths:
- **`CreateRuntimeUI` path** -- used when no Inspector-configured canvas exists. Runs `FindJobData()` and `WireStarterEquipmentIfMissing()`.
- **Inspector path** -- used when the controller exists in the scene with pre-wired references. Skips the runtime wiring.

**How to spot it:** Changes to character creation logic that only test one path. Both must be verified.

### Manager Bootstrap Order

`SystemsBootstrap` script exists but is NOT placed in any scene. `MainMenuController` bootstraps managers via `EnsureXManager()` methods instead. New singleton managers must be added to this bootstrap chain or they will not exist at main menu time.

**How to spot it:** Any new Manager singleton. Verify it gets created before anything references `Manager.Instance`.
