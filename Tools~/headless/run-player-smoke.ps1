# Builds a Windows Mono player from the harness project (Tests/Editor/HeadlessPlayerBuild.cs) and
# runs it headless; the player's smoke hook logs one ARCADIA-PLAYER-SMOKE line and quits.
# Exit codes: 0 player ran the hook, 1 build failed, 2 build setup failed, 5 player didn't log the
# marker, 6 player hung (killed).
param(
    [string]$Project = (Join-Path (Split-Path (Split-Path (Split-Path $PSScriptRoot))) "Arcadia-harness"),
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe",
    [string]$Out,
    [int]$BuildTimeoutSeconds = 1800,
    [int]$PlayerTimeoutSeconds = 120
)
$repo = Split-Path (Split-Path $PSScriptRoot)
$name = Split-Path $Project -Leaf
if (-not $Out) { $Out = Join-Path $repo ".build/player-$name" }
$results = Join-Path $repo ".build/player-build-$name.txt"
$editorLog = Join-Path $repo ".build/player-build-$name-editor.log"
$playerLog = Join-Path $repo ".build/player-$name.log"
if (Test-Path $Out) { Remove-Item -Recurse -Force $Out }
$p = Start-Process -FilePath $Unity -PassThru -NoNewWindow -ArgumentList @(
    "-batchmode", "-nographics", "-projectPath", "`"$Project`"",
    "-executeMethod", "Arcadia.Headless.HeadlessPlayerBuild.Run",
    "-arcadiaBuildOut", "`"$Out`"", "-arcadiaResults", "`"$results`"", "-logFile", "`"$editorLog`"")
$null = $p.Handle  # keeps ExitCode readable without -Wait
if (-not $p.WaitForExit($BuildTimeoutSeconds * 1000)) {
    Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
    Write-Host "build hung; editor log $editorLog"; exit 1
}
if (Test-Path $results) { Get-Content $results }
if ($p.ExitCode -ne 0) { Write-Host "build exit $($p.ExitCode); editor log $editorLog"; exit $p.ExitCode }

$exe = Join-Path $Out "ArcadiaSmoke.exe"
$pl = Start-Process -FilePath $exe -PassThru -ArgumentList @("-batchmode", "-nographics", "-logFile", "`"$playerLog`"")
$null = $pl.Handle
if (-not $pl.WaitForExit($PlayerTimeoutSeconds * 1000)) {
    Stop-Process -Id $pl.Id -Force -ErrorAction SilentlyContinue
    Write-Host "player hung and was killed; player log $playerLog"; exit 6
}
$line = Select-String -Path $playerLog -Pattern "ARCADIA-PLAYER-SMOKE" | Select-Object -First 1
if ($line) { Write-Host "PLAYER OK: $($line.Line)"; exit 0 }
Select-String -Path $playerLog -Pattern "Exception|Error" | Select-Object -First 15 | ForEach-Object { $_.Line }
Write-Host "player exit $($pl.ExitCode), no marker; player log $playerLog"; exit 5
