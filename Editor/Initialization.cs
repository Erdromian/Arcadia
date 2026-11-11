using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using clojure.lang;

namespace Arcadia
{
    [InitializeOnLoad]
    public class Initialization
    {
        // ============================================================
        // Data

        // not statically initializing, because Application.dataPath sometimes isn't available (during serialization, for example)

        //public static string bestGuessDataPath;

        //public static string BestGuessDataPath
        //{
        //    get
        //    {
        //        if (bestGuessDataPath != null)
        //            return bestGuessDataPath;
        //        bestGuessDataPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets");
        //        UnityEngine.Debug.Log($"Computed BestGuessDataPath. BestGuessDataPath: {bestGuessDataPath}");
        //        return bestGuessDataPath;
        //    }
        //}


        //private static string pathToCompiled;

        //public static string PathToCompiled
        //{
        //    get {
        //        if (pathToCompiled != null)
        //            return pathToCompiled;
        //        pathToCompiled = Path.GetFullPath(VariadicPathCombine(BestGuessDataPath, "..", "Arcadia", "Compiled"));
        //        UnityEngine.Debug.Log($"Computed PathToCompiled. PathToCompiled: {pathToCompiled}");
        //        return pathToCompiled;
        //    }
        //}


        // this is for ArcadiaBehaviour
        private static bool initialized;

            // I have doubts about doing this in the static constructor - tsg
        static Initialization()
        {
            // Asset Import Workers are background Unity processes that load editor scripts too.
            // Initializing there starts a second socket REPL and nREPL server, which can take the
            // ports before the editor's own and then never run evals (they don't tick the editor loop).
            if (IsAssetImportWorker())
                return;
            Initialize();
        }

        // Not AssetDatabase.IsAssetImportWorkerProcess(): Unity forbids calling it during
        // serialization, and this constructor can run there (ArcadiaState.OnBeforeSerialize reaches
        // it through PlayModeInitialization). Workers are started with "-name AssetImportWorker<n>".
        static bool IsAssetImportWorker()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-name" && args[i + 1].StartsWith("AssetImportWorker", StringComparison.Ordinal))
                    return true;
            return false;
        }

        //public static String GetClojureDllFolder()
        //{
        //    return Path.GetDirectoryName(typeof(clojure.lang.RT).Assembly.Location).Substring(Directory.GetCurrentDirectory().Length + 1);
        //}

        //public static String GetArcadiaFolder()
        //{
        //    return Directory.GetParent(BasicPaths.ClojureDllFolder).ToString();
        //}

        public static void StartWatching()
        {
            //AssetPostprocessor.StartWatchingFiles();
            Var config = RT.var("arcadia.internal.config", "config");
            Var startAssetWatcher = RT.var("arcadia.internal.asset-watcher", "start-asset-watcher");
            startAssetWatcher.invoke(config.invoke());
        }

        public static void LoadLiterals()
        {
            // this has to happen here becasue the repl
            // binds a thread local *data-readers*
            Util.require("arcadia.data");
        }

        public static void Initialize()
        {
            if (initialized)
                return;

            DisableSpecChecking();
            SetInitialClojureLoadPath();
            LoadConfig();
            LoadLiterals();
            InitializeLoadPathExtensions();
            SetClojureLoadPath();
            InitUserNamespace();
            BuildPipeline.EnsureCompiledFolders();
            StartEditorCallbacks();
            StartWatching();
            LoadSocketREPL();
#if NET_4_6
            NRepl.StartServer();
#endif
            StartNudge();
            Debug.Log("Arcadia Started!");

            initialized = true;
        }

        // ClojureCLR 1.12 only refers clojure.core into `user` from RT.Init (which upstream's
        // Clojure.Main calls); without it the REPLs, which evaluate in `user`, can't resolve `+`.
        // PostBootstrapInit is the part of RT.Init that does that (it also loads user.clj and
        // starts any clojure.server.* servers, as Clojure does). The rest of RT.Init re-loads the
        // spec DLLs with Assembly.LoadFile, which Unity has already loaded from Infrastructure.
        static void InitUserNamespace()
        {
            RT.PostBootstrapInit();
        }

