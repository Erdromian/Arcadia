# Runs Arcadia's tests headless in the harness project made by new-harness-project.ps1.
# Exit codes: 0 pass, 1 failures, 2 Arcadia did not load, 3 timeout, 5 passed with tests pending
# (see HeadlessTestRunner.cs), 4 Unity hung: the editor's main thread stopped ticking, so this
# script killed it.
param(
    [string]$Project = (Join-Path (Split-Path (Split-Path (Split-Path $PSScriptRoot))) "Arcadia-harness"),
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe",
    [string]$Results = (Join-Path (Split-Path (Split-Path $PSScriptRoot)) ".build/arcadia-test-results.txt"),
    [int]$TimeoutSeconds = 600,
    # Extra time for project import and script compilation before the runner's own timeout starts.
    [int]$StartupSeconds = 300,
    # Run the tests in Play mode (HeadlessTestRunner enters it first) instead of edit mode.
    [switch]$PlayMode
)
$repo = Split-Path (Split-Path $PSScriptRoot)
$log = Join-Path $repo ".build/arcadia-tests-editor.log"
$unityArgs = @(
    "-batchmode", "-nographics", "-projectPath", "`"$Project`"",
    "-executeMethod", "Arcadia.Headless.HeadlessTestRunner.Run",
    "-arcadiaResults", "`"$Results`"", "-arcadiaTimeout", $TimeoutSeconds,
    "-logFile", "`"$log`"")
if ($PlayMode) { $unityArgs += "-arcadiaPlayMode" }
$p = Start-Process -FilePath $Unity -PassThru -NoNewWindow -ArgumentList $unityArgs
$null = $p.Handle  # without -Wait, ExitCode is only kept if the handle was opened while the process ran
# The runner's timeout only fires on editor updates; if the main thread blocks, nothing would end
# the run. Kill the editor (and the import workers it started) once the hard limit passes.
if (-not $p.WaitForExit(($TimeoutSeconds + $StartupSeconds) * 1000)) {
    Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" |
        Where-Object { $_.ProcessId -eq $p.Id -or $_.ParentProcessId -eq $p.Id } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
    if (Test-Path $Results) { Add-Content $Results "HUNG: killed after $($TimeoutSeconds + $StartupSeconds) s; last step above`nexit: 4" }
    if (Test-Path $Results) { Get-Content $Results | Select-Object -First 40 }
    Write-Host "Unity hung and was killed; full results $Results; editor log $log"
    exit 4
}
$p.WaitForExit()
if (Test-Path $Results) { Get-Content $Results | Select-Object -First 40 }
Write-Host "Unity exit $($p.ExitCode); full results $Results; editor log $log"
exit $p.ExitCode
