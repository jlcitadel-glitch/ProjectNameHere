# Agent Instructions

> **Project:** ProjectNameHere (Unity 6 2D Metroidvania)
> **Standards:** See [CLAUDE.md](CLAUDE.md) for coding conventions, architecture, and constants.

This project uses **bd** (beads) for persistent issue tracking across agent sessions. Run `bd onboard` to get started.

## Quick Reference

```bash
bd ready              # Find available work (unblocked tasks only)
bd show <id>          # View issue details and history
bd create "Title" -p <0-3> -t task  # Create task with priority
bd update <id> --claim              # Claim work (atomic assignee + status)
bd close <id> --reason "summary"    # Complete work with context
bd dep add <child> <parent>         # Link task dependencies
bd dep tree           # Visualize task hierarchy
bd sync               # Sync with git (flush + commit + push)
bd duplicates --auto-merge          # Check for duplicate issues
```

## Session Workflow

### Starting a Session

1. Run `bd ready` to see what's available
2. Review `CLAUDE.md` for project conventions
3. Claim a task: `bd update <id> --claim`
4. Read task details: `bd show <id>`

### During Work

- Follow the **RPI Pattern**: Research, Plan, Implement (see CLAUDE.md)
- Create subtasks for discovered work: `bd create "Subtask" -p 2`
- Link dependencies when tasks depend on each other: `bd dep add <child> <parent>`
- Use priority 0 for blocking bugs found during work

### Ending a Session (Landing the Plane)

**When ending a work session**, you MUST complete ALL steps below. Work is NOT complete until `git push` succeeds.

**MANDATORY WORKFLOW:**

1. **File issues for remaining work** - Create bd issues for anything that needs follow-up
2. **Run quality gates** (if code changed) - Verify in Unity Editor, check for null refs
3. **Update issue status** - Close finished work, update in-progress items
4. **Write handoff** - Write `handoffs/<agent-name>.json` per the schema in `handoffs/SCHEMA.md`, then append to `handoffs/activity.jsonl`:
   ```
   $(date -Iseconds)|<AGENT>|session_end|<bead_id>|<status>|<summary>
   ```
5. **PUSH TO REMOTE** - This is MANDATORY:
   ```bash
   git pull --rebase
   bd sync
   git push
   git status  # MUST show "up to date with origin"
   ```
6. **Verify** - All changes committed AND pushed, all bd tasks updated
7. **Hand off** - Provide context summary for next session

**CRITICAL RULES:**
- Work is NOT complete until `git push` succeeds
- NEVER stop before pushing - that leaves work stranded locally
- NEVER say "ready to push when you are" - YOU must push
- If push fails, resolve and retry until it succeeds

## Session Handoff (GUPP-Lite)

Session handoffs ensure context survives across agent sessions.

### On Session End

1. Write `handoffs/<agent-name>.json` following the schema in [`handoffs/SCHEMA.md`](handoffs/SCHEMA.md)
2. Append to `handoffs/activity.jsonl`:
   ```
   $(date -Iseconds)|<AGENT>|session_end|<bead_id>|<status>|<summary>
   ```
3. The handoff file is overwritten each session (latest state only)

### On Session Start

1. Check for `handoffs/<agent-name>.json` — if present, read it for prior context
2. If resuming the same bead, pick up from `remaining` and `next_steps`
3. If starting new work, note the prior handoff for awareness then proceed normally

### Activity Log

The `handoffs/activity.jsonl` file is an append-only log of all agent activity:

```
TIMESTAMP|AGENT|EVENT|BEAD_ID|STATUS|MESSAGE
```

Events: `session_start`, `session_end`, `discovery`

---

## Task Organization

### Priority Levels

| Priority | Use For |
|----------|---------|
| 0 (Critical) | Blocking bugs, broken builds, data loss |
| 1 (High) | Current sprint features, important fixes |
| 2 (Normal) | Planned work, enhancements |
| 3 (Low) | Nice-to-have, future ideas, minor polish |

### Hierarchical Tasks

Use parent-child relationships for epics:

```bash
bd create "Combat System" -p 1                    # Parent epic
bd create "HealthSystem component" -p 1            # Subtask
bd dep add <health-id> <combat-id>                 # Link as dependency
```

### Cross-Agent Work

When your task touches another agent's domain:
1. Create a bd issue in their area with clear context
2. Link it as a dependency if your work is blocked
3. Include file paths and expected behavior in the description

## Discovery Protocol

When encountering out-of-scope work during a task, **do not context-switch**. Instead:

1. Continue your current task — do not stop or pivot
2. File a bead: `bd create "Discovered: <title>" -p <priority> -l agent:<target>`
3. Set dependencies if needed: `bd dep add <new-id> <current-id>`
4. Note the discovery in your current bead: `bd comments add <current-id> "Discovered: <new-id> - <desc>"`
5. Log it: append to `handoffs/activity.jsonl`:
   ```
   $(date -Iseconds)|<AGENT>|discovery|<current-bead>|in_progress|Filed <new-id>: <title>
   ```
6. Record the new bead ID in your handoff JSON `beads_created` array
7. Return to your current task

This ensures nothing is lost while keeping agents focused on their primary work.

---

## Witness Check

After closing a bead, run the witness check script to validate closure:

```bash
bash scripts/witness_check.sh <bead-id>
```

The script verifies:
1. A matching handoff JSON exists for the bead
2. Bead status matches handoff status
3. Files listed in `files_touched` appear in recent git history
4. CI passes (if available)

If any check fails, the script reopens the bead with a comment explaining the failure. Run this as part of the landing protocol after `bd close`.

---

## Status Report

Generate a cross-agent coordination report:

```bash
bash scripts/agent_status_report.sh
```

The report includes:
- Bead overview (open/closed/blocked counts)
- Agent handoff status (last session time and status per agent)
- Stale beads (open for 3+ days)
- Orphaned handoffs (handoff says in_progress but bead is closed)
- Recent activity (last 24h from activity log)
- Recently closed beads (last 7 days)

Use this to monitor project health and identify coordination issues.

---

## Unity-Specific Notes

- **Prefab changes** require Prefab Mode editing - note this in task descriptions
- **Scene conflicts** are common in Unity - call out scene-touching tasks explicitly
- **ScriptableObject** references can break if assets are renamed - track renames as tasks
- Always verify in Play Mode after completing implementation tasks
