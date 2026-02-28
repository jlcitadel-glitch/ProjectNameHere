# Enemy Design Card: Slime

> Retroactive design card — filled from existing implementation in SlimeData.asset,
> SlimeAttack.asset, and Slime.prefab.

---

## Header

| Field | Value |
|-------|-------|
| **Enemy Name** | Slime |
| **Enemy Type** | Hopping (HoppingMovement) |
| **Introduced** | Wave 1 — first enemy the player encounters |
| **Design Date** | Retroactive — Feb 2026 |

---

## Step 1 — Combat Role

**Primary Role:** DPS

**One-liner:** A punchy melee bruiser that hops into range and lunges for high damage,
punishing players who let it close the gap.

The encounter loses its primary ground-level melee pressure without the Slime. It's the
baseline "you must respect melee threats" teacher. Not tanky enough to block paths (30 HP),
no ranged options, no support utility — it exists purely to deal damage and die if the
player plays correctly.

---

## Step 2 — Behavior Axes

| Axis | Rating | Reasoning |
|------|--------|-----------|
| **Aggression** | 3/5 | Chases on detection (range 6) but gated by hop cooldown — 0.5s gaps between approaches in chase mode |
| **Mobility** | 2/5 | Hop-only locomotion. Grounded between jumps, no air control, slowest chase speed in roster (5). Completely immobile during cooldown |
| **Range** | 1/5 | Pure melee. 1.5 attack range, no projectile, no area denial |
| **Predictability** | 4/5 | Fixed hop rhythm, 0.2s wind-up telegraph with visible squash animation. Highly readable pattern |
| **Persistence** | 3/5 | Chases within 6-unit detection, loses aggro at 10 units. Moderately sticky — won't follow forever but doesn't give up easily within its range band |

**Nearest neighbor:** Mushroom (3, 2, 1, 4, 3) — nearly identical on all 5 axes.
Mushroom trades speed for tankiness (HP 35 vs 30, chase speed 4 vs 5, cooldown 1.2s vs
1.0s). These two are dangerously close. Mushroom needs differentiation on at least
Range or Persistence to justify coexistence — a poison spit (Range → 3) or death-cloud
area denial (shifting it toward Controller role) would separate them.

---

## Step 3 — Player Verb Tested

**Primary verb:** Spacing — maintain distance from a melee threat, exploit the grounded
cooldown windows between hops to deal damage safely.

**Secondary verb:** Jump-over — the hop arc is predictable and the player can jump over
the Slime mid-hop to reposition behind it, buying time while it turns around.

**Playstyle punished:** Passive/stationary play. Players who stand still or fight at
close range without retreating eat 15-damage lunges every second. The Slime forces the
player to move and creates the foundational habit of "don't stand in melee range of
things that hit hard."

---

## Step 4 — Threat Clock

**Category:** Short Fuse

**Time-to-threat:** ~1.5–2 seconds from detection to first lunge landing. Detection at
6 units, chase speed 5, two hops to close (0.5s cooldown between them), then 0.2s
wind-up on lunge.

Faster than Skeleton (~3s, Delayed) or Mushroom (~2.5s, Delayed). Slower than Bat
(~0.5s, Immediate — flies straight in). The Slime sits in the "you have time to react
but not to ignore" sweet spot appropriate for a Wave 1 enemy.

---

## Step 5 — Movement Identity

**Archetype:** Chase (with Patrol fallback)

**Physics model:** Impulse-based hops. Full velocity applied at moment of jump
(hopForce: 7 vertical, hopHorizontalSpeed: 2.5 patrol / chaseSpeed: 5 pursuit).
Zero air control — committed to the arc once airborne. 2.5x gravity multiplier on
the downward arc for snappy landings. Velocity resets to zero on landing, then waits
for hop cooldown (1.0s patrol, 0.5s chase) before next hop.

**Spatial claim:** The Slime claims a horizontal lane roughly 6 units wide (its detection
range). Its hop arcs own the ground-level space — the airspace above the arc is safe.
Between hops it claims nothing; it's a sitting target. Its spatial presence is rhythmic,
not constant.

---

## Step 6 — Encounter Synergies

**Training combo: Slime + Bat**
Bat forces the player to look up and deal with an aerial Immediate threat while the
Slime closes on the ground. Low combined HP (45) keeps stakes manageable. Teaches
vertical threat triage — clear the fast Bat first, then handle the approaching Slime
during its cooldown window.

**Nightmare combo: Slime + Skeleton**
Skeleton's LineOfSight detection (range 8) and slow, tanky presence (50 HP) creates
ranged pressure that pushes the player forward — directly into the Slime's hop range.
The player must split attention between dodging Skeleton attacks and managing Slime
spacing. Neither can be easily ignored: Skeleton is too durable to burst, Slime is too
aggressive to leave alone.

**Bad pairing: Slime + Mushroom**
Two hoppers with nearly identical behavior profiles. The player can't distinguish threat
priority — they look different but *play* the same. Creates redundant pressure without
tactical depth. Avoid pairing until Mushroom is differentiated (see Step 2 notes).

**Tempo impact:** Low resource drain per kill (30 HP dies fast to any focused attention).
But the Slime taxes *positioning* — the player must reposition to maintain spacing, which
can push them into other threats or away from objectives. In wave mode, Slimes primarily
tax time and attention rather than health, unless the player gets careless.

---

## Step 7 — Commitment Profile

**Reactivity:** Pattern-locked — hops toward last known player position at fixed intervals.
Does not lead targets, does not adjust mid-hop, does not react to player actions.

