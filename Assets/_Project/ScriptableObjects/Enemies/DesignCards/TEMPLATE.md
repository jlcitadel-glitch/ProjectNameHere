# Enemy Design Card: [ENEMY NAME]

> Fill out every step before implementation. Each answer should be 1–3 sentences
> unless a table is provided. If you can't answer a step, the enemy isn't ready to build.

---

## Header

| Field | Value |
|-------|-------|
| **Enemy Name** | |
| **Enemy Type** | Ground / Flying / Hopping / Stationary |
| **Introduced** | Zone or wave range where the player first encounters it |
| **Design Date** | |

---

## Step 1 — Combat Role

*What job does this enemy perform in an encounter?*

**Primary Role:** [Tank / DPS / Support / Controller / Artillery]

**One-liner:** Describe the enemy's purpose in a single sentence. What does the encounter
lose if you remove this enemy?

> Roles reference:
> - **Tank** — Slow, absorbs attention, blocks paths or body-blocks for allies
> - **DPS** — Fragile but punishing, rewards the player for prioritizing it
> - **Support** — Buffs, heals, shields, or summons other enemies
> - **Controller** — Restricts player movement via zones, slows, knockback, terrain denial
> - **Artillery** — Ranged pressure from a distance, forces repositioning

---

## Step 2 — Behavior Axes

*Where does this enemy sit in the design space? Rate each axis 1–5.*

| Axis | Rating | Reasoning |
|------|--------|-----------|
| **Aggression** (passive ↔ relentless) | /5 | |
| **Mobility** (stationary ↔ highly mobile) | /5 | |
| **Range** (melee-only ↔ long-range) | /5 | |
| **Predictability** (erratic ↔ telegraphed) | /5 | |
| **Persistence** (disengages easily ↔ never lets go) | /5 | |

**Nearest neighbor:** Which existing enemy is closest on these axes? How do they differ
on at least 2 axes? If you can't differentiate on 2+ axes, reconsider the design — they'll
feel like reskins.

---

## Step 3 — Player Verb Tested

*What player skill does this enemy teach, test, or demand?*

**Primary verb:** The core mechanic the player must use (e.g., parry, dodge-through,
jump-over, prioritize, patience, spacing, aerial combat).

**Playstyle punished:** What lazy or dominant strategy does this enemy disrupt?
(e.g., "punishes standing still," "counters pure melee rushdown," "breaks ranged-only play")

---

## Step 4 — Threat Clock

*How quickly does this enemy become dangerous after spawning or detecting the player?*

**Category:** [Immediate / Short Fuse / Delayed / Ambient]

**Time-to-threat:** Approximate seconds from detection to first attack reaching the player.

> Clock reference:
> - **Immediate** — Dangerous on contact, always a threat (0–0.5s)
> - **Short Fuse** — Closes fast once triggered, demands quick reaction (0.5–2s)
> - **Delayed** — Winds up a big threat, punishes if ignored (2–5s)
> - **Ambient** — Passive until a condition triggers escalation (variable)

---

## Step 5 — Movement Identity

*How does this enemy own space and move through the arena?*

**Archetype:** [Patrol / Chase / Orbit / Ambush / Swarm / Anchor]

**Physics model:** How does it move mechanically? (e.g., "impulse-based hops with no air
control" or "smooth SmoothDamp chase with sinusoidal hover")

**Spatial claim:** What area of the arena does it effectively "own" or deny through its
movement pattern?

> Archetype reference:
> - **Patrol** — Predictable path, learnable, creates windows
> - **Chase** — Reactive to player position, closes distance
> - **Orbit** — Maintains preferred distance, strafes or circles
> - **Ambush** — Hidden or inactive until triggered, then bursts
> - **Swarm** — Weak solo, overwhelms in groups, fills space
> - **Anchor** — Controls a zone, does not leave it

---

## Step 6 — Encounter Synergies

*What enemies does it pair with, and how does the combination create pressure?*

**Training combo:** A pairing that teaches the player how this enemy works in a
low-stakes context. Name the partner and describe why it works.

**Nightmare combo:** A pairing that creates genuine pressure by layering complementary
threats. Name the partner and describe the interaction.

**Bad pairing:** An enemy it should NOT be paired with, and why. (Redundant roles,
unreadable chaos, frustration stacking, etc.)

**Tempo impact:** Does fighting this enemy drain cooldowns, tax health, consume resources,
or create downtime that affects the player's state going into the next encounter?

---

## Step 7 — Commitment Profile

*How reactive is this enemy to the player, and where are the openings?*

**Reactivity:** [Pattern-locked / Reactive / Punishing]
- **Pattern-locked** — Fixed cycle, player adapts to the rhythm
- **Reactive** — Adjusts behavior based on player position/state
- **Punishing** — Specifically counters player actions (anti-air, gap-close, etc.)

**Commitment level:** How locked-in is the enemy once it starts an action?
(e.g., "fully committed during hop arc, no air steering")

**Exploit window:** The specific moment the player should attack, and how long it lasts.

**Chaos readability:** Is this enemy's telegraph still readable when 3+ enemies are on
screen simultaneously? What makes it visually/audibly distinct?

---

## Step 8 — Counterplay Map

*How does the player dismantle this enemy?*

**Primary counterplay:** The intended, clean way to beat it. The strategy the design
rewards most.

**Secondary counterplay:** An alternative approach that works but is less efficient or
riskier.

**What fails:** A strategy that seems like it should work but doesn't, and why. The
player must learn this through failure.

**Ability interaction:** How do unlockable abilities (dash, double jump, parry, etc.)
change the matchup? Does any ability trivialize it? If so, is that intentional?

---

## Step 9 — Death Consequence

*What happens to the arena when this enemy dies?*

**On-death effect:** Does it leave a hazard, spawn smaller enemies, drop a pickup, buff
nearby allies, explode, or die clean?

**Encounter ripple:** How does its death change the remaining encounter? Does removing it
relieve pressure, create an opening, or trigger an escalation?

**Wave relevance:** In wave/survival mode, does its death contribute to snowball or
reset dynamics?

---

## Step 10 — Density Scaling

*How does this enemy's design hold up in groups?*

| Count | Behavior |
|-------|----------|
| **1x (solo)** | |
| **3x (small group)** | |
| **5x+ (swarm)** | |

**Swarm viability:** Is this enemy designed to appear in large numbers, or is it a
solo/duo threat only?

**Overlap problem:** When multiples are on screen, do their patterns create unreadable
overlap, unavoidable damage, or degenerate into noise? How is this mitigated?

---

## Design Validation Checklist

Before signing off on this card:

- [ ] Nearest neighbor differs on 2+ behavior axes
- [ ] Primary counterplay is learnable without external guidance
- [ ] At least one synergy combo defined for encounter design
- [ ] Not trivially solved by a single unlockable ability (or intentionally so, noted above)
- [ ] Readable at 3x density in wave mode
- [ ] Death consequence considered (even if "clean death" is the answer)