        private static void InitializeLoadPathExtensions()
        {
            Util.require("arcadia.internal.leiningen");
        }

        // workaround for spec issues
        static void DisableSpecChecking()
        {
            Environment.SetEnvironmentVariable("CLOJURE_SPEC_CHECK_ASSERTS", "false");
            Environment.SetEnvironmentVariable("CLOJURE_SPEC_SKIP_MACROS", "true");
            Environment.SetEnvironmentVariable("clojure.spec.check-asserts", "false");
            Environment.SetEnvironmentVariable("clojure.spec.skip-macros", "true");
        }

        public static void LoadConfig()
        {
            Util.require("arcadia.internal.config");
            RT.var("arcadia.internal.config", "update!").invoke();
        }

        public static void StartNudge()
        {
            Util.require("arcadia.internal.nudge");
        }

        public static string InitialClojureLoadPath()
        {
            var path = BasicPaths.PathToCompiled + Path.PathSeparator +
                    Path.GetFullPath(BasicPaths.PathCombine(BasicPaths.ClojureDllFolder, "..", "Source")) + Path.PathSeparator +
                 BasicPaths.BestGuessDataPath;
            return path;
        }

        // need this to set things up so we can get rest of loadpath after loading arcadia.internal.compiler
        public static void SetInitialClojureLoadPath()
        {
            try
            {
                Environment.SetEnvironmentVariable("CLOJURE_LOAD_PATH", InitialClojureLoadPath());

            }
            catch (InvalidOperationException e)
            {
                throw new SystemException("Error Loading Arcadia! Arcadia expects exactly one Arcadia folder (a folder with Clojure.dll in it)");
            }

        }

        public static void SetClojureLoadPath()
        {
            Util.require("arcadia.internal.compiler");
            string clojureDllFolder = BasicPaths.ClojureDllFolder;

            Environment.SetEnvironmentVariable("CLOJURE_LOAD_PATH",
                InitialClojureLoadPath() + Path.PathSeparator +
                RT.var("arcadia.internal.compiler", "loadpath-extension-string").invoke() + Path.PathSeparator +
                Path.GetFullPath(BasicPaths.PathCombine(clojureDllFolder, "..", "Libraries")));
        }

        static void LoadSocketREPL()
        {
            Util.require("arcadia.internal.socket-repl");
            Util.require("arcadia.internal.editor-callbacks");
            RT.var("arcadia.internal.socket-repl", "set-callback-and-start-server").invoke(RT.var("arcadia.internal.editor-callbacks", "add-callback"));
        }

        public static void StartEditorCallbacks()
        {
            Util.require("arcadia.internal.editor-callbacks");
            EditorCallbacks.Initialize();
        }

        // dunno where else to put this
        public static void PurgeAllCompiled()
        {
            var compiledDir = new DirectoryInfo(BasicPaths.PathToCompiled);
            if (compiledDir.Exists)
            {
                foreach (var file in compiledDir.GetFiles())
                {
                    file.Delete();
                }
            }
            var outerCompiledForExportDir = new DirectoryInfo(BasicPaths.PathCombine(BasicPaths.PathToCompiled, "..", "Export"));
            if (outerCompiledForExportDir.Exists)
            {
                foreach (var file in outerCompiledForExportDir.GetFiles())
                {
                    file.Delete();
                }
            }
            var exportDir = new DirectoryInfo(BasicPaths.PathToCompiledForExport);
            if (exportDir.Exists)
            {
                foreach (var file in exportDir.GetFiles())
                {
                    file.Delete();
                }
            }
        }

        public static void Clean()
        {
            PurgeAllCompiled();
        }
    }
}
