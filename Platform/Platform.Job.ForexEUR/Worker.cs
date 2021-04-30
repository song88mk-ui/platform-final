using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Job.ForexEUR.Repository;

namespace Platform.Job.ForexEUR
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
            while (true)
            {
                try
                {
                    await Task.Delay(3600000, stoppingToken);
                    await _platformRepository.ProcessForex();
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
