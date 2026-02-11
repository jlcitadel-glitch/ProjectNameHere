#!/usr/bin/env bash
# agent_status_report.sh — Cross-agent coordination report (Lightweight Overseer)
# Usage: bash scripts/agent_status_report.sh [--webhook URL]
#
# Generates a status report covering:
# 1. Bead overview
# 2. Agent handoff status
# 3. Stale beads
# 4. Orphaned handoffs
# 5. Recent activity
# 6. Recently closed beads

set -euo pipefail

HANDOFFS_DIR="handoffs"
ACTIVITY_LOG="$HANDOFFS_DIR/activity.jsonl"
WEBHOOK_URL=""
AGENTS="architect camera enemy-behavior environment player sound-design systems ui-ux vfx"

# Parse args
while [[ $# -gt 0 ]]; do
    case "$1" in
        --webhook) WEBHOOK_URL="$2"; shift 2 ;;
        *) shift ;;
    esac
done

REPORT=""
append() { REPORT="$REPORT$1"$'\n'; }

append "============================================"
append "  Agent Status Report — $(date -Iseconds 2>/dev/null || date +%Y-%m-%dT%H:%M:%S)"
append "============================================"
append ""

# --- 1. Bead Overview ---
append "## 1. Bead Overview"
append ""
if command -v bd &>/dev/null; then
    BD_STATS=$(bd stats 2>/dev/null || echo "(bd stats not available)")
    append "$BD_STATS"
else
    append "(bd not available — install beads CLI)"
fi
append ""

# --- 2. Agent Handoff Status ---
append "## 2. Agent Handoff Status"
append ""
append "  Agent              | Status       | Last Session            | Bead"
append "  -------------------|--------------|-------------------------|----------"

