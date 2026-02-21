# pause-all.ps1 — Pause all agent runners
$agents = @("architect","camera","enemy-behavior","environment","player","sound-design","systems","ui-ux","vfx")
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
foreach ($a in $agents) {
    $path = Join-Path $root ".claude\agents\$a\PAUSE"
    New-Item $path -ItemType File -Force | Out-Null
    Write-Host "Paused: $a" -ForegroundColor Yellow
}
Write-Host "`nAll agents paused. Run resume-all.ps1 to resume." -ForegroundColor Cyan
