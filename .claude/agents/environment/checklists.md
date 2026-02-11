# Checklists

> Review criteria, common issues, and troubleshooting for environment work.

## Pre-Commit Review

- [ ] All platforms use Kinematic Rigidbody2D with `MovePosition()` in FixedUpdate
- [ ] All hazards respect HealthSystem invulnerability frames
- [ ] All interactables work with both gamepad and keyboard/mouse
- [ ] Tilemaps use CompositeCollider2D (no individual tile colliders)
- [ ] No hardcoded layer numbers — all use serialized LayerMask
- [ ] ScriptableObjects used for config (not hardcoded values on prefabs)
- [ ] Destructible/interactable state has a persistent ID for save/load
- [ ] No `Find()` or `GetComponent()` in Update loops
- [ ] VFX and audio triggered through proper systems (not inline)

## Common Issues

### Player Falls Through Moving Platform
- Platform must be Kinematic Rigidbody2D
- Move in FixedUpdate, not Update
- Player should parent to platform on contact

### Player Catches on Tile Edges
- Ground tilemap MUST use CompositeCollider2D
- Individual TilemapCollider2D tiles create seams between edges
- Check that compositeOperation is set to Composite

### One-Way Platform Won't Let Player Through
- Check PlatformEffector2D surface arc (should be ~180)
- Ensure collider has `usedByEffector = true`
- Verify Rigidbody2D is Static

### Hazard Hits During Invulnerability
- Always check `HealthSystem` invulnerability state before applying damage
- Or let HealthSystem handle it internally (preferred)

### Interactable Prompt Stuck On Screen
- Ensure `OnTriggerExit2D` hides the prompt
- Handle edge case: player dies while in range (subscribe to death event)

### Breakable Wall Doesn't Break
- Check required ability matches what player has
- Verify hit detection collider is on correct layer
- Ensure `hitsToBreak` count is reachable

### Moving Platform Jitters
- Must use `rb.MovePosition()`, not `transform.position`
- Movement must be in `FixedUpdate`, not `Update`
- Check that interpolation is set on the Rigidbody2D

## Testing Checklist

- [ ] Walk across all platform types — no falling through
- [ ] Jump through one-way platforms from below — passes through
- [ ] Drop through one-way platforms with input — works
- [ ] Touch each hazard type — correct damage, respects i-frames
- [ ] Interact with every interactable — prompt shows/hides, action fires
- [ ] Break all destructibles — VFX plays, path opens
- [ ] Save and reload — all interactable states persist
- [ ] Test with both gamepad and keyboard — all interactions work
