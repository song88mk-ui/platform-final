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
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _provider;
        private readonly IMessageBroker _messageBroker;
        private readonly IPlatformRepository _platformRepository;
        private readonly IConfiguration _configuration;
        private Stack<int> _fibonnaci = new Stack<int>();

        public Worker(ILogger<Worker> logger, IServiceProvider provider, IMessageBroker messageBroker, IPlatformRepository platformRepository, IConfiguration configuration)
        {
            _logger = logger;
            _provider = provider;
            _messageBroker = messageBroker;
            _platformRepository = platformRepository;
            _configuration = configuration;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _messageBroker.Dispose();

            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _messageBroker.Connect();

            while (true)
            {
                
                try
                {
                    var waitTime = GetNextRetry(false);

                    Console.WriteLine($"{DateTime.UtcNow} - Current Config - {waitTime} minutes");

                    await Task.Delay(waitTime * 1000 * 60);

                    Console.WriteLine($"{DateTime.UtcNow} - Start");

                    using var scope = _provider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<PlatformContext>();

                    var bets = context.OperatorSettlementRetry.Where(c => c.IsRetry == true).AsNoTracking().ToList();

                    if (bets.Count > 0)
                        _platformRepository.ProcessBets(bets, _messageBroker);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.UtcNow} - ERROR - {ex.Message}");
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
