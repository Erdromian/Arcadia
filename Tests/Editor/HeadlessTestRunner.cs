// Runs Arcadia's own tests (Tests/arcadia/arcadia_tests.clj) in Unity batchmode.
// Lives in its own editor-only assembly and reaches Arcadia by reflection, so it
// still reports when Arcadia's C# fails to compile. Entry points:
//   -executeMethod Arcadia.Headless.HeadlessTestRunner.Run
//       [-arcadiaResults <file>] [-arcadiaTimeout <seconds>]
//   -executeMethod Arcadia.Headless.HeadlessTestRunner.UseNetFramework   (with -quit)
// Exit codes: 0 all pass, 1 failures/errors, 2 Arcadia did not load, 3 timeout.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using clojure.lang;

namespace Arcadia.Headless
{
    public static class HeadlessTestRunner
    {
        static string resultsFile;
        static DateTime deadline;
        static Var poll;

        public static void UseNetFramework()
        {
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Unity_4_8);
            AssetDatabase.SaveAssets();
            Debug.Log("[arcadia-headless] API compatibility set to .NET Framework");
        }

        public static void Run()
        {
            resultsFile = Path.GetFullPath(Arg("-arcadiaResults") ?? "arcadia-test-results.txt");
            deadline = DateTime.Now.AddSeconds(double.Parse(Arg("-arcadiaTimeout") ?? "600"));
            Write("api-compat: " + PlayerSettings.GetApiCompatibilityLevel(NamedBuildTarget.Standalone)
                  + "\nunity: " + Application.unityVersion + "\n", append: false);
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
                init.GetMethod("Initialize").Invoke(null, null);

                var root = Path.GetDirectoryName(Path.GetDirectoryName(typeof(RT).Assembly.Location));
                RT.var("clojure.core", "load-file").invoke(Path.Combine(root, "Tests", "headless", "runner.clj"));
                RT.var("arcadia.headless-runner", "start").invoke(Path.Combine(root, "Tests", "arcadia", "arcadia_tests.clj"));
                poll = RT.var("arcadia.headless-runner", "poll");
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
                var r = poll.invoke(timedOut) as IPersistentVector;
                if (r == null) return;
                EditorApplication.update -= Tick;
                bool complete = (bool)r.nth(0);
                long fail = Convert.ToInt64(r.nth(3)), error = Convert.ToInt64(r.nth(4));
                var summary = $"complete: {complete}\npass: {r.nth(2)}\nfail: {fail}\nerror: {error}\n";
                Finish(!complete ? 3 : fail + error > 0 ? 1 : 0, summary + "\n" + r.nth(1));
            }
            catch (Exception e)
            {
                EditorApplication.update -= Tick;
                Finish(2, "RUNNER-ERROR: " + Root(e));
            }
        }

        static void Finish(int code, string text)
        {
            Write(text + "\nexit: " + code + "\n", append: true);
            Debug.Log("[arcadia-headless] exit " + code + "\n" + text);
            EditorApplication.Exit(code);
        }

        static void Write(string text, bool append)
        {
            if (append) File.AppendAllText(resultsFile, text); else File.WriteAllText(resultsFile, text);
        }

        static string Root(Exception e)
        {
            var s = e.GetType().Name + ": " + e.Message;
            while (e.InnerException != null) { e = e.InnerException; s += "\n  caused by " + e.GetType().Name + ": " + e.Message; }
            return s;
        }

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            var i = Array.IndexOf(a, name);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
        }
    }
}