**Commitment level:** Fully committed during hop arc. No air steering, no ability to
cancel. Once airborne, the trajectory is locked. On the ground during cooldown, it's
also committed — to standing still. Both states are exploitable.

**Exploit window:** The 0.5s–1.0s grounded cooldown between hops. The Slime is stationary,
has no active hitbox, and is waiting for its next hop timer. This is the intended damage
window. Secondary window: mid-hop airborne phase where the player can dash underneath.

**Chaos readability:** Moderate. The squash-stretch attack animation is visually distinct
at close range but could blend at distance when multiple Slimes overlap. The hop arc
itself is the primary read — players track the parabolic trajectory. At 3+ Slimes,
overlapping hop timings create semi-random landing patterns that are harder to read.
Mitigation: Slimes are slow enough (0.5s cooldown) that individual trajectories remain
trackable up to ~4 simultaneously.

---

## Step 8 — Counterplay Map

**Primary counterplay:** Spacing + cooldown punishment. Stay outside the 1.5-unit attack
range, let the Slime hop toward you, then hit it during the grounded cooldown window.
Repeat. This is the intended loop — approach, retreat, punish the gap.

**Secondary counterplay:** Jump over the hop arc and attack from behind while it turns
around. Riskier because it requires precise timing (the hop arc height varies with
distance) but rewards aggressive players with a longer damage window as the Slime
reorients.

**What fails:** Trading hits at close range. The Slime's 15-damage lunge on a 1.0s
cooldown out-DPSes most early-game player damage. The 0.2s wind-up is too short to
consistently react to at melee range — it's designed to catch greedy players. Standing
still and swinging is a trap.

**Ability interaction:**
- **Dash** — Significantly changes the matchup. Dash through the hop arc to avoid
  contact, then punish from behind. Slime becomes near-trivial once dash is unlocked.
  This is intentional — Slime is a Wave 1 enemy and should feel progressively easier.
- **Double Jump** — Provides a safety valve for escaping hop pressure but doesn't
  fundamentally change the counterplay loop.
- **Parry** — Works against the lunge (0.2s wind-up) but requires near-perfect timing.
  High-skill reward option, not the intended primary counterplay.

---

## Step 9 — Death Consequence

**On-death effect:** Clean death. Plays death VFX, drops 3 XP orbs (experienceValue: 10).
No hazard, no spawn, no arena modification.

**Encounter ripple:** Removing a Slime relieves ground-level melee pressure. In mixed
encounters (Slime + ranged enemy), killing the Slime frees the player to focus on
positioning against the remaining threat without worrying about hop approaches. Its death
is pure relief — no new problems created.

**Wave relevance:** Clean deaths with XP drops create a positive kill-reward loop. In
wave mode, Slimes function as "income" — fast to kill, reliable XP, no death penalties.
They accelerate the player's scaling without creating snowball risk.

**Design opportunity (not yet implemented):** Slime splitting into 2 mini-slimes on death
would shift it from "income mob" to "geometric pressure" — killing one creates two
smaller problems. This would differentiate it from other clean-death enemies and create
interesting wave dynamics where killing Slimes too early floods the arena. Would require
a new Mini-Slime variant with reduced stats and no further splitting.

---

## Step 10 — Density Scaling

| Count | Behavior |
|-------|----------|
| **1x (solo)** | Clean hop-timing puzzle. Very manageable. Player learns the spacing loop with no distractions. Appropriate for Wave 1 introduction. |
| **3x (small group)** | Overlapping hop arcs begin to create floor coverage. Individual tracking is still possible but the player must manage multiple cooldown windows simultaneously. Forces prioritization — which Slime is closest to landing? |
| **5x+ (swarm)** | Hop arcs cover most of the ground level. The player is pushed to platform-based play (stay above the hop ceiling) or must use AoE/dash to manage density. Individual tracking breaks down — becomes a spatial avoidance problem rather than per-enemy timing. |

**Swarm viability:** Yes — the Slime works at swarm scale. Low HP (30) means individual
Slimes die fast to AoE or focused damage, preventing permanent overwhelm. The 0 knockback
resistance means hits scatter them, creating momentary gaps. The hop cooldown means swarms
have natural rhythm — bursts of hops followed by grounded lulls.

**Overlap problem:** At 5+ Slimes, hop timings desynchronize into pseudo-random landing
patterns. The squash-stretch attack telegraph becomes hard to distinguish per-enemy.
Mitigation: the slow hop cooldown (0.5s chase) means the density self-regulates —
there are always some Slimes grounded and some airborne, never all attacking
simultaneously. The wave system's maxAlive cap also prevents true density overload.

---

## Design Validation Checklist

- [x] Nearest neighbor differs on 2+ behavior axes — **FAILS.** Mushroom is nearly
  identical. Documented in Step 2 with differentiation recommendations.
- [x] Primary counterplay is learnable without external guidance — **PASS.** Spacing +
  cooldown punishment is discoverable through play. The grounded pause is visually obvious.
- [x] At least one synergy combo defined — **PASS.** Slime+Bat (training), Slime+Skeleton
  (nightmare) documented.
- [x] Not trivially solved by a single ability — **CONDITIONAL.** Dash trivializes it,
  but this is intentional for a Wave 1 enemy. Noted in Step 8.
- [x] Readable at 3x density — **PASS.** Individual tracking holds at 3x. Degrades
  gracefully at 5x+ into spatial avoidance.
- [x] Death consequence considered — **PASS (clean death).** Split-on-death noted as
  future design opportunity.
