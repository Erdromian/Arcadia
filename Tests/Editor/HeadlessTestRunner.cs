// Runs Arcadia's own tests (Tests/arcadia/arcadia_tests.clj) in Unity batchmode.
// Lives in its own editor-only assembly and reaches Arcadia by reflection, so it
// still reports when Arcadia's C# fails to compile. Entry points:
//   -executeMethod Arcadia.Headless.HeadlessTestRunner.Run
//       [-arcadiaResults <file>] [-arcadiaTimeout <seconds>] [-arcadiaQuiet <seconds>] [-arcadiaNoRepl]
//       [-arcadiaPlayMode]   (enter Play mode first, then run the same tests there)
//   The run ends when every test completes, or when results haven't changed for -arcadiaQuiet
//   seconds (default 20): tests waiting on Play-mode frames never finish in edit mode, and are
//   listed as pending. After the tests, ReplProbe checks the socket REPL and nREPL answer evals.
//   -executeMethod Arcadia.Headless.HeadlessTestRunner.UseNetFramework   (with -quit)
// Exit codes: 0 all pass, 1 failures/errors (tests or REPL), 2 Arcadia did not load,
// 3 timeout (results still changing), 5 everything that ran passed but some tests stayed pending.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using clojure.lang;

namespace Arcadia.Headless
{
    [InitializeOnLoad]
    public static class HeadlessTestRunner
    {
        static string resultsFile;
        static DateTime deadline;
        static double quietSeconds;
        static Var poll;
        static ReplProbe probe;
        static int testCode;
        static string testText;

        public static void UseNetFramework()
        {
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Unity_4_8);
            AssetDatabase.SaveAssets();
            Debug.Log("[arcadia-headless] API compatibility set to .NET Framework");
        }

        const string PlayKey = "Arcadia.Headless.PlayMode";

        public static void Run()
        {
            resultsFile = Path.GetFullPath(Arg("-arcadiaResults") ?? "arcadia-test-results.txt");
            deadline = DateTime.Now.AddSeconds(double.Parse(Arg("-arcadiaTimeout") ?? "600"));
            quietSeconds = double.Parse(Arg("-arcadiaQuiet") ?? "20");
            var playMode = Environment.GetCommandLineArgs().Contains("-arcadiaPlayMode");
            Write("api-compat: " + PlayerSettings.GetApiCompatibilityLevel(NamedBuildTarget.Standalone)
                  + "\nunity: " + Application.unityVersion + "\nmode: " + (playMode ? "play" : "edit") + "\n", append: false);
            if (!playMode) { StartTests(); return; }
            // Entering Play mode reloads the domain (Unity's default), which clears these statics and
            // the Clojure runtime. Keep the settings in SessionState, which survives the reload; the
            // static constructor below picks them up and starts the tests once Play mode is running.
            SessionState.SetString(PlayKey, string.Join("|", resultsFile, deadline.Ticks, quietSeconds));
            Step("entering Play mode (domain reload: " + !(EditorSettings.enterPlayModeOptionsEnabled
                 && (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) != 0) + ")");
            EditorApplication.EnterPlaymode();
        }

        static HeadlessTestRunner()
        {
            var saved = SessionState.GetString(PlayKey, "");
            if (saved == "") return;
            var parts = saved.Split('|');
            resultsFile = parts[0];
            deadline = new DateTime(long.Parse(parts[1]));
            quietSeconds = double.Parse(parts[2]);
            EditorApplication.update += WaitForPlay;
        }

        static void WaitForPlay()
        {
            if (DateTime.Now > deadline)
            {
                EditorApplication.update -= WaitForPlay;
                SessionState.EraseString(PlayKey);
                Finish(3, "PLAY-MODE: never entered Play mode");
                return;
            }
            if (!EditorApplication.isPlaying) return;
            EditorApplication.update -= WaitForPlay;
            SessionState.EraseString(PlayKey);
            // Batchmode has no frame cap, so frames take a fraction of a millisecond of game time and
            // tests that wait N frames for physics (multiarg-test's collision) see almost no time
            // pass. Advance game time 1/60 s per frame, as a 60 fps editor would, still running flat out.
            Time.captureDeltaTime = 1f / 60f;
            Step("in Play mode (game time 1/60 s per frame)");
            StartTests();
        }

