# VFX Agent — Sprint Tasks

> Owner: VFX Agent
> Scope: Visual effects, particles, orb visuals

---

## VFX-1: XP Orb Visual Design

**Priority:** P1
**Status:** Implemented
**Dependencies:** SYS-2 (ExperienceOrb system)
**Files to create/modify:**
- Orb sprite (or use existing particle sprite)
- Particle system prefab for orb trail
- ExperienceOrb prefab assembly

### Design Specifications

The XP orb should have three visual layers:

1. **Core sprite:** Small glowing orb (8x8 or 16x16 pixel sprite), bright green/yellow color
2. **Glow effect:** Additive blending sprite behind the core, soft circle, slightly larger, pulsing alpha
3. **Particle trail:** Small particle system emitting behind the orb as it moves

### Implementation

**Orb Visual Setup (on ExperienceOrb prefab):**

```
ExperienceOrb (GameObject)
├── Sprite (SpriteRenderer) — core orb, sorting order 10
├── Glow (SpriteRenderer) — additive blend, sorting order 9, alpha pulse
└── Trail (ParticleSystem) — emission rate based on velocity
```

**Glow Pulsing Script** (can be simple sine wave on SpriteRenderer alpha):

```csharp
// Add to ExperienceOrb.cs or as separate component
private SpriteRenderer glowRenderer;
private float pulseSpeed = 3f;
private float pulseMin = 0.3f;
private float pulseMax = 0.8f;

private void UpdateGlow()
{
    if (glowRenderer == null) return;

    float alpha = Mathf.Lerp(pulseMin, pulseMax,
        (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
    Color c = glowRenderer.color;
    c.a = alpha;
    glowRenderer.color = c;
}
```

**Particle Trail Settings:**
- Shape: Point
- Emission: 10-20 particles/sec
- Lifetime: 0.3-0.5s
- Start size: 0.05-0.1
- Start color: Green-yellow gradient
- Color over lifetime: fade to transparent
- Render mode: Billboard
- Material: Default particle, additive

**State-based visual changes:**
- **Scattering:** Full brightness, trail active
- **Idle:** Gentle pulse, trail reduced (5/sec)
- **Attracting:** Brighten, trail increased (30/sec), scale up slightly

### Collection Effect

On collect, before destroying the orb:
1. Spawn a small burst particle effect (10 particles, outward, 0.2s lifetime)
2. Brief screen-space flash (optional, handled via UI)

### Acceptance Criteria

- Orbs are visually distinct and readable on any background
- Glow pulses smoothly
- Trail follows orb movement
- Visual state changes are noticeable between idle and attracting
- Collection has satisfying visual feedback
- Performance: 50+ orbs on screen without frame drops

---

## VFX-2: Level-Up Visual Effect

**Priority:** P2
**Status:** Implemented
**Dependencies:** SYS-1 (LevelUp wiring)
**Files to create:** Level-up VFX prefab + script

### Design Specifications

When the player levels up, play a brief celebratory effect:

1. **Radial burst:** Particle ring expanding outward from player (0.5s)
2. **Vertical column:** Upward-streaming particles through player (1s)
3. **Number popup:** "LEVEL UP!" text floating upward and fading

### Implementation

**Prefab structure:**

```
LevelUpVFX (GameObject)
├── RadialBurst (ParticleSystem) — ring of particles
├── Column (ParticleSystem) — upward stream
└── TextPopup (TMP or sprite-based) — "LEVEL UP!" text
```

**RadialBurst settings:**
- Shape: Circle (edge), radius 0.5
- Emission: Burst 30 particles at t=0
- Lifetime: 0.5s
- Start speed: 3-5
- Start size: 0.1-0.2
- Color: Gold → White gradient
- Render: Additive

**Column settings:**
- Shape: Cone, angle 5°, radius 0.3
- Emission: 40/sec for 1s, then stop
- Lifetime: 0.8s
- Start speed: 2-4
- Start size: 0.05-0.15
- Color: White → Gold → Transparent
- Render: Additive

**Trigger:** Subscribe to `LevelSystem.OnLevelUp`:

```csharp
// In a LevelUpVFXController component on the player:
[SerializeField] private GameObject levelUpVFXPrefab;

private void Start()
{
    var levelSystem = GetComponent<LevelSystem>();
    if (levelSystem != null)
        levelSystem.OnLevelUp += HandleLevelUp;
}

private void HandleLevelUp(int newLevel)
{
    if (levelUpVFXPrefab != null)
    {
        Instantiate(levelUpVFXPrefab, transform.position, Quaternion.identity);
    }
}
```

### Acceptance Criteria

- VFX plays immediately on level-up
- Visible and satisfying but not obstructive to gameplay
- Auto-destroys after effect completes (~1.5s)
- No persistent particles after effect ends
- Looks good against dark and light backgrounds

---

## VFX-3: Boss Entrance/Phase-Change VFX

**Priority:** P2
**Status:** Implemented
**Dependencies:** PLR-4 (BossController)
**Files to create:** Boss VFX prefabs

### Design Specifications

Three distinct VFX moments:

#### 1. Boss Entrance

When boss spawns / fight begins:
- **Screen shake** (brief, 0.3s, small amplitude)
- **Dark fog sweep** from boss position outward
- **Boss name title card** effect (handled by UI, but VFX supports with particles)

**Fog sweep:**
- Large particle system
- Shape: Circle, expanding
- Dark/red particles
- Lifetime: 1s
- One burst of 50+ particles

#### 2. Phase Transition (Phase 1 → Phase 2)

- **Color flash:** Brief screen flash (red tint, 0.2s)
- **Shockwave ring:** Expanding ring from boss, similar to level-up but red/dark
- **Boss brief pause:** Already handled by BossController (stun/freeze)

**Shockwave:**
- Ring particle or line renderer circle expanding
- Red → Dark gradient
- 0.5s duration
- Slightly larger than level-up burst

#### 3. Enrage Effect

- **Persistent aura:** Particle system that stays active around boss
- Red/orange particles swirling around boss
- Looping emission

**Enrage Aura:**
- Shape: Circle, radius 1.0
- Emission: 20/sec continuous
- Lifetime: 1s
- Color: Red → Orange
- Size over lifetime: shrink
- Simulate in world space: false (follows boss)

### Acceptance Criteria

- Entrance VFX plays when boss fight starts
- Phase change VFX plays at 50% HP transition
- Enrage aura activates at 20% HP and persists until death
- All effects auto-cleanup (no orphaned particle systems)
- Screen shake is brief and non-disorienting
- Effects work with existing camera system
