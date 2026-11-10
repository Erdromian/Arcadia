# Builds the net48 host and runs Source/clojure/test_clojure against the bundled
# Infrastructure/Clojure.dll, one namespace at a time, without Unity. See docs/plans (U3).
# Pass namespace names to run a subset. Results: .build/clr-tests.tsv
param([string[]]$Namespaces = @(), [string]$Results)
$ErrorActionPreference = "Stop"
$repo = Split-Path (Split-Path $PSScriptRoot)
if (-not $Results) { $Results = Join-Path $repo ".build/clr-tests.tsv" }
dotnet build (Join-Path $PSScriptRoot "ClrTestHost.csproj") -c Release -o (Join-Path $repo ".build/clr-tests") | Out-Null
if ($LASTEXITCODE -ne 0) { throw "build failed" }
& (Join-Path $repo ".build/clr-tests/ClrTestHost.exe") (Join-Path $repo "Source") $Results @Namespaces
$code = $LASTEXITCODE
Import-Csv $Results -Delimiter "`t" | Format-Table namespace, status, test, pass, fail, error, note -AutoSize
exit $code
