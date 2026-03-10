# Parallax System

Reference for parallax background layers, configuration, and infinite scrolling.

---

## Layer Configuration

| Layer | Parallax Factor | Z Position | Example |
|-------|-----------------|------------|---------|
| Far Sky | 0.1 | 50 | Clouds, sun/moon |
| Far Mountains | 0.3 | 40 | Distant terrain |
| Mid Mountains | 0.5 | 30 | Closer hills |
| Near Trees | 0.7 | 20 | Forest edge |
| Foreground | 0.9 | 10 | Close foliage |
| Game Layer | 1.0 | 0 | Player, platforms |

**Parallax factor meaning:**
- `0.0` = completely static (pinned to world)
- `0.5` = moves at half the camera speed (mid-distance feel)
- `1.0` = moves with the camera (same plane as gameplay)

---

## Parallax Calculation

Applied in LateUpdate after the camera has moved:

```csharp
// In LateUpdate after camera moves
Vector3 delta = camera.position - previousCameraPosition;
float parallax = 1f - layer.parallaxFactor;
layer.transform.position += new Vector3(delta.x * parallax, delta.y * parallax, 0f);
```

**Why `1 - factor`:** A factor of 0.1 (far away) means the layer should move very little relative to the camera. The layer moves by `delta * 0.9` in world space, but since the camera also moved by `delta`, the apparent movement relative to the camera is `delta * 0.1`.

---

## Infinite Scrolling

For layers that must tile seamlessly (sky, repeating terrain):

```csharp
private void CheckWrapping()
{
    float spriteWidth = spriteRenderer.bounds.size.x;
    float cameraX = camera.transform.position.x;
    float layerX = transform.position.x;

    // If camera has moved far enough, reposition the layer
    if (Mathf.Abs(cameraX - layerX) >= spriteWidth)
    {
        float offset = (cameraX - layerX) % spriteWidth;
        transform.position = new Vector3(cameraX + offset, transform.position.y, transform.position.z);
    }
}
```

**Requirements for infinite scrolling:**
- Sprite must tile seamlessly at left/right edges
- Use at least 3 copies of the sprite (left, center, right) for full coverage
- Wrapping check runs after parallax movement, not before

---

## Layer Setup Checklist

When adding a new parallax layer:

1. Set the sprite's **Sorting Layer** and **Order in Layer** appropriately
2. Assign the **Parallax Factor** on the `ParallaxLayer` component
3. Set the **Z Position** to match the depth table above
4. Ensure the sprite is **wide enough** to cover the camera at max aspect ratio (21:9)
5. If the layer repeats, enable infinite scrolling and verify seamless tiling
6. Register the layer with `ParallaxBackgroundManager`
