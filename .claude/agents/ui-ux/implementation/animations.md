# UI Animation Patterns

## DOTween Menu Transitions

```csharp
using DG.Tweening;

public class MenuTransitions : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float slideDuration = 0.4f;
    [SerializeField] private Ease easeType = Ease.OutQuart;
    [SerializeField] private float vignetteIntensity = 0.3f;

    public void OpenMenu(CanvasGroup menu, RectTransform panel)
    {
        menu.gameObject.SetActive(true);
        menu.alpha = 0f;
        panel.anchoredPosition = new Vector2(0, -50f);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(menu.DOFade(1f, fadeDuration));
        sequence.Join(panel.DOAnchorPosY(0f, slideDuration).SetEase(easeType));
        sequence.Join(DOTween.To(() => vignetteIntensity, x => SetVignette(x), 0.5f, fadeDuration));
    }

    public void CloseMenu(CanvasGroup menu, RectTransform panel, System.Action onComplete = null)
    {
        Sequence sequence = DOTween.Sequence();
        sequence.Append(menu.DOFade(0f, fadeDuration));
        sequence.Join(panel.DOAnchorPosY(-50f, slideDuration).SetEase(Ease.InQuart));
        sequence.OnComplete(() => { menu.gameObject.SetActive(false); onComplete?.Invoke(); });
    }

    // Soul Reaver spectral shimmer
    public void SpectralPulse(Image element)
    {
        element.DOColor(new Color(0.25f, 0.88f, 0.82f, 0.8f), 0.5f).SetLoops(-1, LoopType.Yoyo);
    }
}
```

## Animator-Based UI States

```csharp
public class AnimatedButton : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    [SerializeField] private Animator animator;

    private static readonly int NormalHash = Animator.StringToHash("Normal");
    private static readonly int HighlightedHash = Animator.StringToHash("Highlighted");
    private static readonly int SelectedHash = Animator.StringToHash("Selected");
    private static readonly int PressedHash = Animator.StringToHash("Pressed");

    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip selectSound;
    [SerializeField] private AudioClip confirmSound;

    public void OnSelect(BaseEventData eventData)
    {
        animator.SetTrigger(SelectedHash);
        AudioManager.Instance.PlayUI(selectSound);
    }
}
```
