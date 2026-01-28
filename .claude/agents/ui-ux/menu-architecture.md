# Menu Architecture: Metroidvania Patterns

## Screen Flow Map

```
Title Screen
    ├── New Game
    │   └── Save Slot Selection (3 slots, SOTN style)
    ├── Continue
    │   └── Save Slot Selection
    ├── Options
    │   ├── Audio (Master, Music, SFX, Voice)
    │   ├── Video (Resolution, Fullscreen, VSync, Screen Shake)
    │   ├── Controls (Keyboard/Gamepad Bindings, Sensitivity)
    │   └── Accessibility (Colorblind, Text Size, Screen Reader)
    └── Quit

Pause Menu (In-Game)
    ├── Resume
    ├── Inventory [→]
    ├── Equipment [→]
    ├── Map [→]
    ├── Abilities [→]
    ├── Bestiary [→]
    ├── Options [→]
    └── Quit to Title
```

## Hollow Knight-Style Tab Navigation

```
┌─────────────────────────────────────────────────────────────┐
│  [Inventory]  [Equipment]  [Map]  [Abilities]  [Bestiary]  │
├─────────────────────────────────────────────────────────────┤
│                     Tab Content Area                        │
└─────────────────────────────────────────────────────────────┘
      ← LB/L1                                    RB/R1 →
```

## SOTN-Style Inventory Grid

```
┌──────────────────────────────────────────────────────┐
│                    INVENTORY                          │
├──────────────────────────────────────────────────────┤
│  ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐   │
│  │    │ │    │ │    │ │ ** │ │    │ │    │ │    │   │
│  └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘   │
├──────────────────────────────────────────────────────┤
│  Item Name: Crimson Cloak                            │
│  "A cloak soaked in the blood of a hundred souls."   │
│  DEF +5    LCK +2                                    │
└──────────────────────────────────────────────────────┘
```

## Equipment Screen (SOTN Layout)

```
┌─────────────────────────────────────────────────────────────┐
│                       EQUIPMENT                              │
├───────────────────────┬─────────────────────────────────────┤
│      ┌─────────┐      │   STATS                             │
│      │  HEAD   │      │   STR ████████░░  42                │
│      └─────────┘      │   CON ██████░░░░  31                │
│  ┌─────┐     ┌─────┐  │   INT ████░░░░░░  22                │
│  │HAND │     │HAND │  │   LCK ███░░░░░░░  18                │
│  │ L   │     │  R  │  │                                      │
│  └─────┘     └─────┘  │   DEF  45    ATK  67                │
│      ┌─────────┐      │   RES  23    CRT  12%               │
│      │  BODY   │      │                                      │
│      └─────────┘      │   Gold: 12,450                      │
│      ┌─────────┐      │   Time: 04:23:17                    │
│      │ CLOAK   │      │                                      │
│      └─────────┘      │                                      │
│  ┌─────┐     ┌─────┐  │                                      │
│  │RING │     │RING │  │                                      │
│  └─────┘     └─────┘  │                                      │
└───────────────────────┴─────────────────────────────────────┘
```
