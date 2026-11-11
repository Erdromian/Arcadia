# Puts the pinned upstream ClojureCLR runtime into Infrastructure/, replacing the 2019 1.10 binaries.
# Versions are pinned in Runtime.csproj; packages.lock.json pins each package's content hash, and
# restore runs in locked mode, so a changed or tampered package fails the build. See docs/plans (U5).
# Pass -UpdateLock after changing a version in Runtime.csproj.
param([switch]$UpdateLock)
$ErrorActionPreference = "Stop"
$repo = Split-Path (Split-Path $PSScriptRoot)
$out = Join-Path $repo ".build/runtime"
$infra = Join-Path $repo "Infrastructure"
[string[]]$lockArgs = if ($UpdateLock) { "--force-evaluate" } else { "--locked-mode" }

dotnet restore (Join-Path $PSScriptRoot "Runtime.csproj") @lockArgs
if ($LASTEXITCODE -ne 0) { throw "restore failed" }
dotnet build (Join-Path $PSScriptRoot "Runtime.csproj") -c Release --no-restore -o $out | Out-Null
if ($LASTEXITCODE -ne 0) { throw "build failed" }

# What Arcadia ships. Unity 6's Mono already provides System.Memory, System.Buffers,
# System.Numerics.Vectors and System.Threading.Tasks.Extensions, so those are left out.
# System.Runtime.CompilerServices.Unsafe and Microsoft.Bcl.AsyncInterfaces are shipped: Unity 6000.3
# doesn't offer them to plugins, and without them it refuses System.Text.Json and so Clojure.dll.
$ship = @(
    "Clojure.dll", "Clojure.Source.dll",
    "Microsoft.Dynamic.dll", "Microsoft.Scripting.dll", "Microsoft.Scripting.Metadata.dll",
    "clojure.spec.alpha.dll", "clojure.core.specs.alpha.dll", "clojure.tools.reader.dll",
    "Microsoft.Extensions.DependencyModel.dll", "System.Text.Json.dll",
    "System.Text.Encodings.Web.dll", "System.IO.Pipelines.dll",
    "System.Runtime.CompilerServices.Unsafe.dll", "Microsoft.Bcl.AsyncInterfaces.dll"
)
# The 1.10-era binaries this replaces: per-namespace AOT core libraries and test-only libraries
# compiled against the old, unsigned Clojure.dll (they can't bind to the strong-named 1.12 one).
# Microsoft.Scripting.AspNet.dll is a leftover piece of the old DLR 1.1 that nothing uses.
$old = @(Get-ChildItem $infra -Filter "clojure.*.dll") + @(Get-ChildItem $infra -Filter "Microsoft.Scripting.AspNet.dll")
$old | Where-Object { $ship -notcontains $_.Name } | ForEach-Object {
    Remove-Item $_.FullName
    if (Test-Path "$($_.FullName).meta") { Remove-Item "$($_.FullName).meta" }
    Write-Host "removed $($_.Name)"
}
foreach ($f in $ship) {
    Copy-Item (Join-Path $out $f) (Join-Path $infra $f) -Force
    Write-Host "copied  $f $((Get-Item (Join-Path $out $f)).VersionInfo.FileVersion)"
}
