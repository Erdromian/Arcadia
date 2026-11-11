# Builds the net48 host and runs a ClojureCLR test suite one namespace at a time, without Unity.
# See docs/plans (U3, U5). Pass namespace names to run a subset.
#   default:            bundled Infrastructure/Clojure.dll (1.10) on Source/clojure/test_clojure
#   -Runtime 1.12.6:    upstream ClojureCLR from NuGet on upstream's own test suite for that tag,
#                       cloned once into .build/clojure-clr-<version>
# Results: .build/clr-tests[-<runtime>].tsv
param([string[]]$Namespaces = @(), [string]$Runtime = "bundled", [string]$Results)
$ErrorActionPreference = "Stop"
$repo = Split-Path (Split-Path $PSScriptRoot)
$suffix = if ($Runtime -eq "bundled") { "" } else { "-$Runtime" }
if (-not $Results) { $Results = Join-Path $repo ".build/clr-tests$suffix.tsv" }
$out = Join-Path $repo ".build/clr-tests$suffix"
# The suite matches the runtime's version: the 1.10 tree Arcadia used to carry in Source/clojure,
# or upstream's suite at the clojure-<version> tag for anything newer.
$suiteVersion = $Runtime
if ($Runtime -eq "bundled") {
    $v = (Get-Item (Join-Path $repo "Infrastructure/Clojure.dll")).VersionInfo
    $suiteVersion = "$($v.FileMajorPart).$($v.FileMinorPart).$($v.FileBuildPart)"
}
if ($Runtime -eq "bundled" -and -not (Test-Path (Join-Path $repo "Source/clojure/test_clojure"))) {
    # Infrastructure/ holds a trimmed copy of a pinned NuGet release (Tools~/build-runtime), without
    # the System.* back-ports Unity supplies and without the test-only libraries. Plain .NET needs
    # both, so test the same pinned release from NuGet instead: the shared DLLs are identical files.
    Write-Host "Infrastructure holds ClojureCLR $suiteVersion; testing the NuGet build of $suiteVersion"
    $Runtime = $suiteVersion
    $suffix = "-$Runtime"
    $out = Join-Path $repo ".build/clr-tests$suffix"
    if ($Results -eq (Join-Path $repo ".build/clr-tests.tsv")) { $Results = Join-Path $repo ".build/clr-tests$suffix.tsv" }
}
if ($Runtime -eq "bundled") {
    $testRoot = Join-Path $repo "Source"
} else {
    $clone = Join-Path $repo ".build/clojure-clr-$suiteVersion"
    if (-not (Test-Path $clone)) {
        git clone -q --depth 1 --branch "clojure-$suiteVersion" https://github.com/clojure/clojure-clr.git $clone
        if ($LASTEXITCODE -ne 0) { throw "clone of clojure-$suiteVersion failed" }
    }
    $testRoot = Join-Path $clone "Clojure/Clojure.Tests"
}
Write-Host "runtime $Runtime, suite $suiteVersion at $testRoot"
# --no-incremental: swapped-in NuGet DLLs keep old timestamps, so MSBuild would skip the rebuild.
dotnet build (Join-Path $PSScriptRoot "ClrTestHost.csproj") -c Release --no-incremental -o $out "-p:ClojureRuntime=$Runtime" | Out-Null
if ($LASTEXITCODE -ne 0) { throw "build failed" }
# The host writes progress to stderr; under "Stop" PowerShell 5.1 would abort on it.
$ErrorActionPreference = "Continue"
& (Join-Path $out "ClrTestHost.exe") $testRoot $Results @Namespaces
$code = $LASTEXITCODE
Import-Csv $Results -Delimiter "`t" | Format-Table namespace, status, test, pass, fail, error, note -AutoSize
exit $code
