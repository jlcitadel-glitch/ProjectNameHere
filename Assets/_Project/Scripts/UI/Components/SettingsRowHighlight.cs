using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectName.UI
{
    /// <summary>
    /// Highlights a settings row background when its child selectable has focus.
    /// Attach to a row GameObject that contains a Slider, Dropdown, or Button.
    /// </summary>
    public class SettingsRowHighlight : MonoBehaviour
    {
        private static readonly Color HighlightColor = new Color(0.15f, 0.12f, 0.20f, 0.6f);

        private Image rowImage;
        private Selectable childSelectable;

        private void Awake()
        {
            rowImage = GetComponent<Image>();
            childSelectable = GetComponentInChildren<Selectable>();
        }

        private void Update()
        {
            if (rowImage == null || childSelectable == null) return;

            var current = EventSystem.current;
            if (current == null) return;

            bool isFocused = current.currentSelectedGameObject != null
                && current.currentSelectedGameObject.transform.IsChildOf(transform);

            rowImage.color = isFocused ? HighlightColor : Color.clear;
        }
    }
}
