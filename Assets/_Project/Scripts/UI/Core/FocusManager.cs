using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectName.UI
{
    /// <summary>
    /// Manages UI focus for gamepad and keyboard navigation.
    /// Ensures a valid selection is always maintained for non-mouse input.
    /// </summary>
    public class FocusManager : MonoBehaviour
    {
        [Header("Navigation")]
        [Tooltip("Default element to select when nothing is selected")]
        [SerializeField] private Selectable defaultSelection;

        [Header("Device Detection")]
        [Tooltip("Show keyboard/mouse button prompts")]
        [SerializeField] private GameObject keyboardPrompts;

        [Tooltip("Show gamepad button prompts")]
        [SerializeField] private GameObject gamepadPrompts;

        private Selectable lastSelected;
        private bool isUsingGamepad;

        private EventSystem ActiveEventSystem => EventSystem.current;

        public bool IsUsingGamepad => isUsingGamepad;

        public event System.Action<bool> OnInputDeviceChanged;

        private void OnEnable()
        {
            InputSystem.onDeviceChange += OnDeviceChange;
            InputSystem.onActionChange += OnActionChange;

            DetectCurrentInputDevice();
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            InputSystem.onActionChange -= OnActionChange;
        }

        private void Update()
        {
            EnsureValidSelection();
        }

        /// <summary>
        /// Ensures something is always selected for gamepad/keyboard users.
        /// </summary>
        private void EnsureValidSelection()
        {
            if (ActiveEventSystem == null)
                return;

            // Only enforce selection for non-mouse input
            if (!isUsingGamepad && Mouse.current != null && Mouse.current.wasUpdatedThisFrame)
                return;

            GameObject currentSelected = ActiveEventSystem.currentSelectedGameObject;

            if (currentSelected == null || !currentSelected.activeInHierarchy)
            {
                // Try to restore last selection
                if (lastSelected != null && lastSelected.gameObject.activeInHierarchy && lastSelected.interactable)
                {
                    ActiveEventSystem.SetSelectedGameObject(lastSelected.gameObject);
                }
                // Fall back to default
                else if (defaultSelection != null && defaultSelection.gameObject.activeInHierarchy && defaultSelection.interactable)
                {
                    ActiveEventSystem.SetSelectedGameObject(defaultSelection.gameObject);
                }
            }
            else
            {
                // Track the current selection (skip GetComponent when unchanged)
                if (lastSelected == null || lastSelected.gameObject != currentSelected)
                {
                    Selectable selectable = currentSelected.GetComponent<Selectable>();
                    if (selectable != null)
                    {
                        lastSelected = selectable;
                    }
                }
            }
        }

        /// <summary>
        /// Sets the current focus target.
        /// </summary>
        public void SetFocus(Selectable target)
        {
            if (target == null || ActiveEventSystem == null)
                return;

            ActiveEventSystem.SetSelectedGameObject(target.gameObject);
            lastSelected = target;
        }

        /// <summary>
        /// Sets the current focus target by GameObject.
        /// </summary>
        public void SetFocus(GameObject target)
        {
            if (target == null)
                return;

            Selectable selectable = target.GetComponent<Selectable>();
            if (selectable != null)
            {
                SetFocus(selectable);
            }
            else if (ActiveEventSystem != null)
            {
                ActiveEventSystem.SetSelectedGameObject(target);
            }
        }

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        public void ClearFocus()
        {
            if (ActiveEventSystem != null)
            {
                ActiveEventSystem.SetSelectedGameObject(null);
            }
        }

        /// <summary>
        /// Sets the default selection target.
        /// </summary>
        public void SetDefaultSelection(Selectable selectable)
        {
            defaultSelection = selectable;
        }

        /// <summary>
        /// Gets the currently focused element.
        /// </summary>
        public Selectable GetCurrentFocus()
        {
            if (ActiveEventSystem == null || ActiveEventSystem.currentSelectedGameObject == null)
                return null;

            if (lastSelected != null && lastSelected.gameObject == ActiveEventSystem.currentSelectedGameObject)
                return lastSelected;

            return ActiveEventSystem.currentSelectedGameObject.GetComponent<Selectable>();
        }

        /// <summary>
        /// Navigates to a selectable in a specific direction.
        /// </summary>
        public void Navigate(MoveDirection direction)
        {
            Selectable current = GetCurrentFocus();
            if (current == null)
            {
                if (defaultSelection != null)
                {
                    SetFocus(defaultSelection);
                }
                return;
            }

            Selectable next = null;

            switch (direction)
            {
                case MoveDirection.Up:
                    next = current.FindSelectableOnUp();
                    break;
                case MoveDirection.Down:
                    next = current.FindSelectableOnDown();
                    break;
                case MoveDirection.Left:
                    next = current.FindSelectableOnLeft();
                    break;
                case MoveDirection.Right:
                    next = current.FindSelectableOnRight();
                    break;
            }

            if (next != null && next.interactable)
            {
                SetFocus(next);
            }
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.UsageChanged ||
                change == InputDeviceChange.Added ||
                change == InputDeviceChange.Reconnected)
            {
                DetectCurrentInputDevice();
            }
        }

        private void OnActionChange(object obj, InputActionChange change)
        {
            if (change == InputActionChange.ActionPerformed)
            {
                if (obj is InputAction action)
                {
                    DetectInputFromAction(action);
                }
            }
        }

        private void DetectInputFromAction(InputAction action)
        {
            if (action.activeControl == null)
                return;

            InputDevice device = action.activeControl.device;

            bool wasUsingGamepad = isUsingGamepad;

            if (device is Gamepad)
            {
                isUsingGamepad = true;
            }
            else if (device is Keyboard || device is Mouse)
            {
                isUsingGamepad = false;
            }

            if (wasUsingGamepad != isUsingGamepad)
            {
                UpdatePrompts();
                OnInputDeviceChanged?.Invoke(isUsingGamepad);
            }
        }

        private void DetectCurrentInputDevice()
        {
            bool wasUsingGamepad = isUsingGamepad;

            // Check if gamepad was used more recently
            if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
            {
                isUsingGamepad = true;
            }
            else if ((Keyboard.current != null && Keyboard.current.wasUpdatedThisFrame) ||
                     (Mouse.current != null && Mouse.current.wasUpdatedThisFrame))
            {
                isUsingGamepad = false;
            }
            else
            {
                // Default to gamepad if one is connected
                isUsingGamepad = Gamepad.current != null;
            }

            if (wasUsingGamepad != isUsingGamepad)
            {
                UpdatePrompts();
                OnInputDeviceChanged?.Invoke(isUsingGamepad);
            }
        }

        private void UpdatePrompts()
        {
            if (keyboardPrompts != null)
            {
                keyboardPrompts.SetActive(!isUsingGamepad);
            }

            if (gamepadPrompts != null)
            {
                gamepadPrompts.SetActive(isUsingGamepad);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (defaultSelection == null)
            {
                // Try to find a selectable in children
                defaultSelection = GetComponentInChildren<Selectable>();
            }
        }
#endif
    }
}
