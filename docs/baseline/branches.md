# Branches on the fork (2026-11-10)

Copied from upstream arcadia-unity/Arcadia. Ahead/behind are commits relative to `master` (9fbc2fd, 2020-04-13). Nothing has been deleted; deletion waits for Austin's OK.

## Substantive unmerged work

| Last commit | Branch | Ahead | Behind | Last subject |
|---|---|---|---|---|
| 2022-11-09 | `ifrit` | +21 | -0 | Setting up for 4.x games |
| 2021-03-02 | `develop` | +3 | -0 | Fixes #384 |

## Small unmerged experiments (2017-2019), recommend reviewing then deleting

| Last commit | Branch | Ahead | Behind | Last subject |
|---|---|---|---|---|
| 2019-09-09 | `exportable-nrepl` | +3 | -69 | implement OffMainThreadCompileEvalFn in NRepl |
| 2019-09-04 | `on-add-state-hook` | +7 | -68 | better docs for injections in configuration.edn |
| 2019-08-29 | `no-clojure` | +2 | -69 | add Clojure/Clojure.Source to load path |
| 2019-08-23 | `on-remove-state-hook` | +3 | -69 | adding :on-remove-state hook |
| 2019-08-23 | `special-hooks` | +2 | -69 | Merge branch 'master' into special-hooks |
| 2019-07-15 | `decoupled-callbacks` | +1 | -69 | attempting to decouple PlayerCallbacks.cs from config |
| 2019-03-12 | `issue-355` | +1 | -83 | WIP: calling Initialize across public ArcadiaState API for i |
| 2019-02-27 | `configured-exception-string` | +1 | -81 | Adding configured-exception-string to arcadia.internal.stack |
| 2019-02-26 | `config-repl-opts` | +1 | -82 | experimental support for print-level and print-length in con |
| 2019-02-19 | `around-serialization` | +2 | -82 | adding :around-serialize-data hook |
| 2017-10-30 | `repl-proxy` | +9 | -569 | Merge branch 'repl-proxy' of https://github.com/arcadia-unit |
| 2017-05-19 | `source2017-pass-k` | +4 | -719 | Merge branch 'pass-keys' into source2017-pass-k |
| 2017-05-18 | `source2017` | +2 | -724 | Merge remote-tracking branch 'origin/roles' into source2017 |
| 2017-05-18 | `disable-force-repl-update` | +1 | -734 | Disable force repl update logic for now |
| 2017-05-10 | `inert-inspector` | +1 | -729 | inert state inspector |

## Fully merged into master (0 ahead), recommend deleting

| Last commit | Branch | Behind | Last subject |
|---|---|---|---|
| 2020-02-26 | `issue-336` | -83 | Introducing workaround for #336 |
| 2019-09-19 | `fix-350` | -38 | Start work adding dynamic vars to nREPL |
| 2019-09-13 | `inspector-memory-leak-fix` | -59 | Cleanup inspector caches |
| 2019-09-13 | `socket-repl-reorganization` | -59 | Merge branch 'socket-repl-reorganization' of github.com:arca |
| 2019-09-09 | `exportable-config` | -68 | Access to merged configuraion in exported games |
| 2019-09-03 | `mono-2x-downgrade` | -60 | Warn when API compatibility is not .NET 4.6 |
| 2019-08-28 | `issue-371` | -68 | fixes #371 |
| 2019-05-03 | `issue-363` | -71 | Removing noisey logging |
| 2019-05-03 | `issue-362` | -74 | fixes #362. |
| 2019-05-01 | `remove-hardcoded-paths` | -76 | Remove hardcoded references to Arcadia folder |
| 2019-03-04 | `issue-351` | -84 | Changing logic of Arcadia.Utils/TrueNil. Fixes #351. |
| 2019-02-18 | `issue-346` | -85 | Updating CHANGELOG.md |
| 2019-02-15 | `issue-345` | -88 | arcadia.introspection flattens hierarchy. fixes #345 |
| 2018-12-18 | `beta` | -125 | formatting for docs on defmutable-once |
| 2018-12-11 | `fix-248` | -240 | Patch out AppDomain.BaseDirectory from RT.FindFile search pa |
| 2018-12-03 | `fix-315` | -324 | Change optimized initialization procedure |
| 2018-11-26 | `cache-vars` | -384 | Cache vars in function bodies |
| 2018-11-26 | `new-repl` | -376 | Capture *out* and *err* properly |
| 2018-11-21 | `hss-redux` | -363 | no-key `state` always returns nil if no state entries are at |
| 2018-11-14 | `arcadia-tests` | -385 | adding arcadia-tests.clj |
| 2018-11-14 | `hss-value-types` | -386 | Merge branch 'beta' into hss-value-types |
| 2018-11-13 | `nuget-packages` | -393 | Hide progress bar when done restoring |
| 2018-10-15 | `beta-startup-time` | -407 | Merge branch 'new-compiler-startup-time' into beta-startup-t |
| 2018-10-15 | `new-compiler-startup-time` | -473 | Bring in new compiler from arcadia-unity/clojure-clr@7817a68 |
| 2018-08-01 | `clojure-1.9-spec-update` | -478 | Merge branch 'ekhart-patch' into clojure-1.9-spec-update |
| 2018-04-13 | `debug-support` | -513 | avoiding breakpoint cycles |
| 2018-03-20 | `clojure-1.9` | -533 | Update to Clojure 1.9 |
| 2018-01-02 | `defmutable-extension` | -534 | adding retire* |
| 2017-09-26 | `pass-keys` | -577 | tweaking docs |
| 2017-09-14 | `build` | -686 | Delete export folders recursively |
| 2017-08-24 | `mozroots` | -698 | Correct mozroots path on Windows |
| 2017-06-04 | `socket-repl` | -724 | tweaking docs on socket-repl config |
| 2017-05-03 | `roles` | -724 | Merge branch 'develop' into roles |
| 2017-04-18 | `serialization` | -752 | tweaking better-merge |
| 2017-04-10 | `hook-help` | -764 | adding add-fns to hook-help |
| 2016-09-03 | `packages` | -953 | equipping packages with debugging stuff |
| 2015-06-24 | `unstable` | -1252 | Revert "Merge in new compiler to fix export", except those c |
| 2015-01-11 | `hydrate` | -1414 | adding checked-get |

Note: a branch literally named `origin` sits at the same commit as `master`; it is confusing next to the remote name and should go too.
