(ns smoke.player-smoke
  "Player-build smoke check. HeadlessPlayerBuild copies this file into the harness project's
  Assets/smoke/, hooks `start` onto a GameObject in a scene, stores state on it, exports and
  builds a player. In the player, `start` logs one marker line and quits."
  (:require [arcadia.core :as ac]
            [arcadia.linear :as l]
            [clojure.string :as str]))

(defn start [go k]
  (let [checks {:clojure (clojure-version)
                :linear (str (l/v3+ (l/v3 1 2 3) (l/v3 1 1 1)))
                :state (ac/state go :smoke)
                :string (str/upper-case "ok")
                :editor? (UnityEngine.Application/isEditor)}]
    (UnityEngine.Debug/Log (str "ARCADIA-PLAYER-SMOKE " (pr-str checks)))
    (UnityEngine.Application/Quit 0)))
