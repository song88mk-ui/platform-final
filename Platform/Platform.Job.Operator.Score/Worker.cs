using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Job.Operator.Score.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.Job.Operator.Score
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IBettingRepository _bettingRepository;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;

        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _bettingRepository.PreloadSettledBets();

            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (true)
            {
                await Task.Delay(3600000, stoppingToken);
                await _bettingRepository.ProcessSettledBets();
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
