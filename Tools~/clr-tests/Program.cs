// Runs ClojureCLR test namespaces one at a time against the bundled Clojure.dll
// and prints one result line per namespace, so a crash in one doesn't hide the rest.
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
        Environment.SetEnvironmentVariable("CLOJURE_LOAD_PATH", source);
        Directory.SetCurrentDirectory(source);

        var require = RT.var("clojure.core", "require");
        var loadFrom = RT.var("clojure.core", "assembly-load-from");
        foreach (var dll in new[] { "clojure.tools.namespace.dll", "clojure.data.generators.dll",
                                    "clojure.test.generative.dll", "clojure.test.check.dll" })
            Try(() => System.Reflection.Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dll)));
        require.invoke(Symbol.intern("clojure.test"));

        var namespaces = args.Skip(2).ToArray();
        if (namespaces.Length == 0)
            namespaces = Directory.GetFiles(Path.Combine(source, "clojure", "test_clojure"), "*.clj*")
                .Select(f => "clojure.test-clojure." + Path.GetFileNameWithoutExtension(f).Replace('_', '-'))
                .OrderBy(n => n).ToArray();

        var runTests = RT.var("clojure.test", "run-tests");
        int bad = 0;
        using (var w = new StreamWriter(results) { AutoFlush = true })
        {
            w.WriteLine("namespace\tstatus\ttest\tpass\tfail\terror\tnote");
            foreach (var ns in namespaces)
            {
                string line;
                try
                {
                    var sym = Symbol.intern(ns);
                    require.invoke(sym);
                    var summary = (IPersistentMap)runTests.invoke(sym);
                    long Get(string k) => Convert.ToInt64(summary.valAt(Keyword.intern(k)) ?? 0L);
                    var (t, p, f, e) = (Get("test"), Get("pass"), Get("fail"), Get("error"));
                    var status = f + e == 0 ? "ok" : "failures";
                    if (f + e != 0) bad++;
                    line = $"{ns}\t{status}\t{t}\t{p}\t{f}\t{e}\t";
                }
                catch (Exception ex)
                {
                    bad++;
                    var root = ex; while (root.InnerException != null) root = root.InnerException;
                    line = $"{ns}\tload-error\t\t\t\t\t{root.GetType().Name}: {root.Message.Split('\n')[0]}";
                }
                w.WriteLine(line);
                Console.Error.WriteLine("RESULT " + line);
            }
        }
        // Agent and future threads are foreground threads; without this the process never exits.
        RT.var("clojure.core", "shutdown-agents").invoke();
        Environment.Exit(bad == 0 ? 0 : 1);
        return 0;
    }

    static void Try(Action a) { try { a(); } catch (Exception e) { Console.Error.WriteLine("warn: " + e.Message); } }
}
