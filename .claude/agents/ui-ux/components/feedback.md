# Feedback Systems

## Audio Cues

```csharp
[CreateAssetMenu(fileName = "UISoundBank", menuName = "Audio/UI Sound Bank")]
public class UISoundBank : ScriptableObject
{
    [Header("Navigation")]
    public AudioClip navigate;          // Subtle tick
    public AudioClip select;            // Deeper confirmation
    public AudioClip cancel;            // Soft whoosh back
    public AudioClip tabSwitch;         // Page turn / stone slide

    [Header("Feedback")]
    public AudioClip confirm;           // Satisfying click
    public AudioClip error;             // Low buzz
    public AudioClip itemPickup;        // Mystical chime
    public AudioClip menuOpen;          // Stone door / book open
    public AudioClip menuClose;         // Reverse of open

    [Header("Gothic Ambience")]
    public AudioClip backgroundDrone;   // Low cathedral reverb
    public AudioClip candleFlicker;     // Subtle fire crackle
}
```

## Visual Feedback

```csharp
public class ButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float hoverDuration = 0.15f;
    [SerializeField] private Image glowImage;
    [SerializeField] private Color glowColor = new Color(0.81f, 0.71f, 0.23f, 0.5f);
    [SerializeField] private ParticleSystem clickParticles;

    private Vector3 originalScale;

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.DOScale(originalScale * hoverScale, hoverDuration);
        glowImage.DOColor(glowColor, hoverDuration);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
        clickParticles?.Play();
    }
}
```

## Tooltips

```csharp
public class TooltipSystem : MonoBehaviour
{
    [SerializeField] private RectTransform tooltipPanel;
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Vector2 offset = new Vector2(20f, -20f);
    [SerializeField] private float showDelay = 0.5f;
    [SerializeField] private CanvasGroup canvasGroup;

    public void Show(string header, string description, Vector2 position)
    {
        StartCoroutine(ShowAfterDelay(header, description, position));
    }

    private IEnumerator ShowAfterDelay(string header, string description, Vector2 position)
    {
        yield return new WaitForSeconds(showDelay);
        headerText.text = header;
        descriptionText.text = description;
        tooltipPanel.position = position + offset;
        tooltipPanel.gameObject.SetActive(true);
        canvasGroup.DOFade(1f, 0.15f);
    }
}
```
