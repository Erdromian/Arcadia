# Creates a throwaway Unity project next to this repo (default ../Arcadia-harness) and links this fork in as
# Assets/Arcadia (directory junction). See docs/plans (U2, KTD1).
param(
    [string]$Project = (Join-Path (Split-Path (Split-Path (Split-Path $PSScriptRoot))) "Arcadia-harness"),
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe",
    [ValidateSet("NetFramework", "NetStandard")] [string]$ApiLevel = "NetFramework"
)
$ErrorActionPreference = "Stop"
$repo = Split-Path (Split-Path $PSScriptRoot)
$Project = [IO.Path]::GetFullPath($Project)
if (-not (Test-Path (Join-Path $Project "ProjectSettings"))) {
    # Create the empty project first, without Arcadia, so the first import is fast.
    & $Unity -batchmode -nographics -createProject $Project -logFile (Join-Path $repo ".build/harness-create.log") -quit | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Unity -createProject failed ($LASTEXITCODE); see .build/harness-create.log" }
}
# Arcadia's hook components use UnityEngine.EventSystems, which lives in the uGUI
# package. Unity 6 templates usually include it; a bare -createProject doesn't.
$manifest = Join-Path $Project "Packages/manifest.json"
$m = Get-Content $manifest -Raw | ConvertFrom-Json
if (-not $m.dependencies."com.unity.ugui") {
    $m.dependencies | Add-Member -NotePropertyName "com.unity.ugui" -NotePropertyValue "2.6.0"
    # No BOM: the package manager rejects a manifest that starts with one.
    [IO.File]::WriteAllText($manifest, ($m | ConvertTo-Json -Depth 10), (New-Object Text.UTF8Encoding $false))
}
$link = Join-Path $Project "Assets/Arcadia"
if (-not (Test-Path $link)) { New-Item -ItemType Junction -Path $link -Target $repo | Out-Null }
if ($ApiLevel -eq "NetFramework") {
    & $Unity -batchmode -nographics -projectPath $Project -executeMethod Arcadia.Headless.HeadlessTestRunner.UseNetFramework `
        -logFile (Join-Path $repo ".build/harness-setup.log") -quit | Out-Null
    Write-Host "setup exit $LASTEXITCODE (see .build/harness-setup.log)"
}
Write-Host "Harness project: $Project"
