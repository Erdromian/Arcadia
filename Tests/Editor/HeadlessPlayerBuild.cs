// Builds a Windows Mono player from a one-scene smoke project and leaves it for
// Tools~/headless/run-player-smoke.ps1 to run. Steps: copy Tests/headless/player/player_smoke.clj
// into Assets/smoke/, point Assets/configuration.edn's :export-namespaces at it, make a scene whose
// GameObject has the smoke `start` hook and some state, run Arcadia's "Prepare for Export", build.
//   -executeMethod Arcadia.Headless.HeadlessPlayerBuild.Run -arcadiaBuildOut <dir> [-arcadiaResults <file>]
// Only for a throwaway harness project: it overwrites Assets/configuration.edn unless that file
// exists without the harness marker. Exit codes: 0 built, 1 build failed, 2 setup failed.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using clojure.lang;

namespace Arcadia.Headless
{
    public static class HeadlessPlayerBuild
    {
        const string Marker = ";; written by Arcadia.Headless.HeadlessPlayerBuild";
        const string ScenePath = "Assets/smoke/Smoke.unity";
        static string resultsFile;

        public static void Run()
        {
            resultsFile = Path.GetFullPath(Arg("-arcadiaResults") ?? "arcadia-player-build.txt");
            File.WriteAllText(resultsFile, "unity: " + Application.unityVersion + "\n");
            var outDir = Path.GetFullPath(Arg("-arcadiaBuildOut") ?? "Builds/smoke");
            int code;
            try
            {
                Setup();
                code = Build(outDir);
            }
            catch (Exception e)
            {
                Log("SETUP-ERROR: " + Root(e));
                code = 2;
            }
            Log("exit: " + code);
            EditorApplication.Exit(code);
        }

        static Type init;

        static void Setup()
        {
            init = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("Arcadia.Initialization", false))
                .FirstOrDefault(t => t != null);
            if (init == null) throw new Exception("type Arcadia.Initialization not found (Arcadia's C# did not compile)");
            Log("step: initialize Arcadia");
            init.GetMethod("Initialize").Invoke(null, null);

            var root = Path.GetDirectoryName(Path.GetDirectoryName(typeof(RT).Assembly.Location));
            Directory.CreateDirectory("Assets/smoke");
            File.Copy(Path.Combine(root, "Tests", "headless", "player", "player_smoke.clj"), "Assets/smoke/player_smoke.clj", true);
            const string config = "Assets/configuration.edn";
            if (File.Exists(config) && !File.ReadAllText(config).Contains(Marker))
                throw new Exception(config + " exists and wasn't written by this harness; not overwriting it");
            File.WriteAllText(config, Marker + "\n{:export-namespaces [smoke.player-smoke]}\n");
            AssetDatabase.Refresh();
            RT.var("arcadia.internal.config", "update!").invoke();

            Log("step: make scene");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var go = new GameObject("smoke");
            RT.var("clojure.core", "require").invoke(Symbol.intern("arcadia.core"));
            RT.var("clojure.core", "require").invoke(Symbol.intern("smoke.player-smoke"));
            var kw = Keyword.intern("smoke");
            RT.var("arcadia.core", "hook+").invoke(go, Keyword.intern("start"), kw, RT.var("smoke.player-smoke", "start"));
            RT.var("arcadia.core", "state+").invoke(go, kw, RT.map(Keyword.intern("n"), 42L, Keyword.intern("s"), "kept"));
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new Exception("could not save " + ScenePath);

            Log("step: prepare export");
            // Arcadia's editor code is in Assembly-CSharp-Editor, which an asmdef can't reference.
            var pipeline = init.Assembly.GetType("Arcadia.BuildPipeline", true);
            pipeline.GetMethod("PrepareExport").Invoke(null, null);
            var exportFolder = (string)pipeline.GetField("ExportAssetsFolder").GetValue(null);
            Log("exported to " + exportFolder + ": " + string.Join(" ", Directory.GetFiles(exportFolder, "*.dll").Select(Path.GetFileName)));
        }

        static int Build(string outDir)
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Log("step: build player");
            var report = UnityEditor.BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                target = BuildTarget.StandaloneWindows64,
                locationPathName = Path.Combine(outDir, "ArcadiaSmoke.exe"),
                options = BuildOptions.None,
            });
            Log("build: " + report.summary.result + ", " + report.summary.totalErrors + " errors, " + report.summary.totalSize + " bytes");
            foreach (var m in report.steps.SelectMany(s => s.messages).Where(m => m.type == LogType.Error || m.type == LogType.Exception))
                Log("build error: " + m.content);
            return report.summary.result == BuildResult.Succeeded ? 0 : 1;
        }

        static void Log(string line)
        {
            File.AppendAllText(resultsFile, line + "\n");
            Debug.Log("[arcadia-player-build] " + line);
        }

        static string Root(Exception e)
        {
            var s = e.GetType().Name + ": " + e.Message;
            while (e.InnerException != null) { e = e.InnerException; s += "\n  caused by " + e.GetType().Name + ": " + e.Message; }
            return s + "\n" + string.Join("\n", (e.StackTrace ?? "").Split('\n').Take(25));
        }

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            var i = Array.IndexOf(a, name);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
        }
    }
}
