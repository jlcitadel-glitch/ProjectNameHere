# Input Handling: Multi-Device Support

## Input System Integration

```csharp
public class UIInputHandler : MonoBehaviour
{
    [SerializeField] private InputActionAsset uiActions;
    [SerializeField] private GameObject keyboardPrompts;
    [SerializeField] private GameObject gamepadPrompts;

    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction cancelAction;
    private InputAction tabLeftAction;
    private InputAction tabRightAction;

    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;

        navigateAction = uiActions.FindAction("Navigate");
        submitAction = uiActions.FindAction("Submit");
        cancelAction = uiActions.FindAction("Cancel");

        navigateAction.Enable();
        submitAction.Enable();
        cancelAction.Enable();
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.UsageChanged) UpdatePrompts();
    }

    private void UpdatePrompts()
    {
        bool isGamepad = Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame;
        keyboardPrompts.SetActive(!isGamepad);
        gamepadPrompts.SetActive(isGamepad);
    }
}
```

## Focus Management

```csharp
public class FocusManager : MonoBehaviour
{
    [SerializeField] private Selectable defaultSelection;
    [SerializeField] private bool wrapNavigation = true;

    private EventSystem eventSystem;
    private Selectable lastSelected;

    private void Update()
    {
        // Ensure something is always selected for gamepad users
        if (eventSystem.currentSelectedGameObject == null)
        {
            if (lastSelected != null && lastSelected.gameObject.activeInHierarchy)
                eventSystem.SetSelectedGameObject(lastSelected.gameObject);
            else
                eventSystem.SetSelectedGameObject(defaultSelection.gameObject);
        }
        else
        {
            lastSelected = eventSystem.currentSelectedGameObject.GetComponent<Selectable>();
        }
    }

    public void SetFocus(Selectable target)
    {
        eventSystem.SetSelectedGameObject(target.gameObject);
        lastSelected = target;
    }
}
```
