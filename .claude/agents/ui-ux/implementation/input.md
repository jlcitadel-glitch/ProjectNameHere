# Input Handling: Multi-Device Support

> **Unity 6 + Input System 1.17.0** - Uses InputActionReference, not legacy Input class.

## Input System Integration

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class UIInputHandler : MonoBehaviour
{
    [Header("Action References")]
    [SerializeField] private InputActionReference navigateActionRef;
    [SerializeField] private InputActionReference submitActionRef;
    [SerializeField] private InputActionReference cancelActionRef;
    [SerializeField] private InputActionReference tabLeftActionRef;
    [SerializeField] private InputActionReference tabRightActionRef;

    [Header("Prompt Displays")]
    [SerializeField] private GameObject keyboardPrompts;
    [SerializeField] private GameObject gamepadPrompts;

    private void OnEnable()
    {
        // Unity 6: Use InputActionReference.action for callbacks
        navigateActionRef.action.performed += OnNavigate;
        submitActionRef.action.performed += OnSubmit;
        cancelActionRef.action.performed += OnCancel;

        navigateActionRef.action.Enable();
        submitActionRef.action.Enable();
        cancelActionRef.action.Enable();

        // Device change detection
        InputSystem.onActionChange += OnActionChange;
    }

    private void OnDisable()
    {
        navigateActionRef.action.performed -= OnNavigate;
        submitActionRef.action.performed -= OnSubmit;
        cancelActionRef.action.performed -= OnCancel;

        InputSystem.onActionChange -= OnActionChange;
    }

    private void OnActionChange(object obj, InputActionChange change)
    {
        // Update prompts when any action is triggered
        if (change == InputActionChange.ActionPerformed)
            UpdatePrompts();
    }

    private void UpdatePrompts()
    {
        // Check which device was used most recently
        bool isGamepad = Gamepad.current != null &&
                         Gamepad.current.lastUpdateTime > Keyboard.current?.lastUpdateTime;
        keyboardPrompts.SetActive(!isGamepad);
        gamepadPrompts.SetActive(isGamepad);
    }

    private void OnNavigate(InputAction.CallbackContext ctx) { /* handle navigation */ }
    private void OnSubmit(InputAction.CallbackContext ctx) { /* handle submit */ }
    private void OnCancel(InputAction.CallbackContext ctx) { /* handle cancel */ }
}
```

## Focus Management

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FocusManager : MonoBehaviour
{
    [SerializeField] private Selectable defaultSelection;
    [SerializeField] private bool wrapNavigation = true;

    private EventSystem eventSystem;
    private Selectable lastSelected;

    private void Awake()
    {
        // Cache EventSystem reference in Awake
        eventSystem = EventSystem.current;
    }

    private void Update()
    {
        if (eventSystem == null) return;

        // Ensure something is always selected for gamepad users
        if (eventSystem.currentSelectedGameObject == null)
        {
            if (lastSelected != null && lastSelected.gameObject.activeInHierarchy)
                eventSystem.SetSelectedGameObject(lastSelected.gameObject);
            else if (defaultSelection != null)
                eventSystem.SetSelectedGameObject(defaultSelection.gameObject);
        }
        else
        {
            lastSelected = eventSystem.currentSelectedGameObject.GetComponent<Selectable>();
        }
    }

    public void SetFocus(Selectable target)
    {
        if (eventSystem == null || target == null) return;
        eventSystem.SetSelectedGameObject(target.gameObject);
        lastSelected = target;
    }

    public void ClearFocus()
    {
        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(null);
    }
}
```
