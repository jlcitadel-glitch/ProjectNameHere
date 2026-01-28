# Design Philosophy

## Visual Identity: Gothic Elegance

**Castlevania SOTN Influences:**
- Ornate baroque frames and borders
- Deep crimsons, royal purples, aged golds
- Stained glass motifs and cathedral aesthetics
- Elegant serif typography with flourishes
- Blood moon imagery, roses, thorns
- Candlelit ambiance with soft vignettes

**Legacy of Kain: Soul Reaver Influences:**
- Spectral/material realm duality (glowing ethereal elements)
- Ancient glyphs and runic symbols
- Decayed grandeur - beautiful things in ruin
- Soul energy visualizations (wispy, luminescent)
- Blue-green spectral highlights against dark backgrounds
- Stone textures with supernatural cracks

## Color Palette

```
├── Primary:     Deep Crimson (#8B0000), Midnight Blue (#191970)
├── Secondary:   Aged Gold (#CFB53B), Spectral Cyan (#00CED1)
├── Background:  Charcoal (#1a1a1a), Obsidian (#0d0d0d)
├── Text:        Bone White (#F5F5DC), Faded Parchment (#D4C4A8)
├── Accent:      Blood Red (#DC143C), Soul Blue (#4169E1)
└── Warning:     Poisoned Purple (#9932CC), Ethereal Green (#00FF7F)
```

## Typography

```
Headers:     Serif with flourishes (Cinzel, Cormorant Garamond)
Body:        Clean serif for readability (Crimson Text, EB Garamond)
Numbers:     Monospace for stats (Fira Code, Source Code Pro)
Runes/Lore:  Decorative/symbolic (custom glyph font)
```

**TextMeshPro Settings:**
```csharp
[Header("Gothic Text Style")]
[SerializeField] private TMP_FontAsset headerFont;      // Ornate serif
[SerializeField] private TMP_FontAsset bodyFont;        // Readable serif
[SerializeField] private float headerSize = 36f;
[SerializeField] private float bodySize = 24f;
[SerializeField] private Color textColor = new Color(0.96f, 0.96f, 0.86f); // Bone white
[SerializeField] private float characterSpacing = 2f;   // Slightly spread for elegance
```