for agent in $AGENTS; do
    HANDOFF="$HANDOFFS_DIR/$agent.json"
    if [ -f "$HANDOFF" ]; then
        PARSED=$(node -e "
            const fs = require('fs');
            try {
                const d = JSON.parse(fs.readFileSync('$HANDOFF', 'utf8'));
                const ts = d.timestamp || 'unknown';
                const st = d.status || 'unknown';
                const bd = d.bead_id || '-';
                // Calculate age
                const now = Date.now();
                const then = new Date(ts).getTime();
                const hours = Math.round((now - then) / 3600000);
                const age = hours < 24 ? hours + 'h ago' : Math.round(hours/24) + 'd ago';
                console.log(st + '|' + ts.substring(0,19) + ' (' + age + ')|' + bd);
            } catch(e) {
                console.log('parse_error|?|?');
            }
        " 2>/dev/null || echo "error|?|?")

        IFS='|' read -r STATUS TIMESTAMP BEAD <<< "$PARSED"
        printf -v LINE "  %-19s| %-13s| %-24s| %s" "$agent" "$STATUS" "$TIMESTAMP" "$BEAD"
        append "$LINE"
    else
        printf -v LINE "  %-19s| %-13s| %-24s| %s" "$agent" "no handoff" "-" "-"
        append "$LINE"
    fi
done
append ""

# --- 3. Stale Beads ---
append "## 3. Stale Beads (open > 3 days)"
append ""
if command -v bd &>/dev/null; then
    STALE=$(bd stale --days 3 2>/dev/null || bd list --status=in_progress 2>/dev/null || echo "(could not check stale beads)")
    if [ -z "$STALE" ]; then
        append "  None"
    else
        append "$STALE"
    fi
else
    append "  (bd not available)"
fi
append ""

# --- 4. Orphaned Handoffs ---
append "## 4. Orphaned Handoffs"
append "  (Handoff says in_progress but bead is closed)"
append ""
ORPHAN_COUNT=0
if command -v bd &>/dev/null; then
    for agent in $AGENTS; do
        HANDOFF="$HANDOFFS_DIR/$agent.json"
        [ -f "$HANDOFF" ] || continue

        RESULT=$(node -e "
            const fs = require('fs');
            try {
                const d = JSON.parse(fs.readFileSync('$HANDOFF', 'utf8'));
                if (d.status === 'in_progress' && d.bead_id) {
                    console.log(d.bead_id);
                }
            } catch(e) {}
        " 2>/dev/null || true)

        if [ -n "$RESULT" ]; then
            # Check if bead is actually closed
            BEAD_STATUS=$(bd show "$RESULT" --json 2>/dev/null | node -e "
                let data = '';
                process.stdin.on('data', c => data += c);
                process.stdin.on('end', () => {
                    try { process.stdout.write(JSON.parse(data).status || ''); }
                    catch(e) { process.stdout.write(''); }
                });
            " 2>/dev/null || echo "")

            if [ "$BEAD_STATUS" = "closed" ]; then
                append "  ORPHAN: $agent — handoff says in_progress for $RESULT but bead is closed"
                ORPHAN_COUNT=$((ORPHAN_COUNT + 1))
            fi
        fi
    done
fi
if [ "$ORPHAN_COUNT" -eq 0 ]; then
    append "  None"
fi
append ""

# --- 5. Recent Activity (last 24h) ---
append "## 5. Recent Activity (last 24h)"
append ""
if [ -f "$ACTIVITY_LOG" ]; then
    # Get timestamp from 24h ago
    CUTOFF=$(node -e "console.log(new Date(Date.now() - 86400000).toISOString())" 2>/dev/null || echo "")
    if [ -n "$CUTOFF" ]; then
        RECENT=$(node -e "
            const fs = require('fs');
            const cutoff = '$CUTOFF';
            try {
                const lines = fs.readFileSync('$ACTIVITY_LOG', 'utf8').trim().split('\n');
                const recent = lines.filter(l => {
                    const ts = l.split('|')[0];
                    return ts >= cutoff;
                });
                if (recent.length === 0) console.log('  No activity in last 24h');
                else recent.forEach(l => console.log('  ' + l));
            } catch(e) {
                console.log('  (error reading activity log)');
            }
        " 2>/dev/null || echo "  (error parsing activity log)")
        append "$RECENT"
    else
        append "  (could not determine cutoff time)"
    fi
else
    append "  No activity log yet ($ACTIVITY_LOG)"
fi
append ""

# --- 6. Recently Closed ---
append "## 6. Recently Closed (last 7 days)"
append ""
if command -v bd &>/dev/null; then
    SEVEN_DAYS_AGO=$(node -e "console.log(new Date(Date.now() - 7*86400000).toISOString().split('T')[0])" 2>/dev/null || echo "")
    if [ -n "$SEVEN_DAYS_AGO" ]; then
        CLOSED=$(bd list --all --status closed --closed-after "$SEVEN_DAYS_AGO" 2>/dev/null || bd list --status=closed 2>/dev/null || echo "(could not list closed beads)")
        if [ -z "$CLOSED" ]; then
            append "  None"
        else
            append "$CLOSED"
        fi
    else
        append "  (could not determine date range)"
    fi
else
    append "  (bd not available)"
fi
append ""

# --- Output ---
append "============================================"
append "  End of Report"
append "============================================"

echo "$REPORT"

# Optional webhook delivery
if [ -n "$WEBHOOK_URL" ]; then
    if command -v curl &>/dev/null; then
        PAYLOAD=$(node -e "
            const report = process.argv[1];
            console.log(JSON.stringify({ content: report.substring(0, 2000) }));
        " "$REPORT" 2>/dev/null || echo '{"content":"Report generation failed"}')
        curl -s -X POST -H "Content-Type: application/json" -d "$PAYLOAD" "$WEBHOOK_URL" >/dev/null 2>&1 || true
        echo "(Report sent to webhook)"
    else
        echo "(curl not available — could not send to webhook)"
    fi
fi
