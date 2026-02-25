# Agent Architecture Overview

## System Architecture

```mermaid
graph TB
    subgraph CONFIG["📋 Project Configuration"]
        CLAUDE_MD["CLAUDE.md<br/>Architecture, constants, packages"]
        STANDARDS["STANDARDS.md<br/>Unity 6 APIs, conventions, CI"]
        AGENTS_MD["AGENTS.md<br/>Beads workflow, session protocol"]
        ROUTING[".claude/agents/routing.json<br/>Label-based dispatch rules"]
    end

    subgraph INFRA["⚙️ Infrastructure"]
        RUNNER["agent-runner.ps1<br/>PowerShell polling loop (30s)<br/>Launches claude -p per task"]
        BD["bd (beads CLI)<br/>Dolt-backed issue tracker<br/>Labels, deps, sync"]
        GIT["Git + GitHub<br/>Code persistence<br/>bd sync hooks"]
    end

    subgraph MEMORY["🧠 Agent Memory"]
        HANDOFF_SCHEMA["handoffs/SCHEMA.md<br/>GUPP-Lite v1 format"]
        ACTIVITY["handoffs/activity.jsonl<br/>Append-only event log"]
        AGENT_HANDOFFS["handoffs/<agent>.json<br/>Last session state per agent"]
        AGENT_LOCAL[".claude/agents/<name>/<br/>handoffs/<name>.json<br/>Per-agent local handoff copy"]
        SETTINGS[".claude/agents/<name>/<br/>.claude/settings.local.json<br/>Per-agent permissions"]
    end

    RUNNER -->|"polls bd ready<br/>--label agent:X"| BD
    BD -->|"work found"| RUNNER
    RUNNER -->|"launches claude -p<br/>with task prompt"| AGENTS
    BD <-->|"bd sync"| GIT
    CONFIG -.->|"read on session start"| AGENTS
    AGENT_HANDOFFS -.->|"context recovery"| AGENTS
    AGENTS -.->|"write on session end"| AGENT_HANDOFFS
    AGENTS -.->|"append events"| ACTIVITY

    subgraph AGENTS["🤖 Agent Pool (9 Agents)"]
        direction TB

        subgraph CORE_AGENTS["Core Gameplay"]
            PLAYER["🎮 player<br/>─────────────<br/>Movement, input,<br/>abilities, dash,<br/>double jump, powerups<br/>─────────────<br/>⏳ b8z: Consolidate<br/>input assets"]
            COMBAT_ENEMY["👾 enemy-behavior<br/>─────────────<br/>AI state machines,<br/>bosses, sensors,<br/>wave spawning<br/>─────────────<br/>🔒 6bd: Boss encounters<br/>🔒 80c: Zone grouping"]
            SYSTEMS_AGENT["🔧 systems<br/>─────────────<br/>GameManager, Save,<br/>Health, Mana, XP,<br/>Wind, Cutscenes<br/>─────────────<br/>⏳ mn2: Inventory<br/>⏳ bxi: Balance curves"]
        end

        subgraph WORLD_AGENTS["World & Presentation"]
            ENVIRONMENT["🏰 environment<br/>─────────────<br/>Tilemaps, platforms,<br/>hazards, doors,<br/>levers, interactables<br/>─────────────<br/>🔒 r7p: Exploration gates"]
            CAMERA["📷 camera<br/>─────────────<br/>Follow, parallax,<br/>bounds, boss rooms,<br/>shake, zoom<br/>─────────────<br/>No active beads"]
            VFX_AGENT["✨ vfx<br/>─────────────<br/>Particles, fog,<br/>precipitation,<br/>hit/death effects<br/>─────────────<br/>No active beads"]
            SOUND["🔊 sound-design<br/>─────────────<br/>SFXManager, Music,<br/>UISoundBank,<br/>audio pipeline<br/>─────────────<br/>No active beads"]
        end

        subgraph META_AGENTS["Meta & Interface"]
            ARCHITECT["📐 architect<br/>─────────────<br/>Patterns, CI, reviews,<br/>tech debt, system<br/>design, refactoring<br/>─────────────<br/>⏳ 9ag: Prefab audit<br/>⏳ i17: Zone system<br/>⏳ 17p: Unit tests<br/>⏳ buu: CI pipeline"]
            UI_UX["🎨 ui-ux<br/>─────────────<br/>Gothic UI, menus,<br/>HUD, skill trees,<br/>accessibility<br/>─────────────<br/>🔒 4za: World map"]
        end
    end

    style CONFIG fill:#1a1a2e,stroke:#e94560,color:#eee
    style INFRA fill:#1a1a2e,stroke:#0f3460,color:#eee
    style MEMORY fill:#1a1a2e,stroke:#16213e,color:#eee
    style AGENTS fill:#0f3460,stroke:#533483,color:#eee
    style CORE_AGENTS fill:#16213e,stroke:#e94560,color:#eee
    style WORLD_AGENTS fill:#16213e,stroke:#0f3460,color:#eee
    style META_AGENTS fill:#16213e,stroke:#533483,color:#eee
```