        static void StartTests()
        {
            try
            {
                var init = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("Arcadia.Initialization", false))
                    .FirstOrDefault(t => t != null);
                if (init == null)
                {
                    Finish(2, "LOAD-ERROR: type Arcadia.Initialization not found (Arcadia's C# did not compile; see Editor.log)");
                    return;
                }
                // Progress lines go to the results file as they happen, so a hang shows where it stopped.
                Step("initialize Arcadia");
                init.GetMethod("Initialize").Invoke(null, null);

                var root = Path.GetDirectoryName(Path.GetDirectoryName(typeof(RT).Assembly.Location));
                Step("load runner.clj");
                RT.var("clojure.core", "load-file").invoke(Path.Combine(root, "Tests", "headless", "runner.clj"));
                // Require the test file's dependencies one at a time first, so a hang names its namespace.
                foreach (var ns in new[] { "arcadia.core", "arcadia.linear", "arcadia.internal.editor-callbacks",
                                           "arcadia.internal.player-callbacks", "arcadia.debug" })
                {
                    Step("require " + ns);
                    RT.var("clojure.core", "require").invoke(Symbol.intern(ns));
                }
                Step("load and start arcadia_tests.clj");
                RT.var("arcadia.headless-runner", "start").invoke(Path.Combine(root, "Tests", "arcadia", "arcadia_tests.clj"));
                poll = RT.var("arcadia.headless-runner", "poll");
                Step("waiting for tests");
                EditorApplication.update += Tick;
            }
            catch (Exception e)
            {
                Finish(2, "LOAD-ERROR: " + Root(e));
            }
        }

        static void Tick()
        {
            try
            {
                var timedOut = DateTime.Now > deadline;
                var r = poll.invoke(timedOut, quietSeconds) as IPersistentVector;
                if (r == null) return;
                EditorApplication.update -= Tick;
                var status = ((Keyword)r.nth(0)).Name;
                long pass = Convert.ToInt64(r.nth(2)), fail = Convert.ToInt64(r.nth(3)), error = Convert.ToInt64(r.nth(4));
                long pendingCount = Convert.ToInt64(r.nth(6));
                var summary = $"status: {status}\npass: {pass}\nfail: {fail}\nerror: {error}\npending: {pendingCount}\n";
                // No assertions at all means the runner found no tests: never report that as a pass.
                if (pass + fail + error == 0) summary += "NO-RESULTS: no test assertions ran\n";
                if (pendingCount > 0)
                    summary += "\npending testers (never completed; tests waiting on Play-mode frames can't finish in edit mode):\n"
                               + r.nth(5) + "\n";
                // Failures win; then a run that never settled; then one that passed with tests left pending.
                testCode = fail + error > 0 || pass == 0 ? 1 : status == "forced" ? 3 : pendingCount > 0 ? 5 : 0;
                testText = summary + "\n" + r.nth(1);
                if (Environment.GetCommandLineArgs().Contains("-arcadiaNoRepl")) { Finish(testCode, testText); return; }
                Step("probing REPL servers");
                probe = new ReplProbe(Application.unityVersion, 60);
                probe.Start();
                EditorApplication.update += TickRepl;
            }
            catch (Exception e)
            {
                EditorApplication.update -= Tick;
                Finish(2, "RUNNER-ERROR: " + Root(e));
            }
        }

        // Keeps the editor updating so main-thread evals run, until both probes finish.
        static void TickRepl()
        {
            if (!probe.Done) return;
            EditorApplication.update -= TickRepl;
            var code = !probe.Ok && (testCode == 0 || testCode == 5) ? 1 : testCode;
            Finish(code, testText + "\n" + probe.Report);
        }

        static void Finish(int code, string text)
        {
            Write(text + "\nexit: " + code + "\n", append: true);
            Debug.Log("[arcadia-headless] exit " + code + "\n" + text);
            EditorApplication.Exit(code);
        }

        static void Step(string what)
        {
            var line = $"step {DateTime.Now:HH:mm:ss}: {what}";
            Write(line + "\n", append: true);
            Debug.Log("[arcadia-headless] " + line);
        }

        static void Write(string text, bool append)
        {
            if (append) File.AppendAllText(resultsFile, text); else File.WriteAllText(resultsFile, text);
        }

        static string Root(Exception e)
        {
            var s = e.GetType().Name + ": " + e.Message;
            while (e.InnerException != null) { e = e.InnerException; s += "\n  caused by " + e.GetType().Name + ": " + e.Message; }
            // The innermost stack names the failing Clojure function (munged) or C# method.
            var frames = (e.StackTrace ?? "").Split('\n').Take(30);
            return s + "\n  innermost stack:\n" + string.Join("\n", frames);
        }

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            var i = Array.IndexOf(a, name);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
        }
    }
}
