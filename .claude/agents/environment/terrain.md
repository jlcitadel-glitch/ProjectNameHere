# Terrain & Tilemaps

> **Unity 6 2D** - Tilemap package with CompositeCollider2D for efficient collision.

## Tilemap Layers

```
Tilemap layers (back to front):
  Background  — decorative, no collision
  Ground      — main walkable terrain, CompositeCollider2D
  Foreground  — decorative overlay, no collision
```

- Ground tilemap uses `TilemapCollider2D` + `CompositeCollider2D` for efficient collision
- Set `TilemapCollider2D.compositeOperation = Composite` to merge into the composite
- Composite collider `geometryType` should be `Polygons` for most terrain

## Surface Types

Surface types drive gameplay feedback (footstep sounds, friction):

```csharp
public enum SurfaceType
{
    Stone,
    Wood,
    Ice,
    Metal,
    Dirt,
    Water
}
```

- Surface data can be stored per-tile via custom `TileBase` subclass or via trigger zones overlaid on terrain
- Sound agent uses surface type to select footstep SFX clips
- Ice surfaces can reduce player friction via `PhysicsMaterial2D`

## Composite Collider Setup

```csharp
// Ground tilemap requires these components:
// 1. TilemapCollider2D (compositeOperation = Composite)
// 2. Rigidbody2D (bodyType = Static)
// 3. CompositeCollider2D (geometryType = Polygons)

// NEVER use individual tile colliders — they cause physics seams
// where the player catches on tile edges
```

## Tilemap Rules

- One `Tilemap` per layer, one `Grid` parent per scene
- Decorative tilemaps: no colliders, lower sorting order
- Ground tilemap: always CompositeCollider2D, Static Rigidbody2D
- Avoid overlapping collision tilemaps — causes physics conflicts
