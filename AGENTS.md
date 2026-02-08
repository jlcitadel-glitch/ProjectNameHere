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
4. **PUSH TO REMOTE** - This is MANDATORY:
   ```bash
   git pull --rebase
   bd sync
   git push
   git status  # MUST show "up to date with origin"
   ```
5. **Verify** - All changes committed AND pushed, all bd tasks updated
6. **Hand off** - Provide context summary for next session

**CRITICAL RULES:**
- Work is NOT complete until `git push` succeeds
- NEVER stop before pushing - that leaves work stranded locally
- NEVER say "ready to push when you are" - YOU must push
- If push fails, resolve and retry until it succeeds

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

## Unity-Specific Notes

- **Prefab changes** require Prefab Mode editing - note this in task descriptions
- **Scene conflicts** are common in Unity - call out scene-touching tasks explicitly
- **ScriptableObject** references can break if assets are renamed - track renames as tasks
- Always verify in Play Mode after completing implementation tasks
