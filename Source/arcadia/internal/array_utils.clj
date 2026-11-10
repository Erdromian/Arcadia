(ns arcadia.internal.array-utils)

;; TODO: should be possible for this
;; macro to type-hint returned form

;; Array/CreateInstance lengths are passed as (int ...): with a long, ClojureCLR
;; 1.12 picks the CreateInstance(Type, params long[]) overload and throws
;; InvalidCastException (Int64 to Int64[]).
(defmacro lit-array [t & contents]
  (let [len (count contents)
        arsym (with-meta (gensym "array_")
                {:tag (symbol
                        (.FullName
                          (type (. Array (CreateInstance (resolve t) (int 0))))))})
        assgns (->> contents
                 (map-indexed
                   (fn [n content]
                     `(aset ~arsym ~n ~content))))]
    `(let [~arsym (. Array (CreateInstance ~t (int ~len)))]
       ~@assgns
       ~arsym)))
