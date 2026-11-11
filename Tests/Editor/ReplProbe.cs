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
            // "localhost", as Arcadia's repl-client scripts use: clojure.core.server binds the first
            // address localhost resolves to, which on Windows is usually ::1, not 127.0.0.1.
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
        void Check(string what, NetworkStream s, StringBuilder got, Func<string, bool> ok)
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
                    if (ok(got.ToString(start, got.Length - start))) { Pass(what); return; }
                }
                else Thread.Sleep(50);
            }
            var tail = got.ToString(start, got.Length - start);
            if (tail.Length > 600) tail = tail.Substring(tail.Length - 600);
            Fail(what, "no matching reply; received: " + tail.Replace("\n", "\n"));
        }
    }
}
