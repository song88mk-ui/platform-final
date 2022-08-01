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

        private Thread _threadBetAuditHK;
        private Thread _threadBetSelectionAuditHK;

        private Thread _threadBetAuditSA;
        private Thread _threadBetSelectionAuditSA;
        public Worker(ILogger<Worker> logger, ITableauRepository tableauRepository)
        {
            _logger = logger;
            _tableauRepository = tableauRepository;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _threadBetAuditHK = new Thread(ThreadBetAuditHK);
            _threadBetSelectionAuditHK = new Thread(ThreadBetSelectionAuditHK);

            _threadBetAuditSA = new Thread(ThreadBetAuditSA);
            _threadBetSelectionAuditSA = new Thread(ThreadBetSelectionAuditSA);

            _threadBetAuditHK.Start();
            _threadBetSelectionAuditHK.Start();

            _threadBetAuditSA.Start();
            _threadBetSelectionAuditSA.Start();
        }
        private void ThreadBetAuditHK(object obj)
        {
            while (true)
            {
                Console.WriteLine($"{DateTime.Now} : BEGIN HK ProcessBets");
                _tableauRepository.ProcessBetsHK();
                Console.WriteLine($"{DateTime.Now} : END HK ProcessBets");
            }
        }

        private void ThreadBetSelectionAuditHK(object obj)
        {
            while (true)
            {
                Console.WriteLine($"{DateTime.Now} : BEGIN HK ProcessBetSelections");
                _tableauRepository.ProcessBetSelectionsHK();
                Console.WriteLine($"{DateTime.Now} : END HK ProcessBetSelections");
            }
        }

        private void ThreadBetAuditSA(object obj)
        {
            while (true)
            {
                Console.WriteLine($"{DateTime.Now} : BEGIN SA ProcessBets");
                _tableauRepository.ProcessBetsSA();
                Console.WriteLine($"{DateTime.Now} : END SA ProcessBets");
            }
        }

        private void ThreadBetSelectionAuditSA(object obj)
        {
            while (true)
            {
                Console.WriteLine($"{DateTime.Now} : BEGIN SA ProcessBetSelections");
                _tableauRepository.ProcessBetSelectionsSA();
                Console.WriteLine($"{DateTime.Now} : END SA ProcessBetSelections");
            }
        }
    }
}
