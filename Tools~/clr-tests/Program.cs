// Starts ClojureCLR's clojure.main on run-tests.clj with Source/ as the working
// directory and load path, using the bundled Infrastructure/Clojure.dll.
// Usage: ClrTestHost <Source dir> <results file> [namespace ...]
using System;
using System.IO;
using System.Linq;
using clojure.lang;

static class Program
{
    static int Main(string[] args)
    {
        var source = Path.GetFullPath(args[0]);
        var results = Path.GetFullPath(args[1]);
        var script = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "run-tests.clj");
        Environment.SetEnvironmentVariable("CLOJURE_LOAD_PATH", source);
        Directory.SetCurrentDirectory(source);
        RT.var("clojure.core", "require").invoke(Symbol.intern("clojure.main"));
        var mainArgs = new[] { script, results }.Concat(args.Skip(2)).ToArray<object>();
        RT.var("clojure.main", "main").applyTo(RT.seq(mainArgs));
        return 0; // run-tests.clj exits the process itself
    }
}
