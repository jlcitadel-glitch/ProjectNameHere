# Performance & Localization

> **Unity 6 2D** - Separate canvases by update frequency, disable raycasts on non-interactive elements.

## Canvas Batching

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPerformanceOptimizer : MonoBehaviour
{
    [Header("Optimization Settings")]
    [SerializeField] private bool disableRaycastsOnStatic = true;
    [SerializeField] private bool useAtlasedSprites = true;

    [Header("Canvas References (separate by update frequency)")]
    [SerializeField] private Canvas staticCanvas;      // Frames, backgrounds (rarely changes)
    [SerializeField] private Canvas dynamicCanvas;     // Health, timers (changes often)
    [SerializeField] private Canvas animatedCanvas;    // Constantly moving elements

    private void Start()
    {
        if (disableRaycastsOnStatic)
            OptimizeStaticElements();
    }

    private void OptimizeStaticElements()
    {
        if (staticCanvas == null) return;

        // Disable raycasts on non-interactive images
        var staticImages = staticCanvas.GetComponentsInChildren<Image>(true);
        foreach (var img in staticImages)
        {
            // Keep raycasts on Selectables (buttons, etc.)
            if (img.GetComponent<Selectable>() == null)
                img.raycastTarget = false;
        }

        // Text almost never needs raycasts
        var allTexts = GetComponentsInChildren<TMP_Text>(true);
        foreach (var text in allTexts)
            text.raycastTarget = false;
    }

    // Call this in Editor via context menu to preview optimization
    [ContextMenu("Preview Optimization")]
    private void PreviewOptimization()
    {
        OptimizeStaticElements();
        Debug.Log("UI optimization applied. Check Frame Debugger for batching.");
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
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string localizationKey;
    [Tooltip("German text is ~30% longer than English")]
    [SerializeField] private float horizontalBuffer = 1.3f;
    [SerializeField] private bool autoResize = true;

    private TMP_Text textComponent;
    private RectTransform rectTransform;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        // Subscribe to language change events (implement LocalizationManager as needed)
        // LocalizationManager.OnLanguageChanged += UpdateText;
        UpdateText();
    }

    private void OnDestroy()
    {
        // LocalizationManager.OnLanguageChanged -= UpdateText;
    }

    public void UpdateText()
    {
        if (textComponent == null) return;

        // Replace with your localization system
        // textComponent.text = LocalizationManager.GetString(localizationKey);
        textComponent.text = localizationKey; // Placeholder

        if (autoResize && rectTransform != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    // Call when language changes
    public void SetKey(string newKey)
    {
        localizationKey = newKey;
        UpdateText();
    }
}

// Note: Unity 6 has built-in Localization package (com.unity.localization)
// Consider using it instead of a custom solution for production.
```
