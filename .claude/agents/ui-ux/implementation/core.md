# Core Unity UI Implementation

> **Unity 6 + UGUI 2.0.0** - Screen Space canvases for menus/HUD, World Space for floating text only.

## Canvas Setup

```csharp
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Canvas References")]
    [SerializeField] private Canvas mainMenuCanvas;     // Screen Space - Overlay
    [SerializeField] private Canvas hudCanvas;          // Screen Space - Overlay (2D games)
    [SerializeField] private Canvas worldCanvas;        // World Space (damage numbers)
    [SerializeField] private Canvas pauseCanvas;        // Screen Space - Overlay (high sort)

    [Header("Canvas Groups for Transitions")]
    [SerializeField] private CanvasGroup mainMenuGroup;
    [SerializeField] private CanvasGroup hudGroup;

    private void Awake()
    {
        ConfigureCanvases();
    }

    private void ConfigureCanvases()
    {
        // For 2D games, Screen Space - Overlay is preferred (no camera reference needed)
        mainMenuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainMenuCanvas.sortingOrder = 100;

        // HUD also uses Overlay for 2D - simpler and no depth issues
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 10;

        // Pause menu above everything
        pauseCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        pauseCanvas.sortingOrder = 200;

        // World canvas for damage numbers attached to game objects
        worldCanvas.renderMode = RenderMode.WorldSpace;
        // Note: Scale world canvas appropriately for 2D (e.g., 0.01 for pixel-perfect)
    }
}
```

## Responsive Layout System

```csharp
using UnityEngine;

public class ResponsivePanel : MonoBehaviour
{
    [SerializeField] private AnchorPreset anchorPreset;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920, 1080);
    [SerializeField] private bool useSafeArea = true;

    public enum AnchorPreset
    {
        FullScreen, TopLeft, TopCenter, TopRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        BottomLeft, BottomCenter, BottomRight
    }

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (useSafeArea)
            ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        // Unity 6: Screen.safeArea works for runtime
        // For Editor preview, can use Device.Screen.safeArea (optional)
        Rect safeArea = Screen.safeArea;
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        // Convert screen coords to anchor values (0-1 range)
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
    }
}
```

## 9-Slice Gothic Frames

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "FrameStyle", menuName = "UI/Gothic Frame Style")]
public class GothicFrameStyle : ScriptableObject
{
    [Header("Sprites (set 9-slice borders in Sprite Editor)")]
    public Sprite frameSprite;          // 9-sliced ornate border
    public Sprite cornerAccent;         // Decorative corner pieces
    public Sprite dividerLine;          // Ornate horizontal divider

    [Header("Slicing Reference (actual borders set in Sprite Editor)")]
    [Tooltip("Reference values - set actual borders in Sprite Editor")]
    public Vector4 border = new Vector4(32, 32, 32, 32);  // L, B, R, T

    [Header("Colors")]
    public Color frameColor = new Color(0.81f, 0.71f, 0.23f);  // Aged gold
    public Color shadowColor = new Color(0, 0, 0, 0.5f);

    [Header("Animation")]
    public float pulseSpeed = 1f;
    [Range(0f, 0.5f)]
    public float pulseIntensity = 0.1f;
}

// Usage with Image component:
// 1. Set Image.type = Image.Type.Sliced
// 2. Assign sprite with borders configured in Sprite Editor
// 3. Image.pixelsPerUnitMultiplier for scaling control
```
