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

        public Worker(ILogger<Worker> logger, IBettingRepository bettingRepository)
        {
            _logger = logger;
            _bettingRepository = bettingRepository;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (true)
            {

                await Task.Delay(1000, stoppingToken);
                try
                {
                    _bettingRepository.PreloadSettledBets();
                    await _bettingRepository.ProcessSettledBets();

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.UtcNow} - Error : {ex.Message}");

                }
                await Task.Delay(3600000, stoppingToken);
            }
        }
    }
}
