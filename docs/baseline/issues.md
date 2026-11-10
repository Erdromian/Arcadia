# Open upstream issues (arcadia-unity/Arcadia, 51, checked 2026-11-10)

## Relevant to running on Unity 6

- #391 2025-02-11 [] Update .NET API Compatibility Level check for modern Unity versions
- #367 2019-11-08 [] startup errors loading clj libs in latest Unity
- #378 2020-03-06 [] reify blocks export and play mode on windows
- #332 2019-01-09 [enhancement] ArcadiaState not compatible with new Prefab editor
- #303 2018-12-07 [] clojure.test should work smoothly
- #214 2018-12-04 [enhancement] Testing Framework
- #5 2018-12-04 [enhancement] REPL in exported App

## ClojureCLR compiler bugs a newer compiler may fix

- #364 2019-09-02 [bug,clojure-clr] Line numbers don't show up on exceptions, even in AOT compiled namespaces
- #344 2019-03-04 [bug,clojure-clr] .SetFloat using a def'd int leads to invalid IL
- #321 2018-11-29 [] reify occasionally emits invalid types
- #312 2018-10-11 [bug,clojure-clr] Non-empty C# Dictionaries don't print
- #305 2018-12-04 [bug,clojure-clr] edn serialization of arrays seems broken
- #294 2018-10-01 [bug,clojure-clr,notable-bug] invalid il on deftype mutation
- #264 2018-10-01 [bug,clojure-clr,notable-bug] deftype doesn't work with generic types
- #163 2018-12-04 [bug,clojure-clr,notable-bug] proxy problem
- #123 2016-11-01 [bug,enhancement,clojure-clr] Need support for multidimensional arrays
- #114 2018-10-01 [bug,clojure-clr,notable-bug,solved in magic] by-ref broken

## Open upstream PRs worth cherry-picking after Arcadia loads

- PR #389 (2023-02-15, benjamin-asdf) Enable completions, update eldoc
- PR #387 (2021-10-07, meta-meta) prevent multiple TcpListener instances
- PR #386 (2021-11-26, Folcon) nREPL :ns key
- PR #353 (2019) documentation fix

## Everything else (features, docs, editor tooling)

- #388 2023-09-10 [] Status of this project
- #384 2021-03-02 [] Internal indexing problem when adding and removing hooks during same Unity event
- #382 2020-05-28 [] It's not safe to Load InitType with type name only
- #381 2020-04-01 [] Removing a component is not finished after a call to cmpt-
- #377 2020-02-14 [bug] Bad error when calling `descendents` on destroyed GameObject
- #376 2020-01-30 [] "No reader function for tag object"
- #375 2019-11-12 [tooling] Socket repl timing issues?
- #373 2019-09-12 [enhancement] Use TryGetComponent?
- #372 2019-09-03 [] Investigate "double init" bug
- #370 2019-08-27 [documentation] update Mono Runtime Configuration documentation to reflect new UI in 2019.2
- #369 2019-09-02 [bug] problem with update-state
- #360 2019-04-08 [] Get absolute file paths when using meta
- #358 2019-09-02 [bug] `roles-` broken
- #357 2019-03-25 [enhancement] missing ergonomics to merge roles together
- #354 2019-03-12 [documentation] roles+ missing example in reference
- #352 2019-03-12 [documentation] defrole missing from usage documentation
- #350 2019-02-28 [] Can't set! *print-level* on nrepl
- #349 2019-02-27 [enhancement,API] expose internal stacktrace formatting to user
- #348 2019-02-26 [enhancement] Optionally limit inspector state print out
- #340 2019-01-14 [enhancement] Breakpoint support in nREPL
- #339 2019-01-14 [] :a in arcadia.debug's break results in clojure.lang.ArityException
- #337 2019-01-15 [enhancement] nREPL + break = Object reference not set to an instance of an object
- #320 2018-12-27 [bug] jagged printing in emacs
- #313 2018-10-17 [enhancement,clojure-clr] Further optimizations possible in ClojureCLR
- #310 2018-10-08 [] centralize and document error handling system
- #308 2018-10-08 [] Deprecate AssetPostprocessor 
- #300 2018-12-04 [bug] MissingMethodException Cannot find member Instantiate matching args
- #292 2019-01-13 [bug] Using inf-clojure-eval-buffer stucks the repl
- #288 2019-03-24 [bug] socket repl on emacs eats whitespace
- #271 2018-12-04 [] don't delete built DLLs after building
- #218 2018-12-04 [enhancement] Optional static vars in ArcadiaBehaviour
- #213 2018-12-04 [enhancement] reader conditional extension for editor/build targets
- #128 2020-01-08 [] Good inspectors for Arcadia components
- #98 2018-10-01 [notable-bug] [Using Unity Engine's WWW] How to wait for a download to finish in Arcadia? 
