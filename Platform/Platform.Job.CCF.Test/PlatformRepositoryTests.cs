using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Platform.Job.CCF.Repository;
using Platform.Shared.Models;
using Xunit;

namespace Platform.Job.CCF.Test
{
    public class PlatformRepositoryTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

        // Three data rows (columns 0, 3, 5, 6 are read; whole numbers keep the parse culture independent).
        private const string Report = "header\n"
            + "1700000001,a,b,user1,d,2,1,,,\n"
            + "1700000002,a,b,user2,d,2,1,,,\n"
            + "1700000003,a,b,user3,d,2,1,,,\n";

        [Fact]
        public async Task ProcessCCF_CancelledWhileTheReportRequestIsInFlight_StopsPromptly()
        {
            // Arrange
            var handler = new StubHandler(async token =>
            {
                await Task.Delay(TimeSpan.FromHours(1), token);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            using var http = new HttpClient(handler);
            using var cts = new CancellationTokenSource();
            var repository = CreateRepository(http);

            // Act
            var run = repository.ProcessCCF(cts.Token);
            await handler.Requested.WaitAsync(Timeout);
            cts.Cancel();

            // Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(Timeout));
        }

        [Fact]
        public async Task ProcessCCF_CancelledBetweenRows_StopsBeforeTheNextRowInsteadOfSwallowingTheCancellation()
        {
            // Arrange
            var handler = new StubHandler(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Report) }));
            using var http = new HttpClient(handler);
            using var cts = new CancellationTokenSource();
            var repository = CreateRepository(http);

            // The invalid connection string makes every row fail fast, and each failure is written to the console.
            // Cancelling on that write puts the cancellation between two rows.
            var console = new CancelOnWriteLine(cts);
            var originalOut = Console.Out;
            Console.SetOut(console);

            // Act
            try
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.ProcessCCF(cts.Token));
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            // Assert
            Assert.Equal(1, console.Lines);
        }

        private static PlatformRepository CreateRepository(HttpClient http)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["API:MTSCCFAPI"] = "http://ccf.test/",
                    ["Custom:MTSBookmakerId"] = "1",
                    ["ConnectionStrings:Platform"] = "not a connection string"
                })
                .Build();

            return new PlatformRepository(new FakeAuthRepository(), configuration, http);
        }

        private sealed class FakeAuthRepository : IMTSAuthRepository
        {
            public Task<BaseResponseDTO<MTSAuthResponseDTO>> GetMTSAuthorizationToken() =>
                Task.FromResult(new BaseResponseDTO<MTSAuthResponseDTO> { Result = new MTSAuthResponseDTO { Token = "token" } });
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<CancellationToken, Task<HttpResponseMessage>> _respond;
            private readonly TaskCompletionSource<bool> _requested = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public StubHandler(Func<CancellationToken, Task<HttpResponseMessage>> respond)
            {
                _respond = respond;
            }

            public StubHandler(Func<Task<HttpResponseMessage>> respond)
                : this(_ => respond())
            {
            }

            public Task Requested => _requested.Task;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                _requested.TrySetResult(true);
                return _respond(cancellationToken);
            }
        }

        private sealed class CancelOnWriteLine : TextWriter
        {
            private readonly CancellationTokenSource _cts;
            private int _lines;

            public CancelOnWriteLine(CancellationTokenSource cts)
            {
                _cts = cts;
            }

            public int Lines => Volatile.Read(ref _lines);

            public override Encoding Encoding => Encoding.UTF8;

            public override void WriteLine(string value)
            {
                Interlocked.Increment(ref _lines);
                _cts.Cancel();
            }
        }
    }
}
