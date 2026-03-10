# Baseline Rubric Scores (2026-03-10)

> Scored after Phase 3 structural refactoring (modular conversion + shared protocol + Theory of Mind).
> Before Phase 4 targeted refinement.

## Summary

| Agent | Identity | Theory | Edge Case | Progressive | Boundaries | Actionable | Concise | **Total** | Tier | Weighted Gap |
|-------|----------|--------|-----------|-------------|------------|------------|---------|-----------|------|--------------|
| architect | 5 | 5 | 5 | 5 | 5 | 5 | 5 | **35** | 1 | 0 |
| enemy-behavior | 5 | 5 | 5 | 4 | 5 | 5 | 4 | **33** | 2 | 4 |
| camera | 5 | 5 | 4 | 5 | 4 | 5 | 5 | **33** | 3 | 2 |
| player | 5 | 4 | 5 | 4 | 5 | 5 | 4 | **32** | 1 | 9 |
| sound-design | 5 | 5 | 4 | 4 | 4 | 5 | 4 | **31** | 3 | 4 |
| environment | 5 | 5 | 3 | 4 | 5 | 4 | 4 | **30** | 2 | 10 |
| vfx | 4 | 5 | 4 | 5 | 4 | 4 | 3 | **29** | 3 | 6 |
| systems | 5 | 4 | 5 | 3 | 4 | 4 | 3 | **28** | 1 | 21 |
| ui-ux | 4 | 4 | 2 | 3 | 2 | 3 | 2 | **20** | 2 | 30 |

## Refinement Targets (Phase 4)

### Priority 1: ui-ux (weighted gap: 30)
- Edge Case Coverage: Add project-specific gotchas (button wiring, UISoundBank loading, CharacterCreation dual paths, equipment preview triple hardcode)
- Cross-Agent Boundaries: Add owner/collaborator table, reference _shared/boundaries.md
- Conciseness: Trim bloated sub-files

### Priority 2: systems (weighted gap: 21)
- Progressive Disclosure: Compact owned scripts listing in CLAUDE.md
- Conciseness: Remove redundancy between router and sub-files
- Actionability: Add EnsureNewManager() code example in boot-sequence.md

### Priority 3: environment (weighted gap: 10)
- Edge Case Coverage: Document CompositeCollider2D quirks, platform parenting, hazard collision matrix

### Priority 4: player (weighted gap: 9)
- Conciseness: Clarify IAbility interface status (aspirational vs. current)
- Theory of Mind: Add concrete anecdotes to domain rules

### Cross-cutting fix: boundaries.md reference
camera, sound-design, vfx all missing explicit _shared/boundaries.md reference.
