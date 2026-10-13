using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Platform.Betting.API.IntegrationTests.TestSupport
{
    /// <summary>
    /// A loopback HTTP server that stands in for every service Platform.Betting.API calls out to.
    /// </summary>
    /// <remarks>
    /// <c>appsettings.json</c> points <c>API:*</c> at real MTS and GTS hosts, so the fixture aims all
    /// seven of those keys here. A path with no registered response gets a 404, so a test never
    /// reaches a real service by accident. Every request is recorded with its body, in arrival order,
    /// so a test can check what the API sent. Same shape as platform-web's WebpropsStubServer.
    /// </remarks>
    public sealed class StubHttpServer : IDisposable
    {
        public sealed record RecordedRequest(string Method, string Path, string Body);

        private readonly HttpListener _listener = new();
        private readonly ConcurrentDictionary<string, (HttpStatusCode Status, string Body)> _responses = new(StringComparer.OrdinalIgnoreCase);
        private readonly CancellationTokenSource _stopping = new();
        private readonly Task _loop;

        public string BaseUrl { get; }

        public ConcurrentQueue<RecordedRequest> Requests { get; } = new();

        public StubHttpServer()
        {
            BaseUrl = $"http://127.0.0.1:{FreePort()}";
            _listener.Prefixes.Add($"{BaseUrl}/");
            _listener.Start();
            _loop = Task.Run(AcceptLoopAsync);
        }

        public void SetResponse(string path, string json) => _responses[path] = (HttpStatusCode.OK, json);

        public IEnumerable<RecordedRequest> RequestsTo(string path) =>
            Requests.Where(r => string.Equals(r.Path, path, StringComparison.OrdinalIgnoreCase));

        private async Task AcceptLoopAsync()
        {
            while (!_stopping.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception) when (_stopping.IsCancellationRequested)
                {
                    return;
                }

                _ = Task.Run(() => RespondAsync(context));
            }
        }

        private async Task RespondAsync(HttpListenerContext context)
        {
            var path = context.Request.Url?.AbsolutePath ?? string.Empty;
            string requestBody;
            using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                requestBody = await reader.ReadToEndAsync();
            Requests.Enqueue(new RecordedRequest(context.Request.HttpMethod, path, requestBody));

            var (status, body) = _responses.TryGetValue(path, out var registered)
                ? registered
                : (HttpStatusCode.NotFound, "{\"message\":\"no stub registered\"}");

            try
            {
                var bytes = Encoding.UTF8.GetBytes(body);
                context.Response.StatusCode = (int)status;
                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes);
                context.Response.Close();
            }
            catch (Exception) when (_stopping.IsCancellationRequested)
            {
                // the listener was stopped under an in-flight response
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
            _stopping.Cancel();
            _listener.Stop();
            _listener.Close();
            try { _loop.Wait(TimeSpan.FromSeconds(5)); } catch (AggregateException) { }
            _stopping.Dispose();
        }
    }
}
