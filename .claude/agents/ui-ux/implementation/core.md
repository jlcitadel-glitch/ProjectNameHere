# Core Unity UI Implementation

## Canvas Setup

```csharp
public class UIManager : MonoBehaviour
{
    [Header("Canvas References")]
    [SerializeField] private Canvas mainMenuCanvas;     // Screen Space - Overlay
    [SerializeField] private Canvas hudCanvas;          // Screen Space - Camera
    [SerializeField] private Canvas worldCanvas;        // World Space (damage numbers)
    [SerializeField] private Canvas pauseCanvas;        // Screen Space - Overlay (high sort)

    [Header("Canvas Groups for Transitions")]
    [SerializeField] private CanvasGroup mainMenuGroup;
    [SerializeField] private CanvasGroup hudGroup;

    private void ConfigureCanvases()
    {
        mainMenuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainMenuCanvas.sortingOrder = 100;

        hudCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        hudCanvas.worldCamera = Camera.main;
        hudCanvas.planeDistance = 1f;

        worldCanvas.renderMode = RenderMode.WorldSpace;
    }
}
```

## Responsive Layout System

```csharp
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

    private void ApplySafeArea()
    {
        if (!useSafeArea) return;
        Rect safeArea = Screen.safeArea;
        // Apply safe area anchors...
    }
}
```

## 9-Slice Gothic Frames

```csharp
[CreateAssetMenu(fileName = "FrameStyle", menuName = "UI/Gothic Frame Style")]
public class GothicFrameStyle : ScriptableObject
{
    [Header("Sprites")]
    public Sprite frameSprite;          // 9-sliced ornate border
    public Sprite cornerAccent;         // Decorative corner pieces
    public Sprite dividerLine;          // Ornate horizontal divider

    [Header("Slicing")]
    public Vector4 border = new Vector4(32, 32, 32, 32);  // L, B, R, T

    [Header("Colors")]
    public Color frameColor = new Color(0.81f, 0.71f, 0.23f);  // Aged gold
    public Color shadowColor = new Color(0, 0, 0, 0.5f);

    [Header("Animation")]
    public float pulseSpeed = 1f;
    public float pulseIntensity = 0.1f;
}
```
