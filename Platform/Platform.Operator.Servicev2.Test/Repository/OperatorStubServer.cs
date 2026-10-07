using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Platform.Operator.Servicev2.Test.Repository
{
    // A loopback HTTP server standing in for an operator wallet. BettingRepository builds its own
    // HttpClient per thread and endpoint, so the tests point Bet.Endpoint here instead of adding
    // an HTTP seam to production code. Same approach as platform-web's WebpropsStubServer.
    //
    // Every request gets the current Status and Body, and its body is recorded so a test can check
    // what the repository sent. Status and Body can change between calls, so one test can make the
    // first wallet call fail and the replayed retry succeed.
    internal sealed class OperatorStubServer : IDisposable
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly Task _loop;

        internal OperatorStubServer(HttpStatusCode status, string body)
        {
            var port = FreePort();
            Endpoint = $"http://127.0.0.1:{port}";
            _listener.Prefixes.Add($"{Endpoint}/");
            Status = status;
            Body = body;
            _listener.Start();
            _loop = Task.Run(Serve);
        }

        internal string Endpoint { get; }

        internal HttpStatusCode Status { get; set; }

        internal string Body { get; set; }

        // Optional per-path answers that win over Status and Body, for flows that make several
        // different OG calls (debit, transaction status, wallet balance) and need each to answer
        // differently. The path is the URL path without the query string.
        internal Dictionary<string, (HttpStatusCode Status, string Body)> PathResponses { get; } = new Dictionary<string, (HttpStatusCode, string)>();

        internal List<(string Path, string Body)> Requests { get; } = new List<(string, string)>();

        // A port that was free a moment ago and has nothing listening on it, for the
        // connection-refused case.
        internal static string UnreachableEndpoint() => $"http://127.0.0.1:{FreePort()}";

        private async Task Serve()
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception) when (!_listener.IsListening)
                {
                    return;
                }

                using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                {
                    var requestBody = await reader.ReadToEndAsync();
                    lock (Requests)
                        Requests.Add((context.Request.Url.AbsolutePath, requestBody));
                }

                var (status, body) = PathResponses.TryGetValue(context.Request.Url.AbsolutePath, out var perPath)
                    ? perPath
                    : (Status, Body);
                var bytes = Encoding.UTF8.GetBytes(body);
                context.Response.StatusCode = (int)status;
                context.Response.ContentType = "application/json";
                await context.Response.OutputStream.WriteAsync(bytes);
                context.Response.Close();
            }
        }

        private static int FreePort()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }

        public void Dispose()
        {
            _listener.Stop();
            _listener.Close();
            try { _loop.Wait(TimeSpan.FromSeconds(5)); } catch (AggregateException) { }
        }
    }
}
