---
title: Arcadia on Unity 6 (Mono) - Plan
type: chore
date: 2026-11-10
artifact_contract: ce-unified-plan/v1
product_contract_source: ce-plan-bootstrap
execution: code
---

# Arcadia on Unity 6 (Mono) - Plan

## Goal Capsule

- **Objective:** Someone with Unity 6000.x can drop this fork into a project on the Mono backend, get a working Clojure REPL in the editor, and see Arcadia's own tests pass from the command line, without hunting for prebuilt binaries of unknown origin.
- **Means:** first make the existing tests runnable headless and record a baseline (KTD1, KTD2); then fix loading on Unity 6 in small commits; then replace the committed 2019 compiler binaries with a reproducible build of upstream ClojureCLR's `net462`/`net481` target (KTD3).
- **Authority:** Austin Finlinson owns the fork (Erdromian/Arcadia). This plan is subordinate to his instructions. His global rule applies: failing tests are fixed in code, never weakened, skipped or deleted without asking him.
- **Stop conditions:** stop and ask Austin before deleting branches, opening PRs or issues upstream (arcadia-unity), changing README claims or cutting releases, or publishing anywhere outside the fork.
- **Execution profile:** characterization first. Record what passes and fails before changing behaviour; every later unit re-runs the same headless commands and compares.
- **Who finishes:** agent sessions in this fork, on branches named `unity6/<topic>`, merged into `master` of the fork with Austin's OK.

---

## Product Contract

### Summary

Arcadia (Clojure for Unity, Apache-2.0) has had no real commit since 2020-04-13. It targets Unity 2018/2019 on Mono and embeds a patched ClojureCLR 1.10 as prebuilt `.NET Framework 4.0` DLLs. This fork revives it for Unity 6000.x on Mono only.

### Problem Frame

Arcadia's value is the live REPL into a running Unity editor. Today nobody can tell whether it still loads on a current Unity, its tests only run by hand inside the editor, and the compiler it ships cannot be rebuilt without archaeology: the DLLs were last replaced on 2019-01-15 from a commit on the `unity` branch of `arcadia-unity/clojure-clr`, built with `msbuild`/Mono and `ilrepack` on a 2019 toolchain. Without a repeatable test run and a repeatable build, every change is a guess.

### Requirements

**Tests and baseline**
- R1. Arcadia's own tests (`Tests/arcadia/arcadia_tests.clj`, five test groups on the harness in `Source/arcadia/internal/test.clj`) run headless from one command against a throwaway Unity 6000.x project, and print a pass/fail summary and exit code.
- R2. The bundled compiler's test suite (`Source/clojure/test_clojure/`) runs without Unity where it can, so compiler regressions are separable from Unity regressions.
- R3. A baseline of what passes and fails, before any code change, is committed in `docs/baseline/`.

