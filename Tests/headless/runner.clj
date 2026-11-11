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

(defn- child-testers
  "The testers directly under data-scuba'd node `x`, through its result groups."
  [x]
  (case (::at/type x)
    ::at/tester-exec (::at/children x)
    ::at/tester-state (mapcat #(child-testers %) (::at/result-groups x))
    ::at/result-group (mapcat #(if (#{::at/tester-state ::at/result-group} (::at/type %))
                                 (if (= ::at/tester-state (::at/type %)) [%] (child-testers %))
                                 [])
                              (::at/results x))
    []))

(defn- open-testers
  "Label paths of the innermost testers under `x` that haven't completed:
  ones still waiting on their own results, not just on a child."
  ([x] (open-testers x []))
  ([x path]
   (let [path (if-let [l (::at/label x)] (conj path l) path)
         open-children (remove ::at/complete (child-testers x))]
     (cond
       (::at/complete x) []
       (seq open-children) (mapcat #(open-testers % path) open-children)
       :else [path]))))

(defn start
  "Loads the test file and starts every test registered in
  arcadia.arcadia-tests. Returns immediately; tests finish asynchronously."
  [tests-file]
  (load-file tests-file)
  ;; The test registry is keyed by Namespace objects, not symbols.
  (let [w (at/run-tests (the-ns 'arcadia.arcadia-tests))]
    (reset! state {:exec (.results w)
                   :last-text nil
                   :changed-at (System.DateTime/Now)
                   :checked-at (System.DateTime/MinValue)})
    :started))

(defn- seconds-since [^System.DateTime t]
  (.TotalSeconds (.Subtract (System.DateTime/Now) t)))

(defn poll
  "nil while tests run. Returns [status report-text pass fail error pending]
  once they finish, once results have stopped changing for `quiet-seconds`
  (tests waiting on Play-mode frames never finish in edit mode), or when
  `force?` is true. status is :complete, :quiet or :forced; pending lists the
  label paths of testers that never completed."
  [force? quiet-seconds]
  (let [d @(:exec @state)
        complete? (boolean (::at/complete d))
        ;; Rendering the report every editor update is wasteful; once a second is plenty.
        check? (or complete? force? (<= 1 (seconds-since (:checked-at @state))))]
    (when check?
      (let [text (report d)]
        (when (not= text (:last-text @state))
          (swap! state assoc :last-text text :changed-at (System.DateTime/Now)))
        (swap! state assoc :checked-at (System.DateTime/Now))
        (let [quiet? (<= quiet-seconds (seconds-since (:changed-at @state)))]
          (when (or complete? force? quiet?)
            (let [{:keys [pass fail error]} (counts text)
                  pending (when-not complete?
                            (->> (open-testers (at/data-scuba d))
                                 (map #(str/join " / " %))
                                 vec))]
              [(cond complete? :complete force? :forced :else :quiet)
               text pass fail error (str/join "\n" pending) (count pending)])))))))
