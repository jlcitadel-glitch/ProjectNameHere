# agent-runner.ps1 — Auto-polling agent runner for Claude Code agents
# Usage: powershell -ExecutionPolicy Bypass -File scripts\agent-runner.ps1 -AgentName <name>
#
# Polls bd ready for labeled work, launches claude -p when work appears.
# Control: Create .claude/agents/<name>/PAUSE to pause, STOP to exit.

param(
    [Parameter(Mandatory=$true)]
    [string]$AgentName,

    [int]$PollInterval = 30,
    [int]$SessionCooldown = 10,
    [int]$MaxRetries = 3
)

# --- Setup ---

$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$AgentDir = Join-Path $ProjectRoot ".claude\agents\$AgentName"
$LogFile = Join-Path $AgentDir "runner.log"
$PauseFile = Join-Path $AgentDir "PAUSE"
$StopFile = Join-Path $AgentDir "STOP"

# Ensure Dolt is on PATH
$DoltPath = "C:\Program Files\Dolt\bin"
if ($env:PATH -notlike "*$DoltPath*") {
    $env:PATH = "$DoltPath;$env:PATH"
}

# --- Logging ---

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-ddTHH:mm:ss"
    $line = "[$timestamp] [$Level] $Message"
    Write-Host $line
    Add-Content -Path $LogFile -Value $line -ErrorAction SilentlyContinue
}

# --- Banner ---

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Agent Runner: $AgentName" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Poll interval   : ${PollInterval}s"
Write-Host "  Session cooldown : ${SessionCooldown}s"
Write-Host "  Agent dir        : $AgentDir"
Write-Host "  Pause            : Create $PauseFile"
Write-Host "  Stop             : Create $StopFile"
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Validate agent directory
if (-not (Test-Path $AgentDir)) {
    Write-Log "Agent directory not found: $AgentDir" "ERROR"
    exit 1
}

Write-Log "Runner started for agent: $AgentName"

# Clean stale STOP file from previous session
if (Test-Path $StopFile) {
    Remove-Item $StopFile -Force
    Write-Log "Cleaned up stale STOP file"
}

$consecutiveFailures = 0

# --- Main Loop ---

while ($true) {
    # Check STOP
    if (Test-Path $StopFile) {
        Write-Log "STOP file detected. Exiting runner."
        Write-Host "`nRunner stopped." -ForegroundColor Yellow
        break
    }

    # Check PAUSE
    if (Test-Path $PauseFile) {
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] PAUSED - Remove $PauseFile to resume..." -ForegroundColor Yellow
        Start-Sleep -Seconds $PollInterval
        continue
    }

    # Poll for work
    try {
        $bdOutput = & bd ready --label "agent:$AgentName" --json --limit 1 2>&1 | Out-String

        # Handle empty output or non-JSON
        if ([string]::IsNullOrWhiteSpace($bdOutput) -or $bdOutput.Trim() -eq "[]" -or $bdOutput.Trim() -notmatch '^\[') {
            Write-Host "[$(Get-Date -Format 'HH:mm:ss')] No work for $AgentName. Sleeping ${PollInterval}s..." -ForegroundColor DarkGray
            Start-Sleep -Seconds $PollInterval
            $consecutiveFailures = 0
            continue
        }

        $issues = $bdOutput | ConvertFrom-Json -ErrorAction Stop

        if ($null -eq $issues -or $issues.Count -eq 0) {
            Write-Host "[$(Get-Date -Format 'HH:mm:ss')] No work for $AgentName. Sleeping ${PollInterval}s..." -ForegroundColor DarkGray
            Start-Sleep -Seconds $PollInterval
            $consecutiveFailures = 0
            continue
        }

        # Work found
        $issue = $issues[0]
        $issueId = $issue.id
        $issueTitle = $issue.title
        $issuePriority = $issue.priority

        Write-Log "Work found: $issueId - $issueTitle (P$issuePriority)"
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "  WORK FOUND: $issueId" -ForegroundColor Green
        Write-Host "  $issueTitle (P$issuePriority)" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""

    } catch {
        Write-Log "bd ready failed: $_" "WARN"
        $consecutiveFailures++
        $backoff = [Math]::Min($PollInterval * $consecutiveFailures, 300)
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] bd ready error. Retrying in ${backoff}s..." -ForegroundColor Red
        Start-Sleep -Seconds $backoff
        continue
    }

    # Build prompt
    $prompt = @"
You are the $AgentName agent, auto-dispatched by the agent runner.

## Your Task
- **ID**: $issueId
- **Title**: $issueTitle
- **Priority**: P$issuePriority

## Session Protocol
1. Read STANDARDS.md for project invariants
2. Check handoffs/$AgentName.json for prior session context (if it exists)
3. Claim the task: ``bd update $issueId --claim``
4. Read task details: ``bd show $issueId``
5. Follow the RPI pattern: Research -> Plan -> Implement
6. Complete the task

## Landing Protocol (mandatory before exiting)
1. File beads for any remaining or discovered work
2. Close the task: ``bd close $issueId --reason "summary of work done"``
3. Write handoffs/$AgentName.json per handoffs/SCHEMA.md
4. Append to handoffs/activity.jsonl
5. Run: ``git add <changed files> && git commit -m "description" && git pull --rebase && git push``
6. Run: ``bd sync``

## Rules
- If the task requires user approval (Plan step), write your plan to the handoff, note it needs review, and exit
- If blocked or needing input, update the bead, write handoff, and exit
- If you discover out-of-scope work, file a new bead (bd create) — do not context-switch
- Always push before exiting
"@

    # Launch Claude
    Write-Log "Launching claude session for $issueId"

    try {
        Push-Location $AgentDir

        & claude -p $prompt

        $exitCode = $LASTEXITCODE
        Pop-Location

        Write-Log "Claude session completed (exit=$exitCode) for $issueId"
        Write-Host ""
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Session complete for $issueId." -ForegroundColor DarkGray
        $consecutiveFailures = 0

    } catch {
        Pop-Location
        Write-Log "Claude session crashed: $_" "ERROR"
        $consecutiveFailures++

        if ($consecutiveFailures -ge $MaxRetries) {
            $backoff = 120
            Write-Log "Max retries ($MaxRetries) reached. Backing off ${backoff}s" "WARN"
            Write-Host "[ERROR] Repeated failures. Backing off ${backoff}s..." -ForegroundColor Red
            Start-Sleep -Seconds $backoff
        }
    }

    # Cooldown
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Cooling down ${SessionCooldown}s..." -ForegroundColor DarkGray
    Start-Sleep -Seconds $SessionCooldown
}

Write-Log "Runner exiting for agent: $AgentName"
