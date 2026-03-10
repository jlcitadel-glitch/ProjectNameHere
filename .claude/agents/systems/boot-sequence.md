# Boot Sequence

## CRITICAL: SystemsBootstrap Is Not In Any Scene

`SystemsBootstrap.cs` exists at `Assets/_Project/Scripts/Systems/Core/SystemsBootstrap.cs` with `[DefaultExecutionOrder(-1000)]`, but it is **NOT placed in any scene**. It does nothing at runtime.

**MainMenuController** bootstraps all managers instead, using `EnsureXManager()` helper methods that check for an existing instance before creating one.

---

## Initialization Order

All managers are bootstrapped by MainMenuController on the MainMenu scene:

```
1. GameManager      (EnsureGameManager)
2. SaveManager      (EnsureSaveManager)
3. UIManager        (EnsureUIManager)
4. MusicManager     (EnsureMusicManager)
```

Each manager uses the persistent singleton pattern:
```csharp
if (Instance != null && Instance != this) { Destroy(gameObject); return; }
Instance = this;
DontDestroyOnLoad(gameObject);
```

After scene transition to gameplay, managers persist via DontDestroyOnLoad.

---

## DontDestroyOnLoad Persistence

Objects marked DontDestroyOnLoad survive scene loads but:
- Their references to scene objects become null after the old scene unloads.
- Their event subscriptions to destroyed scene objects are silently removed from delegate chains.
- They must re-discover scene objects after each scene load (via `SceneManager.sceneLoaded` callback or by waiting for new scene objects to register themselves).

---

## Adding a New Manager

When adding a new persistent manager:

1. Create the script with the persistent singleton pattern.
2. Add an `EnsureNewManager()` method to MainMenuController.
3. Call it in MainMenuController's init sequence **in the correct position** relative to its dependencies.
4. File a bead for the UI agent if the manager has any HUD/menu integration: `bd create "Integrate NewManager with UI" -p 2 -l agent:ui`.
5. Document the new init position in this file.

**Risk:** Forgetting step 2 means the manager never gets created. SystemsBootstrap will NOT save you — it is not in any scene.

### EnsureNewManager() Example

```csharp
private void EnsureNewManager()
{
    if (NewManager.Instance == null)
    {
        var go = new GameObject("NewManager");
        go.AddComponent<NewManager>();
    }
}
```

---

## ScriptableObject Lookup Gotcha

`Resources.FindObjectsOfTypeAll<T>()` only finds assets that are **already loaded in memory**. At main menu time, most assets are not loaded.

**Affected pattern:** `CharacterCreationController.FindJobData()` uses this call and fails to find JobClassData assets because they live in `ScriptableObjects/Skills/Jobs/`, not in a `Resources/` folder.

**Rule:** Any ScriptableObject that must be discoverable at runtime needs to be either:
- In a `Resources/` folder (loaded via `Resources.Load<T>()`)
- Directly referenced by a serialized field in a loaded scene
- Addressable (if using Addressables)

Current `Resources/` layout:
```
Assets/_Project/Resources/
├── Equipment/    # EquipmentData assets
├── Jobs/         # JobClassData assets (Warrior, Mage, Rogue, Beginner)
└── UISoundBank   # UI sound effects bank
```

---

## Common Issues

### Null Reference on Manager Access in Gameplay Scene
**Root cause:** Code calls `SomeManager.Instance` in Awake, but the manager has not been created yet because MainMenuController has not run (e.g., starting Play Mode directly in the gameplay scene during development).
**Fix:** Either add a bootstrap prefab to the gameplay scene for editor-only testing, or guard with `if (SomeManager.Instance == null) return;` in early lifecycle methods.

### Manager Event Subscriptions Break After Scene Load
**Root cause:** A persistent manager subscribed to events from scene objects. When the scene unloads, those objects are destroyed and the subscriptions vanish. The new scene's objects have not registered yet.
**Fix:** Persistent managers should subscribe to `SceneManager.sceneLoaded` and re-discover scene objects there. Alternatively, have scene objects register themselves with the manager in their OnEnable.

### New Manager Not Created
**Root cause:** Added the script but forgot to add `EnsureNewManager()` in MainMenuController.
**Fix:** Follow the "Adding a New Manager" checklist above. SystemsBootstrap is a trap — it looks like it should handle this but it is not active.
