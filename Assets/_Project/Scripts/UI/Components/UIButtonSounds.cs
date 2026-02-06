using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectName.UI
{
    /// <summary>
    /// Add to any Button for SOTN-style hover and click sound feedback.
    /// Set the click sound type in the Inspector to match the button's role.
    /// </summary>
    public class UIButtonSounds : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        public enum ClickSoundType
        {
            Select,
            Cancel,
            Confirm
        }

        [SerializeField] private ClickSoundType clickSound = ClickSoundType.Select;

        public void SetClickSound(ClickSoundType type) => clickSound = type;

        public void OnPointerEnter(PointerEventData eventData)
        {
            UIManager.Instance?.PlayNavigateSound();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            switch (clickSound)
            {
                case ClickSoundType.Select:
                    UIManager.Instance?.PlaySelectSound();
                    break;
                case ClickSoundType.Cancel:
                    UIManager.Instance?.PlayCancelSound();
                    break;
                case ClickSoundType.Confirm:
                    UIManager.Instance?.PlayConfirmSound();
                    break;
            }
        }
    }
}
