# Shared Agent Protocol

> This file is referenced by all agent CLAUDE.md files. It defines session lifecycle, handoff, and discovery procedures that are identical across agents.

## Session Start

1. Read [STANDARDS.md](../../../STANDARDS.md) for project invariants
2. Check `../../../handoffs/<agent-name>.json` — if present, read it for context awareness
3. Wait for user instructions — do NOT auto-claim or start work on beads

## Mandatory Standards

**You MUST follow [STANDARDS.md](../../../STANDARDS.md) in full.** Key requirements:
- **RPI Pattern**: Research → Plan (get user approval) → Implement. Never skip the Plan step, because skipping it leads to wasted work when the approach doesn't match what was needed.
- All code conventions, null safety, performance rules, and CI requirements apply.
- Violations of STANDARDS.md are not acceptable regardless of task urgency.

## Session Handoff Protocol

On **session start**: Check `../../../handoffs/<agent-name>.json`. If it exists, read it for prior context. If resuming the same bead, pick up from `remaining` and `next_steps`.

On **session end**: Write `../../../handoffs/<agent-name>.json` per the schema in `../../../handoffs/SCHEMA.md`. Append to `../../../handoffs/activity.jsonl`:
```
$(date -Iseconds)|<agent-name>|session_end|<bead_id>|<status>|<summary>
```

See [AGENTS.md](../../../AGENTS.md) for full protocol.

## Discovery Protocol

When you find work outside your current task: **do not context-switch**, because context-switching fragments focus and increases the chance of incomplete work. Instead:

1. File a bead: `bd create "Discovered: <title>" -p <priority> -l agent:<target>`
2. Set dependencies if needed
3. Note it in your current bead
4. Continue your current task

See [AGENTS.md](../../../AGENTS.md) for full protocol.
