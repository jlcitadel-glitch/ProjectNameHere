#!/usr/bin/env bash
# witness_check.sh — Post-closure validation for beads
# Usage: bash scripts/witness_check.sh <bead-id>
#
# Checks:
# 1. Matching handoff JSON exists
# 2. Bead status matches handoff status
# 3. Files in files_touched appear in recent git history
# 4. CI passes (optional, if python available)

set -euo pipefail

BEAD_ID="${1:-}"
HANDOFFS_DIR="handoffs"
ACTIVITY_LOG="$HANDOFFS_DIR/activity.jsonl"

# --- Helpers ---

log_error() { echo "FAIL: $1" >&2; }
log_ok()    { echo "  OK: $1"; }
log_info()  { echo "INFO: $1"; }

reopen_bead() {
    local id="$1"
    local reason="$2"
    log_error "$reason"
    if command -v bd &>/dev/null; then
        bd reopen "$id" -r "Witness check failed" 2>/dev/null || true
        bd comments add "$id" "Witness check failed: $reason" 2>/dev/null || true
    fi
}

# --- Validation ---

if [ -z "$BEAD_ID" ]; then
    echo "Usage: bash scripts/witness_check.sh <bead-id>"
    echo "  Validates that a closed bead has a matching handoff and consistent state."
    exit 1
fi

echo "=== Witness Check: $BEAD_ID ==="
echo ""

FAILURES=0

# 1. Find matching handoff JSON
HANDOFF_FILE=""
for f in "$HANDOFFS_DIR"/*.json; do
    [ -f "$f" ] || continue
    MATCH=$(node -e "
        const fs = require('fs');
        try {
            const data = JSON.parse(fs.readFileSync('$f', 'utf8'));
            if (data.bead_id === '$BEAD_ID') process.stdout.write('yes');
        } catch(e) {}
    " 2>/dev/null || true)
    if [ "$MATCH" = "yes" ]; then
        HANDOFF_FILE="$f"
        break
    fi
done

if [ -z "$HANDOFF_FILE" ]; then
    log_error "No handoff JSON found for $BEAD_ID in $HANDOFFS_DIR/"
    log_info "Agents should write handoffs/<agent>.json before closing beads."
    FAILURES=$((FAILURES + 1))
else
    log_ok "Found handoff: $HANDOFF_FILE"
fi

# 2. Check bead status matches handoff status
if [ -n "$HANDOFF_FILE" ] && command -v bd &>/dev/null; then
    BEAD_STATUS=$(bd show "$BEAD_ID" --json 2>/dev/null | node -e "
        let data = '';
        process.stdin.on('data', c => data += c);
        process.stdin.on('end', () => {
            try {
                const obj = JSON.parse(data);
                process.stdout.write(obj.status || 'unknown');
            } catch(e) {
                process.stdout.write('unknown');
            }
        });
    " 2>/dev/null || echo "unknown")

    HANDOFF_STATUS=$(node -e "
        const fs = require('fs');
        try {
            const data = JSON.parse(fs.readFileSync('$HANDOFF_FILE', 'utf8'));
            process.stdout.write(data.status || 'unknown');
        } catch(e) {
            process.stdout.write('unknown');
        }
    " 2>/dev/null || echo "unknown")

    if [ "$BEAD_STATUS" = "unknown" ]; then
        log_info "Could not retrieve bead status (bd show may not support --json)"
    elif [ "$BEAD_STATUS" = "$HANDOFF_STATUS" ] || [ "$BEAD_STATUS" = "closed" ]; then
        log_ok "Bead status consistent (bead=$BEAD_STATUS, handoff=$HANDOFF_STATUS)"
    else
        log_error "Status mismatch: bead=$BEAD_STATUS, handoff=$HANDOFF_STATUS"
        FAILURES=$((FAILURES + 1))
    fi
elif [ -z "$HANDOFF_FILE" ]; then
    log_info "Skipping status check (no handoff file)"
else
    log_info "Skipping status check (bd not available)"
fi

# 3. Check files_touched appear in recent git history
if [ -n "$HANDOFF_FILE" ]; then
    FILES_TOUCHED=$(node -e "
        const fs = require('fs');
        try {
            const data = JSON.parse(fs.readFileSync('$HANDOFF_FILE', 'utf8'));
            if (data.files_touched && data.files_touched.length > 0) {
                data.files_touched.forEach(f => console.log(f));
            }
        } catch(e) {}
    " 2>/dev/null || true)

    if [ -z "$FILES_TOUCHED" ]; then
        log_info "No files_touched in handoff (or empty array)"
    else
        # Get recent git changes (last 20 commits)
        RECENT_FILES=$(git diff --name-only HEAD~20 HEAD 2>/dev/null || git diff --name-only HEAD 2>/dev/null || echo "")
        STAGED_FILES=$(git diff --name-only --cached 2>/dev/null || echo "")
        ALL_CHANGED="$RECENT_FILES"$'\n'"$STAGED_FILES"

        MISSING_COUNT=0
        while IFS= read -r touched_file; do
            [ -z "$touched_file" ] && continue
            if echo "$ALL_CHANGED" | grep -qF "$touched_file"; then
                log_ok "File in git history: $touched_file"
            else
                log_error "File NOT in recent git history: $touched_file"
                MISSING_COUNT=$((MISSING_COUNT + 1))
            fi
        done <<< "$FILES_TOUCHED"

        if [ "$MISSING_COUNT" -gt 0 ]; then
            FAILURES=$((FAILURES + 1))
        fi
    fi
fi

# 4. CI check (optional)
if command -v python &>/dev/null && [ -f "ci/run_all.py" ]; then
    log_info "Running CI checks..."
    if python ci/run_all.py 2>&1; then
        log_ok "CI passed"
    else
        log_error "CI failed"
        FAILURES=$((FAILURES + 1))
    fi
else
    log_info "Skipping CI (python or ci/run_all.py not available)"
fi

# --- Result ---

echo ""
if [ "$FAILURES" -gt 0 ]; then
    echo "RESULT: $FAILURES check(s) failed for $BEAD_ID"
    reopen_bead "$BEAD_ID" "$FAILURES witness check(s) failed"
    exit 1
else
    echo "RESULT: All checks passed for $BEAD_ID"
    # Log success to activity log
    if [ -d "$HANDOFFS_DIR" ]; then
        echo "$(date -Iseconds 2>/dev/null || date +%Y-%m-%dT%H:%M:%S)|witness|check_passed|$BEAD_ID|closed|All witness checks passed" >> "$ACTIVITY_LOG"
    fi
    exit 0
fi
