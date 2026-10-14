using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Job.ForexEUR.Repository;

namespace Platform.Job.ForexEUR
{
    public class Worker : BackgroundService
    {
        private static readonly TimeSpan DefaultInterval = TimeSpan.FromHours(1);
        private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromMinutes(1);

        private readonly ILogger<Worker> _logger;
        private readonly IPlatformRepository _platformRepository;
        private readonly TimeSpan _interval;
        private readonly TimeSpan _retryDelay;

        public Worker(ILogger<Worker> logger, IPlatformRepository platformRepository)
            : this(logger, platformRepository, DefaultInterval, DefaultRetryDelay)
        {
        }

        internal Worker(ILogger<Worker> logger, IPlatformRepository platformRepository, TimeSpan interval, TimeSpan retryDelay)
        {
            _logger = logger;
            _platformRepository = platformRepository;
            _interval = interval;
            _retryDelay = retryDelay;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TimeSpan nextRun;
                try
                {
                    _logger.LogInformation("processing job: {time}", DateTimeOffset.Now);
                    await _platformRepository.ProcessForex();
                    nextRun = _interval;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "error on ProcessForex(), retrying in {retryDelay}", _retryDelay);
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
    }
}
