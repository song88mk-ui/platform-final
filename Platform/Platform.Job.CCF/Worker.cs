using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Job.CCF.Repository;
using Platform.Shared.Utility;

namespace Platform.Job.CCF
{
    public class Worker : BackgroundService
    {
        private static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(5);
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
                    await _platformRepository.ProcessCCF(stoppingToken);
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    nextRun = _interval;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "error during execution, retrying in {RetryDelay}", _retryDelay);
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
