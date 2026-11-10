(ns arcadia.headless-runner
  "Drives Arcadia's async test harness (arcadia.internal.test) from batchmode.
  HeadlessTestRunner.cs calls `start`, then `poll` once per editor update."
  (:require [arcadia.internal.test :as at]
            [clojure.string :as str]))

(defonce ^:private state (atom nil))

(defn- report [d]
  (with-out-str (at/print-data {:indent 0} (at/data-scuba d))))

(defn- counts [text]
  (let [lines (str/split-lines text)
        n (fn [re] (count (filter #(re-find re %) lines)))]
    {:pass (n #"^[|\s]*PASS ")
     :fail (n #"^[|\s]*FAIL ")
     :error (n #"^[|\s]*ERROR")}))

(defn start
  "Loads the test file and starts every test registered in
  arcadia.arcadia-tests. Returns immediately; tests finish asynchronously."
  [tests-file]
  (load-file tests-file)
  ;; The test registry is keyed by Namespace objects, not symbols.
  (let [w (at/run-tests (the-ns 'arcadia.arcadia-tests))]
    (reset! state {:exec (.results w)})
    :started))

(defn poll
  "nil while tests run. Once complete (or when `force?` is true), returns
  [complete? report-text pass fail error]."
  [force?]
  (let [d @(:exec @state)
        complete? (boolean (::at/complete d))]
    (when (or complete? force?)
      (let [text (report d)
            {:keys [pass fail error]} (counts text)]
        [complete? text pass fail error]))))
