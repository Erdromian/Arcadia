# Runs Arcadia's tests headless in the harness project made by new-harness-project.ps1.
# Exit codes: 0 pass, 1 failures, 2 Arcadia did not load, 3 timeout (see HeadlessTestRunner.cs).
param(
    [string]$Project = (Join-Path (Split-Path (Split-Path (Split-Path $PSScriptRoot))) "Arcadia-harness"),
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe",
    [string]$Results = (Join-Path (Split-Path (Split-Path $PSScriptRoot)) ".build/arcadia-test-results.txt"),
    [int]$TimeoutSeconds = 600
)
$repo = Split-Path (Split-Path $PSScriptRoot)
$log = Join-Path $repo ".build/arcadia-tests-editor.log"
$p = Start-Process -FilePath $Unity -PassThru -Wait -NoNewWindow -ArgumentList @(
    "-batchmode", "-nographics", "-projectPath", "`"$Project`"",
    "-executeMethod", "Arcadia.Headless.HeadlessTestRunner.Run",
    "-arcadiaResults", "`"$Results`"", "-arcadiaTimeout", $TimeoutSeconds,
    "-logFile", "`"$log`"")
if (Test-Path $Results) { Get-Content $Results | Select-Object -First 40 }
Write-Host "Unity exit $($p.ExitCode); full results $Results; editor log $log"
exit $p.ExitCode
