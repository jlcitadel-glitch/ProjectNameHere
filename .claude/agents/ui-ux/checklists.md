# Checklists & References

## Accessibility Checklist

- [ ] Minimum contrast ratio 4.5:1 for body text
- [ ] Minimum contrast ratio 3:1 for large text and icons
- [ ] Colorblind mode with pattern/shape differentiation
- [ ] Scalable text (3 size options minimum)
- [ ] Full keyboard/gamepad navigation
- [ ] Focus indicators clearly visible
- [ ] No information conveyed by color alone
- [ ] Screen reader compatible labels
- [ ] Remappable controls
- [ ] Subtitle options for audio

## UI Review Checklist

- [ ] Matches gothic aesthetic (SOTN/Soul Reaver themes)
- [ ] Navigation works with gamepad (D-pad + bumpers for tabs)
- [ ] Focus states are clear and visible
- [ ] Transitions are smooth (0.2-0.4s)
- [ ] Audio feedback on all interactions
- [ ] Text is readable at 1080p and 4K
- [ ] 9-slice frames scale correctly
- [ ] Canvas batching optimized (check Frame Debugger)
- [ ] Localization-ready (no hardcoded strings)
- [ ] Consistent with established patterns

## Reference Games

| Game | Learn From |
|------|------------|
| Castlevania: SOTN | Equipment screen, inventory grid, gothic frames |
| Hollow Knight | Minimal HUD, tab navigation, map overlay |
| Dead Cells | Item descriptions, run stats, pause menu |
| Salt and Sanctuary | Skill tree, equipment weight system |
| Blasphemous | Prayer/ability slots, confession menus |
| Legacy of Kain: Soul Reaver | Spectral effects, glyph menus, health coil |

## Figma to Unity Pipeline

```
1. Design in Figma
   ├── Use 1920x1080 artboard
   ├── Create component library
   ├── Export sprites as PNG with 2x scale
   └── Document spacing, colors, typography

2. Import to Unity
   ├── Set texture type to Sprite (2D and UI)
   ├── Configure 9-slice borders
   ├── Create Sprite Atlas for batching
   └── Set filter mode (Point for pixel art)

3. Build in Unity
   ├── Match Figma layout with anchors/pivots
   ├── Use Layout Groups for dynamic content
   ├── Apply styles via ScriptableObjects
   └── Test at multiple resolutions
```
