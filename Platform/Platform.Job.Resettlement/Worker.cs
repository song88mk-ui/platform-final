using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Job.Resettlement.Repository;
using Platform.Shared.Data.Platform;
using Platform.Shared.Utility;

namespace Platform.Job.Resettlement
{
    public class Worker : BackgroundService
    {
        const int MaxRetryCount = 8;

        private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromMinutes(1);

        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _provider;
        private readonly IMessageBroker _messageBroker;
        private readonly IPlatformRepository _platformRepository;
        private readonly IConfiguration _configuration;
        private readonly TimeSpan _retryDelay;
        private Stack<int> _fibonnaci = new Stack<int>();

        public Worker(ILogger<Worker> logger,
                IServiceProvider provider,
                IMessageBroker messageBroker,
                IPlatformRepository platformRepository,
                IConfiguration configuration)
            : this(logger, provider, messageBroker, platformRepository, configuration, DefaultRetryDelay)
        {
        }

        internal Worker(ILogger<Worker> logger,
                IServiceProvider provider,
                IMessageBroker messageBroker,
                IPlatformRepository platformRepository,
                IConfiguration configuration,
                TimeSpan retryDelay)
        {
            _logger = logger;
            _provider = provider;
            _messageBroker = messageBroker;
            _platformRepository = platformRepository;
            _configuration = configuration;
            _retryDelay = retryDelay;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await base.StopAsync(cancellationToken);
            }
            finally
            {
                // after the loop has stopped, so an in-flight ProcessBets never publishes on a disposed broker
                _messageBroker.Dispose();
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Connecting to Broker");
            _messageBroker.Connect();
            _logger.LogInformation("Connected. Running job loop");

            while (!stoppingToken.IsCancellationRequested)
            {
                TimeSpan nextRun;
                try
                {
                    using var scope = _provider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<PlatformContext>();

                    var bets = await context.OperatorSettlementRetry
                        .Where(c => c.IsRetry == true && c.RetryCount < MaxRetryCount)
                        .AsNoTracking().ToListAsync(stoppingToken);

                    _logger.LogInformation("Retrieved {betCount} bets for resettlement", bets.Count);

                    if (bets.Count > 0)
                    {
                        _platformRepository.ProcessBets(bets, _messageBroker, stoppingToken);

                        nextRun = TimeSpan.FromMinutes(GetNextRetry(false));
                    }
                    else
                    {
                        nextRun = TimeSpan.FromMinutes(GetNextRetry(true));
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "error processing bets, retrying in {RetryDelay}", _retryDelay);
                    nextRun = _retryDelay;
                }

                try
                {
                    await Task.Delay(nextRun, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private int GetNextRetry(bool isReset)
        {
            if (isReset)
                _fibonnaci.Clear();

            if (_fibonnaci.Count == 0)
            {
                var times = _configuration["Custom:Fibonnaci"].Split(',').Select(s => Convert.ToInt32(s)).OrderByDescending(o => o).ToList();

                foreach (var time in times)
                {
                    _fibonnaci.Push(time);
                }
            }

            return _fibonnaci.Pop();
        }
    }
}
