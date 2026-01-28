# HUD Design

## Minimal Gothic HUD (Hollow Knight Inspired)

```
┌─────────────────────────────────────────────────────────────┐
│ ♦♦♦♦♦○○○○○                                    [Soul Meter] │
│                                                             │
│                                                             │
│                                               ┌───────────┐ │
│                                               │  [Ability]│ │
│ [Currency]                                    └───────────┘ │
└─────────────────────────────────────────────────────────────┘
```

## Implementation

```csharp
public class GothicHUD : MonoBehaviour
{
    [Header("Health Display - SOTN Style")]
    [SerializeField] private Image[] healthGems;
    [SerializeField] private Sprite filledGem;
    [SerializeField] private Sprite emptyGem;
    [SerializeField] private Sprite breakingGem;

    [Header("Soul/Magic Meter - Soul Reaver Style")]
    [SerializeField] private Image soulMeterFill;
    [SerializeField] private Image soulMeterGlow;
    [SerializeField] private Gradient soulGradient;

    [Header("Currency")]
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private Image currencyIcon;

    [Header("Ability Indicator")]
    [SerializeField] private Image abilityIcon;
    [SerializeField] private Image abilityCooldownOverlay;

    public void UpdateHealth(int current, int max)
    {
        for (int i = 0; i < healthGems.Length; i++)
        {
            if (i < current) healthGems[i].sprite = filledGem;
            else if (i < max) healthGems[i].sprite = emptyGem;
            else healthGems[i].gameObject.SetActive(false);
        }
    }

    public void UpdateSoulMeter(float normalized)
    {
        soulMeterFill.fillAmount = normalized;
        soulMeterFill.color = soulGradient.Evaluate(normalized);
        float glowAlpha = Mathf.Lerp(0.2f, 0.8f, normalized);
        soulMeterGlow.color = new Color(1f, 1f, 1f, glowAlpha);
    }
}
```
