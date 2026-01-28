# Performance & Localization

## Canvas Batching

```csharp
public class UIPerformanceOptimizer : MonoBehaviour
{
    [SerializeField] private bool disableRaycastsOnStatic = true;
    [SerializeField] private bool useAtlasedSprites = true;

    // Separate canvases by update frequency
    [SerializeField] private Canvas staticCanvas;      // Frames, backgrounds
    [SerializeField] private Canvas dynamicCanvas;     // Health, timers
    [SerializeField] private Canvas animatedCanvas;    // Constantly moving elements

    private void OptimizeStaticElements()
    {
        var staticImages = staticCanvas.GetComponentsInChildren<Image>();
        foreach (var img in staticImages)
        {
            if (!img.GetComponent<Selectable>())
                img.raycastTarget = false;
        }

        var texts = GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var text in texts)
            text.raycastTarget = false;
    }
}
```

## Sprite Atlas Structure

```
UI_Gothic_Atlas
├── Frames/
│   ├── frame_ornate_9slice
│   ├── frame_simple_9slice
│   └── corner_flourish, divider_line
├── Icons/
│   ├── icon_health_gem, icon_soul_orb
│   └── icon_currency, ability_icons...
├── Buttons/
│   └── button_normal, button_hover, button_pressed
└── Effects/
    └── glow_soft, vignette, spectral_shimmer
```

## Localization Support

```csharp
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string localizationKey;
    [SerializeField] private float horizontalBuffer = 1.3f;  // German ~30% longer
    [SerializeField] private bool autoResize = true;

    private void Start()
    {
        LocalizationManager.OnLanguageChanged += UpdateText;
        UpdateText();
    }

    private void UpdateText()
    {
        GetComponent<TextMeshProUGUI>().text = LocalizationManager.GetString(localizationKey);
        if (autoResize)
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }
}
```
