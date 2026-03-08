using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Changes button text to aged gold when highlighted or selected,
    /// and back to dim white when not. Souls-like text-only button effect.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SoulsButtonTextHighlight : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private static readonly Color NormalColor = new Color(0.93f, 0.89f, 0.82f, 0.5f);
        private static readonly Color HighlightColor = new Color(0.81f, 0.71f, 0.23f, 1f);

        private TMP_Text label;
        private bool isHighlighted;
        private bool isSelected;

        private void Awake()
        {
            label = GetComponentInChildren<TMP_Text>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHighlighted = true;
            UpdateColor();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHighlighted = false;
            UpdateColor();
        }

        public void OnSelect(BaseEventData eventData)
        {
            isSelected = true;
            UpdateColor();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            isSelected = false;
            UpdateColor();
        }

        private void UpdateColor()
        {
            if (label == null) return;
            label.color = (isHighlighted || isSelected) ? HighlightColor : NormalColor;
        }
    }
}
