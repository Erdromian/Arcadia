// Connects to Arcadia's socket REPL (37220) and nREPL (3722) servers from background threads and
// evaluates a pure form and a main-thread form on each. HeadlessTestRunner starts it after the
// tests and keeps the editor ticking (evals run on the main thread through editor callbacks)
// until Done. Speaks just enough of each protocol to check a round trip; no client library.
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Arcadia.Headless
{
    class ReplProbe
    {
        const int SocketPort = 37220, NReplPort = 3722;
        readonly string unityVersion;
        readonly int timeoutMs;
        readonly StringBuilder log = new StringBuilder();
        int pending = 2;
        public bool Ok { get; private set; } = true;
        public bool Done => Volatile.Read(ref pending) == 0;
        public string Report { get { lock (log) return log.ToString(); } }

        public ReplProbe(string unityVersion, int timeoutSeconds)
        {
            this.unityVersion = unityVersion;
            timeoutMs = timeoutSeconds * 1000;
        }

        public void Start()
        {
            Run("socket-repl", SocketRepl);
            Run("nrepl", NRepl);
        }

        void Run(string name, Action body)
        {
            new Thread(() =>
            {
                try { body(); }
                catch (Exception e) { Fail(name, e.GetType().Name + ": " + e.Message); }
                finally { Interlocked.Decrement(ref pending); }
            }) { IsBackground = true, Name = "arcadia-probe-" + name }.Start();
        }

        void Pass(string what) { lock (log) log.AppendLine("REPL PASS " + what); }
        void Fail(string what, string why) { lock (log) { Ok = false; log.AppendLine("REPL FAIL " + what + ": " + why); } }

        void SocketRepl()
        {
            // "localhost", as Arcadia's repl-client scripts use. The server is pinned to 127.0.0.1
            // (socket_repl.clj's server-defaults); localhost also reaches it when it resolves to ::1 first.
            using (var c = new TcpClient("localhost", SocketPort))
            {
                var s = c.GetStream();
                var got = new StringBuilder();
                Send(s, "(+ 1 2)\n");
                Check("socket-repl (+ 1 2)", s, got, t => t.Contains("3"));
                Send(s, "(UnityEngine.Application/unityVersion)\n");
                Check("socket-repl main-thread eval", s, got, t => t.Contains("\"" + unityVersion + "\""));
                Send(s, ":repl/quit\n");
            }
        }

        void NRepl()
        {
            using (var c = new TcpClient("127.0.0.1", NReplPort))
            {
                var s = c.GetStream();
                var got = new StringBuilder();
                Send(s, Dict("id", "1", "op", "describe"));
                Check("nrepl describe", s, got, t => Reply(t, "1") && t.Contains("8:versions"));
                Send(s, Dict("code", "(+ 1 2)", "id", "2", "op", "eval"));
                Check("nrepl (+ 1 2)", s, got, t => Reply(t, "2") && t.Contains("5:value1:3"));
                var quoted = "\"" + unityVersion + "\"";
                Send(s, Dict("code", "(UnityEngine.Application/unityVersion)", "id", "3", "op", "eval"));
                Check("nrepl main-thread eval", s, got, t => Reply(t, "3") && t.Contains("5:value" + quoted.Length + ":" + quoted));

                // What an editor (Calva, CIDER) does next: clone a session and send it with every op.
                var start = got.Length;
                Send(s, Dict("id", "4", "op", "clone"));
                if (!Check("nrepl clone", s, got, t => Reply(t, "4") && t.Contains("11:new-session36:"))) return;
                var at = got.ToString().IndexOf("11:new-session36:", start, StringComparison.Ordinal) + "11:new-session36:".Length;
                var session = got.ToString(at, 36);

                const string file = "(ns arcadia.probe.loaded) (defn answer \"The probe's answer.\" [] 42)";
                Send(s, Dict("file", file, "file-name", "loaded.clj", "file-path", "probe/loaded.clj",
                             "id", "5", "op", "load-file", "session", session));
                Check("nrepl load-file", s, got, t => Reply(t, "5") && t.Contains("#'arcadia.probe.loaded/answer"));
                Send(s, Dict("code", "(arcadia.probe.loaded/answer)", "id", "6", "op", "eval", "session", session));
                Check("nrepl eval after load-file", s, got, t => Reply(t, "6") && t.Contains("5:value2:42"));
                Send(s, Dict("id", "7", "ns", "user", "op", "info", "session", session, "symbol", "map"));
                Check("nrepl info clojure.core/map", s, got, t => Reply(t, "7") && t.Contains("3:doc") && t.Contains("12:arglists-str"));
                Send(s, Dict("id", "8", "ns", "user", "op", "info", "session", session, "symbol", "arcadia.probe.loaded/answer"));
                Check("nrepl info on loaded var", s, got, t => Reply(t, "8") && t.Contains("The probe's answer."));
                Send(s, Dict("id", "9", "ns", "user", "op", "eldoc", "session", session, "symbol", "map"));
                Check("nrepl eldoc", s, got, t => Reply(t, "9") && t.Contains("12:arglists-str"));
                Send(s, Dict("id", "10", "ns", "user", "op", "complete", "session", session, "symbol", "mapc"));
                Check("nrepl complete", s, got, t => Reply(t, "10") && t.Contains("9:candidate6:mapcat"));
                Send(s, Dict("id", "11", "op", "classpath", "session", session));
                Check("nrepl classpath", s, got, t => Reply(t, "11") && t.Contains("9:classpathl"));
            }
        }

        // A reply for request `id` has arrived and says it's done.
        static bool Reply(string text, string id)
        {
            var at = text.LastIndexOf("2:id" + id.Length + ":" + id, StringComparison.Ordinal);
            return at >= 0 && text.IndexOf("4:done", StringComparison.Ordinal) >= 0;
        }

        static string Dict(params string[] kv)
        {
            var b = new StringBuilder("d");
            foreach (var x in kv) b.Append(Encoding.UTF8.GetByteCount(x)).Append(':').Append(x);
            return b.Append('e').ToString();
        }

        static void Send(NetworkStream s, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            s.Write(bytes, 0, bytes.Length);
        }

        // Reads until `ok` holds for everything received on this connection so far, or times out.
        bool Check(string what, NetworkStream s, StringBuilder got, Func<string, bool> ok)
        {
            var start = got.Length;
            var deadline = DateTime.Now.AddMilliseconds(timeoutMs);
            var buf = new byte[8192];
            while (DateTime.Now < deadline)
            {
                if (s.DataAvailable)
                {
                    var n = s.Read(buf, 0, buf.Length);
                    if (n == 0) break;
                    got.Append(Encoding.UTF8.GetString(buf, 0, n));
                    if (ok(got.ToString(start, got.Length - start))) { Pass(what); return true; }
                }
                else Thread.Sleep(50);
            }
            var tail = got.ToString(start, got.Length - start);
            if (tail.Length > 600) tail = tail.Substring(tail.Length - 600);
            Fail(what, "no matching reply; received: " + tail.Replace("\n", "\\n"));
            return false;
        }
    }
}
