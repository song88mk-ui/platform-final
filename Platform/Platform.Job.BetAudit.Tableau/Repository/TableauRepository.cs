using Microsoft.Extensions.DependencyInjection;
using Platform.Job.BetAudit.Tableau.Data;
using Platform.Job.BetAudit.Tableau.Data.Betting;
using System;
using System.Linq;
using System.Threading;

namespace Platform.Job.BetAudit.Tableau.Repository
{
    public class TableauRepository : ITableauRepository
    {
        private readonly IServiceProvider _serviceProvider;

        public TableauRepository(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void ProcessBets()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var tableauContext = scope.ServiceProvider.GetRequiredService<TableauContext>();
                var bettingContext = scope.ServiceProvider.GetRequiredService<BettingContext>();

                var lastRun = tableauContext.LastRunBetHK.Find(1);

                if (lastRun != null)
                {
                    var bets = bettingContext.BetAudit.Where(c => c.Id > lastRun.LastId).OrderBy(o => o.Id).Take(100).ToList();

                    if (bets.Count == 0)
                    {
                        Console.WriteLine($"{DateTime.Now} : SLEEPING ProcessBets for 5 seconds");
                        Thread.Sleep(5000);
                    }


                    foreach (var bet in bets)
                    {
                        var betSelectionCount = bettingContext.BetSelection.Where(c => c.BetId == bet.BetId).Count();

                        var isParlay = false;

                        if (betSelectionCount > 1)
                            isParlay = true;

                        var count = tableauContext.BetAuditHK.Where(c => c.BetId == bet.BetId).Count();

                        var betAudit = new Data.BetAuditHK
                        {
                            Id = bet.Id,
                            BetId = bet.BetId,
                            BetSequence = count + 1,
                            IsParlay = isParlay,
                            IsLastBet = true,
                            Timestamp = bet.Timestamp
                        };

                        lastRun.LastId = bet.Id;

                        var previousBet = tableauContext.BetAuditHK.Where(c => c.BetId == bet.BetId).OrderByDescending(o => o.Timestamp).Take(1).FirstOrDefault();

                        if (previousBet != null)
                        {
                            previousBet.IsLastBet = false;

                            tableauContext.BetAuditHK.Update(previousBet);
                        }

                        tableauContext.LastRunBetHK.Update(lastRun);
                        tableauContext.BetAuditHK.Add(betAudit);
                        tableauContext.SaveChanges();
                    }
                }
            }
            catch (Exception)
            {
                Thread.Sleep(5000);
            }
        }
        public void ProcessBetSelections()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var tableauContext = scope.ServiceProvider.GetRequiredService<TableauContext>();
                var bettingContext = scope.ServiceProvider.GetRequiredService<BettingContext>();

                var lastRun = tableauContext.LastRunBetSelectionHK.Find(1);

                if (lastRun != null)
                {
                    var betSelections = bettingContext.BetSelectionAudit.Where(c => c.Id > lastRun.LastId).OrderBy(o => o.Id).Take(100).ToList();

                    if (betSelections.Count == 0)
                    {
                        Console.WriteLine($"{DateTime.Now} : SLEEPING ProcessBetSelections for 5 seconds");
                        Thread.Sleep(5000);
                    }

                    foreach (var betSelection in betSelections)
                    {
                        var count = tableauContext.BetSelectionAuditHK.Where(c => c.BetSelectionId == betSelection.BetSelectionId).Count();

                        var betSelectionAudit = new Data.BetSelectionAuditHK
                        {
                            Id = betSelection.Id,
                            BetId = betSelection.BetId,
                            BetSelectionId = betSelection.BetSelectionId,
                            BetSelectionSequence = count + 1,
                            IsLastBetSelection = true,
                            Timestamp = betSelection.Timestamp
                        };

                        lastRun.LastId = betSelection.Id;

                        var previousBetSelection = tableauContext.BetSelectionAuditHK.Where(c => c.BetSelectionId == betSelection.BetSelectionId).OrderByDescending(o => o.Timestamp).Take(1).FirstOrDefault();

                        if (previousBetSelection != null)
                        {
                            previousBetSelection.IsLastBetSelection = false;

                            tableauContext.BetSelectionAuditHK.Update(previousBetSelection);
                        }

                        tableauContext.LastRunBetSelectionHK.Update(lastRun);
                        tableauContext.BetSelectionAuditHK.Add(betSelectionAudit);
                        tableauContext.SaveChanges();
                    }
                }
            }
            catch (Exception)
            {
                Thread.Sleep(5000);
            }
        }
    }
}