**Loading on Unity 6**
- R4. Arcadia initializes on Unity 6000.x with API Compatibility Level ".NET Framework" on Mono: no compile errors, `Clojure.dll` loads, `arcadia.core` requires, and the socket REPL accepts a connection.
- R5. The API-compatibility check (upstream issue #391) recognizes Unity 6's levels instead of warning on everything that isn't `.NET 4.6`.

**Reproducible runtime**
- R6. The compiler and DLR in `Infrastructure/` are built by a script from pinned sources, not committed opaque binaries, if that build loads in Unity 6 Mono. If it doesn't, the existing binaries stay and their provenance is documented.

**Cleanup**
- R7. Dead branches and stale upstream issues are listed for Austin with a recommendation each; nothing is deleted without his OK.

### Scope Boundaries

- Out: IL2CPP. Clojure's `eval` and REPL generate code at runtime, which IL2CPP forbids. An ahead-of-time path for player builds is a later question.
- Out: Unity's CoreCLR migration, Godot, the MAGIC compiler.
- Out: upstream PRs/issues and any public release, unless Austin asks.
- Deferred: the open upstream PRs (#386 nREPL `:ns`, #387 TcpListener, #389 completions) are candidates to cherry-pick after R4 holds.

### Sources

- Upstream repo [arcadia-unity/Arcadia](https://github.com/arcadia-unity/Arcadia), checked 2026-11-10.
- Compiler fork [arcadia-unity/clojure-clr](https://github.com/arcadia-unity/clojure-clr), branch `unity`, checked 2026-11-10.
- Upstream [clojure/clojure-clr](https://github.com/clojure/clojure-clr), `Clojure/Clojure/Clojure.csproj` on `master`, checked 2026-11-10.

---

## Planning Contract

### Findings that shape the plan

**What's in the repo** (fork `master` = upstream `master`, 9fbc2fd, 2020-04-13)
- `Source/arcadia/` (9 namespaces) and `Source/arcadia/internal/` (62 files): the Clojure side.
- `Editor/` (initialization, REPL, nREPL, build pipeline, package restore through `Infrastructure/NuGet.exe`), `Helpers/`, `Components/` (one `MonoBehaviour` per Unity message, ~75 hook files): the C# side.
- `Source/clojure/`: the ClojureCLR 1.10 Clojure sources plus its own test suite `test_clojure/` (55 files) and runners (`run_test.clj`).
- `Infrastructure/`: `Clojure.dll` 1.10.0.0, `Microsoft.Dynamic/Scripting/Scripting.Metadata.dll` 1.1.0.21 (the DLR), 40+ AOT-compiled `clojure.*.clj.dll`, `BencodeNET.dll` 2.2.24, `Ionic.Zip.dll`, `NuGet.exe`. All target `.NETFramework,Version=v4.0` (BencodeNET v4.5).
- `Tests/arcadia/arcadia_tests.clj`: five async test groups (`scene-graph-system`, `cmpt-system`, `hook-state-system`, `core-namespace`, `linear-namespace`). They create GameObjects and wait on editor frames, play-mode frames or milliseconds, so they need a live Unity editor. There is no CI config.

**Where the compiler comes from.** Commit history of `Infrastructure/Clojure.dll` names its source each time. The current binaries (a1b47aa, 2019-01-15) come from `arcadia-unity/clojure-clr@9e5b7330`, which sits on that fork's `unity` branch, two commits behind its tip (`55fd3293`, 2019-01-24, "Build spec from submodules"). The build is `make` → `msbuild arcadia.proj` with `Runtime=Mono`, `PlatformSym=MONO`, `DirectLinking=true`, `Configuration="Release 4.0"`, and a `Dist` target that `ilrepack`s the per-namespace DLLs. spec came from `arcadia-unity/clr.spec.alpha@8a18a16`.

**Upstream ClojureCLR today.** Latest stable 1.12.6, alpha 1.13.0-alpha8 (NuGet, checked 2026-11-10). `Clojure.csproj` targets `net6.0;net8.0;net9.0;net10.0;net11.0;net462;net481`, with DLR from NuGet (`DynamicLanguageRuntime` 1.3.5) and spec as NuGet packages. No `netstandard` target, but **`net462`/`net481` still exist**, and Unity 6 Mono with the ".NET Framework" API level runs .NET Framework 4.8 class libraries. That is the most promising route to R6, and a much smaller gap than "modern .NET vs Mono" suggested. *Inference, untested: the `net462` build may still use APIs Unity's Mono lacks, such as `System.Reflection.Emit` save-to-disk paths used by AOT compile.*

**Branches** (56 on the fork). Only two carry unmerged work of substance:
- `ifrit` (+21, last 2022-11-09, Tims Gardner): "Setting up for 4.x games", a cross-version synchronized queue, a FileConsole, REPL startup hardening, better stack traces, and **removes `Infrastructure/*.dll` from the tree**. The maintainers' own last direction; worth reading before U5.
- `develop` (+3, 2021-03-02): fix for upstream #384 plus a config base-directory fix. Small, likely worth taking.
- About 40 branches have 0 commits ahead of `master` (fully merged): safe to delete once Austin agrees. About 12 have 1-9 unmerged commits from 2017-2019 experiments (`on-add-state-hook`, `exportable-nrepl`, `no-clojure`, `repl-proxy`, ...). A full list with ahead/behind counts goes in `docs/baseline/branches.md` (U1).

**Open upstream issues** (51) that matter for this goal: #391 (API compat check, R5), #367 (startup errors loading clj libs on newer Unity), #378 (`reify` blocks export/play on Windows), #332 (ArcadiaState vs the prefab editor), #321 / #294 / #264 / #163 / #114 (ClojureCLR codegen bugs that a newer compiler may fix), #303 / #214 (clojure.test / test framework, relevant to R1). The rest are feature requests or docs.

### Key Technical Decisions

- KTD1. **Test against a throwaway Unity project outside the repo, linked in.** A script creates a Unity 6000.6 project outside the clone and links the fork in as `Assets/Arcadia` (a directory junction), so the repo stays a plain Unity package folder and no `Library/` lands in it. Rationale: Arcadia expects to live under `Assets/` (`Editor/Initialization.cs` looks for exactly one folder holding `Clojure.dll`).
- KTD2. **Drive the async harness from a batchmode entry point.** A small editor-only C# class (in the fork, under `Tests/Editor/`) is called with `-executeMethod`. It waits for Arcadia to initialize, requires `arcadia.arcadia-tests`, calls `arcadia.internal.test/run-tests`, watches for completion over editor updates, writes the result data to a file and exits with a code. No `-quit`, so the editor keeps ticking for the async tests. Play-mode (`:frames`) waits need Play mode in batchmode; if that doesn't tick, record those tests as blocked, not failed.
- KTD3. **Rebuild from upstream ClojureCLR's `net462` target before patching the 2019 fork.** Upstream has moved 1.10 → 1.12 with many fixes; Arcadia's compiler patches (var caching, startup, `RT.FindFile`, metadata keys) are few and listed in the DLL commit messages. Try upstream `net462` first, port only the patches the tests prove necessary. Fallback: rebuild the `unity` branch at `9e5b7330` with modern MSBuild.
- KTD4. **Unity 6000.6.0f1 is the target editor** (installed on this machine; also VoxPListen's version). 6000.3.23f1 is the fallback.

### Assumptions

- Unity 6 still ships the ".NET Framework" API compatibility level and defines `NET_4_6` for it (Arcadia's `Editor/NRepl.cs` and `Editor/Initialization.cs` are wrapped in `#if NET_4_6`). Verified or refuted by U2.
- The harness Unity project needs no Unity licence beyond the one already active on this machine.
- Plain .NET Framework 4.8 on Windows can host the bundled `Clojure.dll` for R2 even though it was built with `PlatformSym=MONO`.

### Sequencing

U1 → U2 → U3 (baseline commit) → U4 and U5 can go in parallel → U6. U7 is independent and waits only on Austin.

---

## Implementation Units

### U1. Inventory notes

- **Goal:** commit the branch and issue survey so Austin can decide what to delete or take.
- **Requirements:** R7
- **Files:** `docs/baseline/branches.md`, `docs/baseline/issues.md`
- **Approach:** generate from `git for-each-ref` (date, ahead/behind `master`, last subject) and `gh issue list -R arcadia-unity/Arcadia`. Group branches into merged / unmerged-small / substantive.
- **Verification:** every remote branch and every open issue appears once.

### U2. Headless Unity harness

- **Goal:** one command runs Arcadia's tests in Unity 6 batchmode (R1).
- **Requirements:** R1, R4 (first signal)
- **Files:** `Tools~/headless/new-harness-project.ps1`, `Tools~/headless/run-arcadia-tests.ps1`, `Tests/Editor/HeadlessTestRunner.cs`, `Tests/Editor/Arcadia.HeadlessTests.asmdef` (only if an asmdef is needed to keep it editor-only)
- **Approach:** per KTD1/KTD2. Project lives in a temp or sibling folder, API level ".NET Framework", scripting backend Mono. The runner logs Arcadia initialization errors as their own failure class, so "doesn't load" and "test failed" are distinct.
- **Test scenarios:** harness on unchanged `master` code produces either a results file or a classified load failure; a deliberately failing assertion is reported as a failure with non-zero exit; a test that never completes hits a timeout and is reported, not hung.
- **Verification:** `run-arcadia-tests.ps1` exits within its timeout and leaves a results file.

### U3. Compiler suite without Unity, and the baseline

- **Goal:** run `Source/clojure/test_clojure` against the bundled `Clojure.dll` on .NET Framework 4.8 (R2) and commit the baseline (R3).
- **Requirements:** R2, R3
- **Files:** `Tools~/clr-tests/ClrTestHost.csproj`, `Tools~/clr-tests/Program.cs`, `Tools~/clr-tests/run.ps1`, `docs/baseline/2026-11-10-baseline.md`
- **Approach:** a tiny `net48` console that references `Infrastructure/*.dll`, puts `Source/` on the Clojure load path and runs each `clojure.test-clojure.*` namespace separately, so one crash doesn't hide the rest. Record per-namespace pass/fail/error counts.
- **Verification:** baseline file lists every test namespace and every Arcadia test group with a status. No test edited.

### U4. Load on Unity 6

- **Goal:** fix whatever U2 shows stopping initialization (R4), and update the API-compat check (R5).
- **Requirements:** R4, R5
- **Files:** `Editor/ArcadiaProjectInitialization.cs`, `Editor/Initialization.cs`, `Helpers/BasicPaths.cs`, others as U2 shows
- **Approach:** one commit per distinct breakage, each re-running U2. Consider taking `develop`'s two commits first.
- **Test scenarios:** initialization succeeds with ".NET Framework" level; with ".NET Standard 2.1" Arcadia logs a clear warning naming the setting; socket REPL accepts a `(+ 1 2)` eval.
- **Verification:** U2's harness reports Arcadia loaded and the test groups ran; compare against the baseline.

### U5. Reproducible compiler build

- **Goal:** build `Infrastructure/` compiler DLLs from pinned sources (R6).
- **Requirements:** R6
- **Files:** `Tools~/build-runtime/build.ps1`, `Tools~/build-runtime/README.md`, `Infrastructure/` (replaced binaries, or removed and gitignored), `.gitignore`
- **Approach:** per KTD3. Pin upstream ClojureCLR by tag; build `net462`; AOT-compile the `clojure.*` namespaces Arcadia needs. Read `ifrit` first for how the maintainers planned to drop committed DLLs.
- **Test scenarios:** the U3 compiler suite on the new build does no worse than the baseline; U2 Arcadia tests do no worse; the build is byte-for-byte unnecessary, but rerunning the script yields a loadable result.
- **Verification:** clean clone + `build.ps1` + U2 harness passes at least the baseline.

### U6. Report and next steps

- **Goal:** tell Austin what loads, what passes, the blockers and the next step; write durable findings to the research vault.
- **Requirements:** R3, R7
- **Files:** `docs/baseline/2026-11-10-baseline.md` (summary section)
- **Verification:** Austin has the summary and the branch list with recommendations.

### U7. Branch cleanup (only on Austin's OK)

- **Goal:** delete the fully merged branches Austin approves.
- **Requirements:** R7
- **Files:** none (remote refs only)
- **Verification:** remaining branches match his list.

---

## Verification Contract

| Check | Command | Applies to |
|---|---|---|
| Arcadia tests in Unity 6 batchmode | `powershell -File Tools~/headless/run-arcadia-tests.ps1` | U2, U4, U5 |
| Compiler suite without Unity | `powershell -File Tools~/clr-tests/run.ps1` | U3, U5 |
| Compare with baseline | diff of results against `docs/baseline/` | U4, U5 |

## Definition of Done

- Global: both commands run from a clean clone; no test weakened, skipped or deleted without Austin's OK; experimental code from abandoned attempts removed.
- First pass (this session): fork exists and is cloned, this plan is committed, U1-U3 done with a baseline, U6 report delivered.
