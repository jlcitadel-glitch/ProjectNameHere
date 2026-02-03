# HUD Design

> **Unity 6 2D** - Screen Space Overlay canvas, no camera reference needed.

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
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private Image currencyIcon;

    [Header("Ability Indicator")]
    [SerializeField] private Image abilityIcon;
    [SerializeField] private Image abilityCooldownOverlay;

    public void UpdateHealth(int current, int max)
    {
        if (healthGems == null) return;

        for (int i = 0; i < healthGems.Length; i++)
        {
            if (healthGems[i] == null) continue;

            if (i < current)
                healthGems[i].sprite = filledGem;
            else if (i < max)
                healthGems[i].sprite = emptyGem;
            else
                healthGems[i].gameObject.SetActive(false);
        }
    }

    public void UpdateSoulMeter(float normalized)
    {
        normalized = Mathf.Clamp01(normalized);

        if (soulMeterFill != null)
        {
            soulMeterFill.fillAmount = normalized;
            soulMeterFill.color = soulGradient.Evaluate(normalized);
        }

        if (soulMeterGlow != null)
        {
            float glowAlpha = Mathf.Lerp(0.2f, 0.8f, normalized);
            soulMeterGlow.color = new Color(1f, 1f, 1f, glowAlpha);
        }
    }

    public void UpdateCurrency(int amount)
    {
        if (currencyText != null)
            currencyText.text = amount.ToString("N0"); // Formatted with commas
    }

    public void UpdateAbilityCooldown(float normalizedCooldown)
    {
        if (abilityCooldownOverlay != null)
            abilityCooldownOverlay.fillAmount = normalizedCooldown;
    }
}
```
