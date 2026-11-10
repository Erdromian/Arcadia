;; Runs ClojureCLR test namespaces one at a time and writes a TSV row per
;; namespace, so one crash doesn't hide the rest. Started through clojure.main
;; (ClrTestHost.exe), which sets up the same bindings as ClojureCLR's own
;; run_test.clj. Args: <results.tsv> [namespace ...]
(require '[clojure.test :as test] '[clojure.string :as str])

(doseq [dll ["clojure.tools.namespace.dll" "clojure.data.generators.dll"
             "clojure.test.generative.dll" "clojure.test.check.dll"]]
  (try (assembly-load-from (System.IO.Path/Combine (.BaseDirectory System.AppDomain/CurrentDomain) dll))
       (catch Exception e (binding [*out* *err*] (println "warn:" (.Message e))))))

(defn- root-cause [^Exception e]
  (if-let [c (.InnerException e)] (recur c) e))

(defn- all-namespaces []
  (->> (System.IO.Directory/GetFiles "clojure/test_clojure")
       ;; only sources: Unity drops .clj.meta files beside them
       (filter #(re-find #"\.cljc?$" %))
       (map #(-> (System.IO.Path/GetFileNameWithoutExtension %) (str/replace "_" "-")))
       (map #(symbol (str "clojure.test-clojure." %)))
       sort))

(let [[results & names] *command-line-args*
      list-only? (= names ["--list"])
      namespaces (if (and (seq names) (not list-only?)) (map symbol names) (all-namespaces))
      _ (when list-only? (run! println namespaces) (Environment/Exit 0))
      rows (doall
             (for [ns-sym namespaces]
               (let [row (try
                           (require ns-sym)
                           (let [{:keys [test pass fail error]} (test/run-tests ns-sym)]
                             [ns-sym (if (zero? (+ fail error)) "ok" "failures") test pass fail error ""])
                           (catch Exception e
                             (let [r (root-cause e)]
                               [ns-sym "load-error" "" "" "" ""
                                (str (.Name (class r)) ": " (first (str/split-lines (str (.Message r)))))])))]
                 (binding [*out* *err*] (println "RESULT" (str/join "\t" row)))
                 row)))]
  (spit results (str/join "\n" (cons "namespace\tstatus\ttest\tpass\tfail\terror\tnote"
                                     (map #(str/join "\t" %) rows)))
        :encoding System.Text.Encoding/UTF8)
  (shutdown-agents)
  (Environment/Exit (if (every? #(= "ok" (second %)) rows) 0 1)))
