// Starts ClojureCLR's clojure.main on run-tests.clj with the test root as the working
// directory and load path, using whichever ClojureCLR runtime sits next to this exe.
// Usage: ClrTestHost <test root> <results file> [namespace ...]
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using clojure.lang;

static class Program
{
    static int Main(string[] args)
    {
        var root = Path.GetFullPath(args[0]);
        var results = Path.GetFullPath(args[1]);
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var script = Path.Combine(baseDir, "run-tests.clj");

        // ClojureCLR 1.12 finds library sources (spec, tools.reader, ...) embedded in assemblies
        // already loaded into the AppDomain. Unity loads every managed DLL under Assets/, so
        // loading the ones beside us here gives the same picture.
        foreach (var dll in Directory.GetFiles(baseDir, "*.dll"))
        {
            try { Assembly.LoadFrom(dll); }
            catch (Exception e) { Console.Error.WriteLine($"warn: {Path.GetFileName(dll)}: {e.Message}"); }
        }

        Environment.SetEnvironmentVariable("CLOJURE_LOAD_PATH", root);
        Directory.SetCurrentDirectory(root);
        // Like upstream's Clojure.Main: RT.Init loads spec, turns on macro spec checks and sets up
        // the user namespace. The bundled 1.10 runtime may not have it, hence reflection.
        typeof(RT).GetMethod("Init", Type.EmptyTypes)?.Invoke(null, null);
        RT.var("clojure.core", "require").invoke(Symbol.intern("clojure.main"));
        var mainArgs = new[] { script, results }.Concat(args.Skip(2)).ToArray<object>();
        RT.var("clojure.main", "main").applyTo(RT.seq(mainArgs));
        return 0; // run-tests.clj exits the process itself
    }
}
