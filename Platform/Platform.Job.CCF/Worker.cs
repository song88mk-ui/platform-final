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
        private readonly ILogger<Worker> _logger;
        private readonly IPlatformRepository _platformRepository;
        public Worker(ILogger<Worker> logger, IPlatformRepository platformRepository)
        {
            _logger = logger;
            _platformRepository = platformRepository;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _platformRepository.ProcessCCF();
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(300000, stoppingToken);
            }
        }
    }
}
