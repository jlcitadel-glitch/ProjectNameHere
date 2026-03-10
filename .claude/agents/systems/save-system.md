# Save System

## SaveManager (Singleton, Persistent)

PlayerPrefs + JSON serialization with versioned save format and multiple save slots.

### SaveData Structure

```csharp
[Serializable]
public class SaveData
{
    public int saveVersion;
    public float playerPositionX, playerPositionY;
    public int currentHealth, maxHealth;
    public List<string> unlockedAbilities;
    public List<string> collectedItems;
    public string lastCheckpointId;
    public float playTime;
}
```

### Integration Pattern

```csharp
// Saving
SaveManager.Instance.Save();

// Loading (two-step: load data, then apply)
if (SaveManager.Instance.Load())
    SaveManager.Instance.ApplyLoadedData();
```

SaveManager auto-saves/restores abilities from PowerUpManager.

---

## Save Slots

`SaveSlotInfo` holds metadata per slot (name, play time, last save date). SaveManager manages multiple slots indexed by int.

---

## Common Issues

### Save Data Corruption
**Root cause:** Unhandled exceptions during serialization leave partial JSON in PlayerPrefs. Also: adding new fields without incrementing `saveVersion` causes deserialization failures on old saves.
**Fix:** Always increment `saveVersion` when changing SaveData structure. Wrap save/load in try-catch. Validate data integrity on load. Keep a backup of the previous save before overwriting.

### Save Migration
**Root cause:** Old save files lack new fields, causing null references when code assumes they exist.
**Fix:** Implement migration in a `MigrateSaveData(SaveData data)` method that checks `saveVersion` and fills defaults for missing fields before the rest of the system touches the data.

### Race Conditions on Save/Load
**Root cause:** Auto-save triggers during a scene transition while managers are being destroyed/recreated.
**Fix:** Guard save operations with a `GameManager.CurrentState` check — never save during `Loading` or `GameOver` states. Use a `isSaving` flag to prevent concurrent save operations.
