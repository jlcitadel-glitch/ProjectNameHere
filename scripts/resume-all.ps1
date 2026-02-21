# resume-all.ps1 — Resume all paused agent runners
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Get-ChildItem (Join-Path $root ".claude\agents\*\PAUSE") -ErrorAction SilentlyContinue | ForEach-Object {
    $agent = $_.Directory.Name
    Remove-Item $_.FullName -Force
    Write-Host "Resumed: $agent" -ForegroundColor Green
}
Write-Host "`nAll agents resumed." -ForegroundColor Cyan