## Current Blocking Chain

```mermaid
graph LR
    subgraph COMPLETED["✅ Completed"]
        SO["1s2: ScriptableObject<br/>data assets<br/><b>P0 ✓ CLOSED</b>"]
    end

    subgraph BOTTLENECK["🚧 Critical Path (In Progress)"]
        PREFABS["9ag: Audit prefabs<br/><b>P0 ⏳ architect</b>"]
        ZONES["i17: Multi-scene<br/>zone system<br/><b>P1 ⏳ architect</b>"]
    end

    subgraph BLOCKED["🔒 Blocked (6 beads)"]
        CI["1j4: Prefab CI check<br/>P2"]
        BOSSES["6bd: Boss encounters<br/>P1"]
        ENEMIES["80c: Zone enemy groups<br/>P1"]
        EXPLORE["r7p: Exploration gates<br/>P1"]
        NPC["7hq: NPC/dialogue<br/>P3"]
        MAP["4za: World map UI<br/>P3"]
    end

    SO -->|"unblocked"| ZONES
    PREFABS --> CI
    ZONES --> BOSSES
    ZONES --> ENEMIES
    ZONES --> EXPLORE
    ZONES --> NPC
    ZONES --> MAP

    style COMPLETED fill:#1b4332,stroke:#40916c,color:#eee
    style BOTTLENECK fill:#7f4f24,stroke:#dda15e,color:#eee
    style BLOCKED fill:#6a040f,stroke:#d00000,color:#eee
```

## Agent Session Lifecycle

```mermaid
sequenceDiagram
    participant R as agent-runner.ps1
    participant BD as bd (beads)
    participant C as claude -p
    participant G as Git/GitHub

    loop Every 30s
        R->>BD: bd ready --label agent:X --json --limit 1
        BD-->>R: [] (no work) or [{issue}]
    end

    Note over R,BD: Work found!

    R->>C: Launch with task prompt
    activate C

    C->>C: Read STANDARDS.md + CLAUDE.md
    C->>C: Read handoffs/<agent>.json (prior context)
    C->>BD: bd update <id> --claim
    C->>BD: bd show <id>

    Note over C: RPI: Research → Plan → Implement

    C->>C: Write/edit code files
    C->>BD: bd close <id> --reason "summary"
    C->>C: Write handoffs/<agent>.json
    C->>C: Append to activity.jsonl
    C->>G: git add + commit + pull --rebase + push
    C->>BD: bd sync

    deactivate C
    C-->>R: Exit (code 0)
    R->>R: Cooldown 10s, resume polling
```

## Project Stats Snapshot

| Metric | Count |
|--------|-------|
| Total beads | 62 |
| Closed | 49 |
| In Progress | 7 |
| Open (unblocked) | 0 |
| Blocked | 6 |
| Ready to work | 0 |
| Agents with handoffs | 3 (vfx, systems, sound-design) |

**Legend:** ⏳ In Progress | 🔒 Blocked | ✅ Completed
