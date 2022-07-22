using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Job.BetAudit.Tableau.Repository;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.Job.BetAudit.Tableau
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ITableauRepository _tableauRepository;

        private Thread _threadBetAudit;
        private Thread _threadBetSelectionAudit;

        public Worker(ILogger<Worker> logger, ITableauRepository tableauRepository)
        {
            _logger = logger;
            _tableauRepository = tableauRepository;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _threadBetAudit = new Thread(ThreadBetAudit);
            _threadBetSelectionAudit = new Thread(ThreadBetSelectionAudit);

            _threadBetAudit.Start();
            _threadBetSelectionAudit.Start();
        }
        private void ThreadBetAudit(object obj)
        {
            while (true)
            {
                Console.WriteLine($"{DateTime.Now} : BEGIN ProcessBets");
                _tableauRepository.ProcessBets();
                Console.WriteLine($"{DateTime.Now} : END ProcessBets");
            }
        }

        private void ThreadBetSelectionAudit(object obj)
        {
            while (true)
            {
                Console.WriteLine($"{DateTime.Now} : BEGIN ProcessBetSelections");
                _tableauRepository.ProcessBetSelections();
                Console.WriteLine($"{DateTime.Now} : END ProcessBetSelections");
            }
        }
    }
}
