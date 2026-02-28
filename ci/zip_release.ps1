$source = "Builds"
$dest = "ProjectNameHere-Release.zip"
$exclude = @("build.log", "ProjectNameHereUnity_BurstDebugInformation_DoNotShip")

$staging = Join-Path $env:TEMP "ProjectNameHere-Release"
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Path $staging | Out-Null

Get-ChildItem -Path $source | Where-Object { $exclude -notcontains $_.Name } | ForEach-Object {
    Copy-Item -Path $_.FullName -Destination $staging -Recurse
}

if (Test-Path $dest) { Remove-Item $dest -Force }

Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $dest -CompressionLevel Optimal

Remove-Item $staging -Recurse -Force

$size = [math]::Round((Get-Item $dest).Length / 1MB, 1)
Write-Host "Created $dest - $size MB"
