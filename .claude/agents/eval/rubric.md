# Agent CLAUDE.md Quality Rubric

> Score each dimension 1-5. Used to evaluate agent responses to eval prompts.

## Dimensions

### 1. Identity Clarity (1-5)
Does the agent know exactly what it owns and doesn't own?

| Score | Criteria |
|-------|----------|
| 1 | No mention of ownership; would attempt any task |
| 2 | Lists owned scripts but no boundaries |
| 3 | Owns scripts + knows some boundaries |
| 4 | Clear ownership + explicit "not my domain" awareness |
| 5 | Ownership + boundaries + knows who to hand off to |

### 2. Theory of Mind (1-5)
Does the agent explain WHY rules exist, not just WHAT they are?

| Score | Criteria |
|-------|----------|
| 1 | Rules stated as bare commands ("do X") |
| 2 | Some rules have rationale |
| 3 | Most domain rules have "because" clauses |
| 4 | Rules + rationale + consequences of violation |
| 5 | Rules + rationale + consequences + examples of when the rule saved time |

### 3. Edge Case Coverage (1-5)
Does the agent handle known gotchas from MEMORY.md and past sessions?

| Score | Criteria |
|-------|----------|
| 1 | No mention of common issues |
| 2 | Generic troubleshooting list |
| 3 | Project-specific gotchas listed |
| 4 | Gotchas with root cause AND fix |
| 5 | Gotchas with root cause + fix + prevention strategy |

### 4. Progressive Disclosure (1-5)
Is info loaded only when needed, or is everything dumped upfront?

| Score | Criteria |
|-------|----------|
| 1 | Everything in one monolithic file (200+ lines) |
| 2 | Sections exist but all loaded at once |
| 3 | Some info in sub-files, but core file still bloated |
| 4 | Thin router CLAUDE.md + task-specific sub-files |
| 5 | Router + sub-files + sub-files have clear load triggers |

### 5. Cross-Agent Boundaries (1-5)
Are handoff points to other agents explicit?

| Score | Criteria |
|-------|----------|
| 1 | No mention of other agents |
| 2 | Mentions "file a bead" generically |
| 3 | Names specific agents for specific scenarios |
| 4 | Boundary table with owner/collaborator roles |
| 5 | Boundary table + references _shared/boundaries.md + examples |

### 6. Actionability (1-5)
Can the agent act on instructions without ambiguity?

| Score | Criteria |
|-------|----------|
| 1 | Vague guidance ("follow best practices") |
| 2 | Some concrete instructions mixed with vague |
| 3 | Concrete instructions for common tasks |
| 4 | Step-by-step procedures for common tasks + code patterns |
| 5 | Procedures + code patterns + decision trees for edge cases |

### 7. Conciseness (1-5)
Does every line earn its place?

| Score | Criteria |
|-------|----------|
| 1 | Massive file with redundant/obvious content |
| 2 | Some bloat but mostly useful |
| 3 | Reasonable length, minor redundancy |
| 4 | Tight — no line is wasted |
| 5 | Tight + progressive disclosure handles depth on demand |

## Scoring Template

```markdown
## [Agent Name] — Eval Prompt: "[prompt text]"

| Dimension | Score | Notes |
|-----------|-------|-------|
| Identity Clarity | /5 | |
| Theory of Mind | /5 | |
| Edge Case Coverage | /5 | |
| Progressive Disclosure | /5 | |
| Cross-Agent Boundaries | /5 | |
| Actionability | /5 | |
| Conciseness | /5 | |
| **Total** | **/35** | |
```

## Priority Weighting

For refinement prioritization, multiply score gaps by usage frequency:

| Tier | Agents | Weight |
|------|--------|--------|
| Tier 1 | architect, player, systems | x3 |
| Tier 2 | enemy-behavior, ui-ux, environment | x2 |
| Tier 3 | camera, sound-design, vfx | x1 |

**Refinement priority** = (35 - total_score) * weight
