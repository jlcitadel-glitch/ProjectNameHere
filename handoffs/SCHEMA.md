# Handoff Schema (GUPP-Lite v1)

Agents write a handoff JSON file at the end of each session to preserve context for the next session.

## File Location

- Handoff file: `handoffs/<agent-name>.json`
- Activity log: `handoffs/activity.jsonl`

## JSON Schema

```json
{
  "$schema": "handoff-v1",
  "agent": "<agent-name>",
  "bead_id": "<current-bead-id>",
  "status": "completed | in_progress | blocked",
  "timestamp": "<ISO-8601>",
  "session_summary": "<one-line summary>",
  "done": ["<completed items>"],
  "remaining": ["<remaining items>"],
  "decisions": ["<key decisions and reasoning>"],
  "files_touched": ["<repo-relative paths>"],
  "beads_created": ["<bead IDs filed this session>"],
  "next_steps": ["<prioritized actionable items>"]
}
```

## Field Reference

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `$schema` | string | yes | Always `"handoff-v1"` |
| `agent` | string | yes | Agent name (e.g., `"player"`, `"enemy-behavior"`) |
| `bead_id` | string | yes | The bead ID being worked on |
| `status` | enum | yes | `"completed"`, `"in_progress"`, or `"blocked"` |
| `timestamp` | string | yes | ISO-8601 timestamp of session end |
| `session_summary` | string | yes | One-line summary of what was accomplished |
| `done` | string[] | yes | List of completed items this session |
| `remaining` | string[] | yes | List of items still to do (empty if completed) |
| `decisions` | string[] | no | Key decisions made and reasoning |
| `files_touched` | string[] | yes | Repo-relative paths of modified files |
| `beads_created` | string[] | no | IDs of beads filed during this session |
| `next_steps` | string[] | yes | Prioritized actionable items for next session |

## Activity Log Format

Each session appends one line to `handoffs/activity.jsonl`:

```
TIMESTAMP|AGENT|EVENT|BEAD_ID|STATUS|MESSAGE
```

| Field | Description |
|-------|-------------|
| `TIMESTAMP` | ISO-8601 with seconds |
| `AGENT` | Agent name |
| `EVENT` | `session_start`, `session_end`, `discovery` |
| `BEAD_ID` | Current bead ID (or `-` if none) |
| `STATUS` | Bead status at time of event |
| `MESSAGE` | One-line description |

## Example

```json
{
  "$schema": "handoff-v1",
  "agent": "enemy-behavior",
  "bead_id": "beads-042",
  "status": "in_progress",
  "timestamp": "2026-02-10T14:30:00-05:00",
  "session_summary": "Implemented mushroom enemy attack patterns",
  "done": [
    "Created MushroomAttack1 and MushroomAttack2 ScriptableObjects",
    "Wired attack hitboxes with correct timing"
  ],
  "remaining": [
    "Add hurt/death animations",
    "Tune attack cooldowns for wave scaling"
  ],
  "decisions": [
    "Used two separate attack assets instead of one multi-phase attack for easier balancing"
  ],
  "files_touched": [
    "Assets/_Project/ScriptableObjects/Enemies/Attacks/MushroomAttack1.asset",
    "Assets/_Project/ScriptableObjects/Enemies/Attacks/MushroomAttack2.asset"
  ],
  "beads_created": ["beads-045"],
  "next_steps": [
    "Add mushroom hurt/death animations (beads-045)",
    "Test mushroom in wave 5+ for scaling balance"
  ]
}
```
