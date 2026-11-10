# Nested Class/method calls compile in exponential time (1.12.2 and later)

**Title:** Nested `Class/method` calls compile in time exponential in nesting depth since 1.12.2

Compiling nested static method calls written as `Class/method` takes time that doubles with each level of nesting. The equivalent `(. Class method args)` form, and the same code on 1.12.1-alpha1 and earlier, compile in linear time.

Repro (ClojureCLR 1.12.6, .NET 10.0.10; same shape on .NET Framework 4.8 and on Mono):

```clojure
(defn nest [mk n]
  (let [args (vec (for [i (range n)] (symbol (str "a" i))))]
    `(fn [~@args] ~(reduce mk args))))

(doseq [[label mk] [["Math/Max    " (fn [a b] (list 'Math/Max a b))]
                    ["(. Math Max)" (fn [a b] (list '. 'Math 'Max a b))]]
        depth [8 10 12 14]]
  (let [form (nest mk (inc depth))
        sw (System.Diagnostics.Stopwatch/StartNew)]
    (eval form)
    (println label "depth" depth (.ElapsedMilliseconds sw) "ms")))
```

Output on 1.12.6:

```
Math/Max     depth 8 68 ms
Math/Max     depth 10 168 ms
Math/Max     depth 12 656 ms
Math/Max     depth 14 2280 ms
(. Math Max) depth 8 23 ms
(. Math Max) depth 10 28 ms
(. Math Max) depth 12 32 ms
(. Math Max) depth 14 38 ms
```

`Math/Max` at depth 14 by version: 1.11.0 23 ms, 1.12.0 24 ms, 1.12.1-alpha1 43 ms, 1.12.2 1876 ms, 1.12.6 2280 ms, 1.13.0-alpha8 1948 ms.

Likely cause: commit 0dbb29bf ("Update to Java commit 9b0b383: CLJ-2899 ...") moved the loop that analyzes the arguments in `InvokeExpr.Parse` above the `QualifiedMethodExpr` branch, so the static-field case can report the argument count. The `QualifiedMethodExpr` branch still passes the raw argument forms (`form.next()`) to `ToHostExpr`, which analyzes them again via `HostExpr.ParseArgs`. Every argument of a `Class/method` call is analyzed twice, so a nest of depth n is analyzed about 2^n times. Counting the arguments without analyzing them for the static-field check (`RT.count(form.next())`), or analyzing them after the `QualifiedMethodExpr` branch as before, should restore linear time.

Real-world impact: Arcadia (Clojure for Unity) generates vector-math functions with arities nesting up to 20 calls such as `UnityEngine.Vector2/op_Addition`; loading that namespace went from about 3 s to over an hour. Rewriting the generated calls to the `(. Class method ...)` form works around it.
