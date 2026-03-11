# Design Philosophy

> **Unity 6 2D** - All UI uses UGUI with TextMeshPro (integrated in Unity 6).

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

### Font Families
```
Headers:     Serif with flourishes (Cinzel, Cormorant Garamond)
Body:        Clean serif for readability (Crimson Text, EB Garamond)
Numbers:     Monospace for stats (Fira Code, Source Code Pro)
Runes/Lore:  Decorative/symbolic (custom glyph font)
```

### Size Tiers (4 tiers only — no exceptions)

Every UI screen must use exactly these 4 sizes. No arbitrary intermediate values.

| Tier | Constant | Size | Usage |
|------|----------|------|-------|
| **Header** | `FontHeader` | 24px | Panel titles, character name |
| **Primary** | `FontPrimary` | 16px | Stat names & values, equipment names, HP/MP, allocate buttons |
| **Secondary** | `FontSecondary` | 13px | Slot labels, inventory count, derived stats, class/level, compare deltas |
| **Flavor** | `FontFlavor` | 11px | Descriptions, lore, tooltips, smallest slot labels |

Auto-sizing ranges must only span between adjacent tiers:
- Primary text: `fontSizeMin = FontSecondary`, `fontSizeMax = FontPrimary`
- Secondary text: `fontSizeMin = FontFlavor`, `fontSizeMax = FontSecondary`

### Color Rules for Text
- **All body text**: Bone White (#F5F5DC) — HP, MP, stat values all use this
- **Headers/highlights**: Aged Gold (#CFB53B) — titles, character name, stat points label
- **Secondary labels**: Subtle Text (0.7, 0.65, 0.55) — slot labels, derived stats
- **Never color-code HP red or MP blue** on the character/stats screen — color is reserved for HUD bars and combat feedback
- **Stat deltas** (compare panel only): Faded Moss green for positive, Deep Crimson for negative

### Stat Display Format
- Core stats show base with total when equipment modifies: `Strength: 1 [6]`
- HP/MP show bonus inline: `HP: 100/130 (+30)`
- Derived stats use prefix: `Melee Damage: x1.12`, `Critical: 2.5% (x2.01)`
