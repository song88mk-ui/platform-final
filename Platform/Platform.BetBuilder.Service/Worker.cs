using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.BetBuilder.Service.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.BetBuilder.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ISportsbookRepository _repository;

        public Worker(ILogger<Worker> logger, ISportsbookRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _repository.GetBetBuilderSelectionsOdds(1); //soccer
            await _repository.GetBetBuilderSelectionsOdds(2); //basketball
        }
    }
}
