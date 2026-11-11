# Array/CreateInstance with a long length picks the params long[] overload (1.12.0 and later)

**Title:** `(Array/CreateInstance Object 2)` throws InvalidCastException since 1.12.0

Calling `System.Array.CreateInstance` with a type and a long length now resolves to the `CreateInstance(Type, params long[])` overload, but the long is passed as is rather than wrapped in an array, so the call throws. On 1.11.0 the same call returns an `Object[]`.

Repro (ClojureCLR 1.12.6, .NET 10.0.10; same on .NET Framework 4.8 and on Mono):

```clojure
(Array/CreateInstance Object 2)
;; InvalidCastException: Unable to cast object of type 'System.Int64' to type 'System.Int64[]'.

(. Array (CreateInstance Object 2))        ; same exception
(let [n 2] (Array/CreateInstance Object n)) ; same exception

(class (Array/CreateInstance Object (int 2)))
;; => System.Object/1   works
```

By version: 1.11.0 returns `System.Object[]`; 1.12.0, 1.12.2, 1.12.6 and 1.13.0-alpha8 throw.

Expected: either pick `CreateInstance(Type, int)` with a conversion as 1.11 did, or, if the `params long[]` overload is chosen, pass the argument wrapped in a `long[]`. On Mono the message is the bare "Specified cast is not valid.", which makes this hard to track down.

Real-world impact: Arcadia (Clojure for Unity) builds literal arrays with `(. Array (CreateInstance t n))`, which broke every function built on that macro. Passing `(int n)` works around it.
