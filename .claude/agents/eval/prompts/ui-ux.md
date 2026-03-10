# UI/UX — Eval Prompts

## Prompt 1: "Add an inventory screen with drag-and-drop"
**Tests:** Gothic style adherence, menu architecture, input dual-support
**Assertions:**
- Should use gothic color palette (crimson, midnight blue, aged gold)
- Should support both gamepad and mouse (drag-and-drop + cursor navigation)
- Should use 9-slice gothic frames
- Should integrate with UIManager and canvas separation pattern
- Should file bead for systems agent (inventory data backend)

## Prompt 2: "The skill tree tooltip flickers when moving between nodes"
**Tests:** UI debugging, input system knowledge
**Assertions:**
- Should investigate focus/hover state transitions
- Should check if tooltip repositioning causes layout rebuild
- Should consider CanvasGroup alpha transitions
- Should reference FocusManager behavior
- Should NOT modify non-UI scripts

## Prompt 3: "Add LB/RB tab navigation to a new settings panel"
**Tests:** Core UI pattern knowledge, implementation specifics
**Assertions:**
- Should reference TabbedMenuController pattern
- Should use InputAction callbacks for LB/RB
- Should handle both gamepad bumpers and keyboard alternatives
- Should add tab sound feedback via UISoundBank
- Should use existing UIStyleGuide for consistent styling

## Prompt 4: "Damage numbers are hard to read against dark backgrounds"
**Tests:** Visual feedback design, accessibility awareness
**Assertions:**
- Should suggest outline/shadow on TextMeshPro
- Should reference spectral cyan or aged gold from palette
- Should consider World Space canvas setup (damage numbers are 3D exception)
- Should test at different font sizes for readability
- Should mention localization-ready text (no hardcoded format strings)

## Prompt 5: "Main menu buttons don't respond to gamepad"
**Tests:** Input system knowledge, project-specific gotcha awareness
**Assertions:**
- Should check EventSystem has InputSystemUIInputModule (not StandaloneInputModule)
- Should verify firstSelectedGameObject is set on EventSystem
- Should check button navigation is set (explicit or automatic)
- Should reference FocusManager for initial selection
- Should know buttons are wired via Inspector onClick (not code AddListener)
