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

        public void ProcessBetsHKFirst()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var tableauContext = scope.ServiceProvider.GetRequiredService<TableauContext>();
                var bettingContext = scope.ServiceProvider.GetRequiredService<BettingContextHK>();

                var lastRun = tableauContext.LastRunBetHKFirst.Find(1);

                if (lastRun != null)
                {
                    var bets = bettingContext.BetAudit_bak2.Where(c => c.Id > lastRun.LastId).OrderBy(o => o.Id).Take(1000).ToList();

                    if (bets.Count == 0)
                    {
                        Console.WriteLine($"{DateTime.Now} : SLEEPING ProcessBets for 10 seconds");
                        Thread.Sleep(10000);
                        return;
                    }

                    foreach (var bet in bets)
                    {
                        using var transaction = tableauContext.Database.BeginTransaction();

                        var betSelectionCount = bettingContext.BetSelection.Where(c => c.BetId == bet.BetId).Count();

                        var isParlay = false;

                        if (betSelectionCount > 1)
                            isParlay = true;

                        var count = tableauContext.BetAuditHK.Where(c => c.BetId == bet.BetId).Count();

                        var previousBet = tableauContext.BetAuditHK.Where(c => c.BetId == bet.BetId).OrderByDescending(o => o.Id).Take(1).FirstOrDefault();

                        BetAuditHK betAudit;

                        if (previousBet != null)
                        {
                            previousBet.IsLastBet = false;

                            tableauContext.BetAuditHK.Update(previousBet);

                            betAudit = new BetAuditHK
                            {
                                RefId = bet.Id,
                                BetId = bet.BetId,
                                BetSequence = count + 1,
                                IsParlay = isParlay,
                                IsLastBet = true,
                                BetStatusId = bet.BetStatusId,
                                Timestamp = bet.Timestamp,
                                PreviousBetStatusId = previousBet.BetStatusId,
                                PreviousTimestamp = previousBet.Timestamp
                            };
                        }
                        else
                        {
                            betAudit = new BetAuditHK
                            {
                                RefId = bet.Id,
                                BetId = bet.BetId,
                                BetSequence = count + 1,
                                IsParlay = isParlay,
                                IsLastBet = true,
                                BetStatusId = bet.BetStatusId,
                                Timestamp = bet.Timestamp,
                                PreviousBetStatusId = null,
                                PreviousTimestamp = null
                            };
                        }

                        lastRun.LastId = bet.Id;

                        tableauContext.BetAuditHK.Add(betAudit);
                        tableauContext.LastRunBetHKFirst.Update(lastRun);
                        tableauContext.SaveChanges();

                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} : {ex.Message}");
                Thread.Sleep(10000);
            }
        }
        public void ProcessBetSelectionsHKFirst()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var tableauContext = scope.ServiceProvider.GetRequiredService<TableauContext>();
                var bettingContext = scope.ServiceProvider.GetRequiredService<BettingContextHK>();

                var lastRun = tableauContext.LastRunBetSelectionHKFirst.Find(1);

                if (lastRun != null)
                {
                    var betSelections = bettingContext.BetSelectionAudit_bak2.Where(c => c.Id > lastRun.LastId).OrderBy(o => o.Id).Take(1000).ToList();

                    if (betSelections.Count == 0)
                    {
                        Console.WriteLine($"{DateTime.Now} : SLEEPING ProcessBetSelections for 10 seconds");
                        Thread.Sleep(10000);
                        return;
                    }

                    foreach (var betSelection in betSelections)
                    {
                        using var transaction = tableauContext.Database.BeginTransaction();

                        var count = tableauContext.BetSelectionAuditHK.Where(c => c.BetSelectionId == betSelection.BetSelectionId).Count();

                        var previousBetSelection = tableauContext.BetSelectionAuditHK.Where(c => c.BetSelectionId == betSelection.BetSelectionId).OrderByDescending(o => o.Id).Take(1).FirstOrDefault();

                        BetSelectionAuditHK betSelectionAudit;

                        if (previousBetSelection != null)
                        {
                            previousBetSelection.IsLastBetSelection = false;

                            tableauContext.BetSelectionAuditHK.Update(previousBetSelection);

                            betSelectionAudit = new BetSelectionAuditHK
                            {
                                RefId = betSelection.Id,
                                BetId = betSelection.BetId,
                                BetSelectionId = betSelection.BetSelectionId,
                                BetSelectionSequence = count + 1,
                                IsLastBetSelection = true,
                                BetSelectionStatusId = betSelection.BetSelectionStatusId,
                                Timestamp = betSelection.Timestamp,
                                PreviousBetSelectionStatusId = previousBetSelection.BetSelectionStatusId,
                                PreviousTimestamp = previousBetSelection.Timestamp
                            };
                        }
                        else
                        {
                            betSelectionAudit = new BetSelectionAuditHK
                            {
                                RefId = betSelection.Id,
                                BetId = betSelection.BetId,
                                BetSelectionId = betSelection.BetSelectionId,
                                BetSelectionSequence = count + 1,
                                IsLastBetSelection = true,
                                BetSelectionStatusId = betSelection.BetSelectionStatusId,
                                Timestamp = betSelection.Timestamp,
                                PreviousBetSelectionStatusId = null,
                                PreviousTimestamp = null
                            };
                        }

                        lastRun.LastId = betSelection.Id;

                        tableauContext.BetSelectionAuditHK.Add(betSelectionAudit);
                        tableauContext.LastRunBetSelectionHKFirst.Update(lastRun);
                        tableauContext.SaveChanges();

                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} : {ex.Message}");
                Thread.Sleep(10000);
            }
        }


        public void ProcessBetsHK()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var tableauContext = scope.ServiceProvider.GetRequiredService<TableauContext>();
                var bettingContext = scope.ServiceProvider.GetRequiredService<BettingContextHK>();

                var lastRun = tableauContext.LastRunBetHK.Find(1);

                if (lastRun != null)
                {
                    var bets = bettingContext.BetAudit.Where(c => c.Id > lastRun.LastId).OrderBy(o => o.Id).Take(1000).ToList();

                    if (bets.Count == 0)
                    {
                        Console.WriteLine($"{DateTime.Now} : SLEEPING ProcessBets for 10 seconds");
                        Thread.Sleep(10000);
                        return;
                    }

                    foreach (var bet in bets)
                    {
                        using var transaction = tableauContext.Database.BeginTransaction();

                        var betSelectionCount = bettingContext.BetSelection.Where(c => c.BetId == bet.BetId).Count();

                        var isParlay = false;

                        if (betSelectionCount > 1)
                            isParlay = true;

                        var count = tableauContext.BetAuditHK.Where(c => c.BetId == bet.BetId).Count();

                        var previousBet = tableauContext.BetAuditHK.Where(c => c.BetId == bet.BetId).OrderByDescending(o => o.Id).Take(1).FirstOrDefault();

                        BetAuditHK betAudit;

                        if (previousBet != null)
                        {
                            previousBet.IsLastBet = false;

                            tableauContext.BetAuditHK.Update(previousBet);

                            betAudit = new BetAuditHK
                            {
                                RefId = bet.Id,
                                BetId = bet.BetId,
                                BetSequence = count + 1,
                                IsParlay = isParlay,
                                IsLastBet = true,
                                BetStatusId = bet.BetStatusId,
                                Timestamp = bet.Timestamp,
                                PreviousBetStatusId = previousBet.BetStatusId,
                                PreviousTimestamp = previousBet.Timestamp
                            };
                        }
                        else
                        {
                            betAudit = new BetAuditHK
                            {
                                RefId = bet.Id,
                                BetId = bet.BetId,
                                BetSequence = count + 1,
                                IsParlay = isParlay,
                                IsLastBet = true,
                                BetStatusId = bet.BetStatusId,
                                Timestamp = bet.Timestamp,
                                PreviousBetStatusId = null,
                                PreviousTimestamp = null
                            };
                        }

                        lastRun.LastId = bet.Id;

                        tableauContext.BetAuditHK.Add(betAudit);
                        tableauContext.LastRunBetHK.Update(lastRun);
                        tableauContext.SaveChanges();

                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} : {ex.Message}");
                Thread.Sleep(10000);
            }
        }
        public void ProcessBetSelectionsHK()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var tableauContext = scope.ServiceProvider.GetRequiredService<TableauContext>();
                var bettingContext = scope.ServiceProvider.GetRequiredService<BettingContextHK>();

                var lastRun = tableauContext.LastRunBetSelectionHK.Find(1);

                if (lastRun != null)
                {
                    var betSelections = bettingContext.BetSelectionAudit.Where(c => c.Id > lastRun.LastId).OrderBy(o => o.Id).Take(1000).ToList();

                    if (betSelections.Count == 0)
                    {
                        Console.WriteLine($"{DateTime.Now} : SLEEPING ProcessBetSelections for 10 seconds");
                        Thread.Sleep(10000);
                        return;
                    }

                    foreach (var betSelection in betSelections)
                    {
                        using var transaction = tableauContext.Database.BeginTransaction();

                        var count = tableauContext.BetSelectionAuditHK.Where(c => c.BetSelectionId == betSelection.BetSelectionId).Count();

                        var previousBetSelection = tableauContext.BetSelectionAuditHK.Where(c => c.BetSelectionId == betSelection.BetSelectionId).OrderByDescending(o => o.Id).Take(1).FirstOrDefault();

                        BetSelectionAuditHK betSelectionAudit;

                        if (previousBetSelection != null)
                        {
                            previousBetSelection.IsLastBetSelection = false;

                            tableauContext.BetSelectionAuditHK.Update(previousBetSelection);

                            betSelectionAudit = new BetSelectionAuditHK
                            {
                                RefId = betSelection.Id,
                                BetId = betSelection.BetId,
                                BetSelectionId = betSelection.BetSelectionId,
                                BetSelectionSequence = count + 1,
                                IsLastBetSelection = true,
                                BetSelectionStatusId = betSelection.BetSelectionStatusId,
                                Timestamp = betSelection.Timestamp,
                                PreviousBetSelectionStatusId = previousBetSelection.BetSelectionStatusId,
                                PreviousTimestamp = previousBetSelection.Timestamp
                            };
                        }
                        else
                        {
                            betSelectionAudit = new BetSelectionAuditHK
                            {
                                RefId = betSelection.Id,
                                BetId = betSelection.BetId,
                                BetSelectionId = betSelection.BetSelectionId,
                                BetSelectionSequence = count + 1,
                                IsLastBetSelection = true,
                                BetSelectionStatusId = betSelection.BetSelectionStatusId,
                                Timestamp = betSelection.Timestamp,
                                PreviousBetSelectionStatusId = null,
                                PreviousTimestamp = null
                            };
                        }

                        lastRun.LastId = betSelection.Id;

                        tableauContext.BetSelectionAuditHK.Add(betSelectionAudit);
                        tableauContext.LastRunBetSelectionHK.Update(lastRun);
                        tableauContext.SaveChanges();

                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} : {ex.Message}");
                Thread.Sleep(10000);
            }
        }


        public void ProcessBetsSA()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var tableauContext = scope.ServiceProvider.GetRequiredService<TableauContext>();
                var bettingContext = scope.ServiceProvider.GetRequiredService<BettingContextSA>();

                var lastRun = tableauContext.LastRunBetSA.Find(1);

                if (lastRun != null)
                {
                    var bets = bettingContext.BetAudit.Where(c => c.Id > lastRun.LastId).OrderBy(o => o.Id).Take(1000).ToList();

                    if (bets.Count == 0)
                    {
                        Console.WriteLine($"{DateTime.Now} : SLEEPING ProcessBets for 10 seconds");
                        Thread.Sleep(10000);
                        return;
                    }

                    foreach (var bet in bets)
                    {
                        using var transaction = tableauContext.Database.BeginTransaction();

                        var betSelectionCount = bettingContext.BetSelection.Where(c => c.BetId == bet.BetId).Count();

                        var isParlay = false;

                        if (betSelectionCount > 1)
                            isParlay = true;

                        var count = tableauContext.BetAuditSA.Where(c => c.BetId == bet.BetId).Count();

                        var previousBet = tableauContext.BetAuditSA.Where(c => c.BetId == bet.BetId).OrderByDescending(o => o.Id).Take(1).FirstOrDefault();

                        BetAuditSA betAudit;

                        if (previousBet != null)
                        {
                            previousBet.IsLastBet = false;

                            tableauContext.BetAuditSA.Update(previousBet);

                            betAudit = new BetAuditSA
                            {
                                RefId = bet.Id,
                                BetId = bet.BetId,
                                BetSequence = count + 1,
                                IsParlay = isParlay,
                                IsLastBet = true,
                                BetStatusId = bet.BetStatusId,
                                Timestamp = bet.Timestamp,
                                PreviousBetStatusId = previousBet.BetStatusId,
                                PreviousTimestamp = previousBet.Timestamp
                            };
                        }
                        else
                        {
                            betAudit = new BetAuditSA
                            {
                                RefId = bet.Id,
                                BetId = bet.BetId,
                                BetSequence = count + 1,
                                IsParlay = isParlay,
                                IsLastBet = true,
                                BetStatusId = bet.BetStatusId,
                                Timestamp = bet.Timestamp,
                                PreviousBetStatusId = null,
                                PreviousTimestamp = null
                            };
                        }

                        lastRun.LastId = bet.Id;

                        tableauContext.BetAuditSA.Add(betAudit);
                        tableauContext.LastRunBetSA.Update(lastRun);
                        tableauContext.SaveChanges();

                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} : {ex.Message}");
                Thread.Sleep(10000);
            }
        }
        public void ProcessBetSelectionsSA()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var tableauContext = scope.ServiceProvider.GetRequiredService<TableauContext>();
                var bettingContext = scope.ServiceProvider.GetRequiredService<BettingContextSA>();

                var lastRun = tableauContext.LastRunBetSelectionSA.Find(1);

                if (lastRun != null)
                {
                    var betSelections = bettingContext.BetSelectionAudit.Where(c => c.Id > lastRun.LastId).OrderBy(o => o.Id).Take(1000).ToList();

                    if (betSelections.Count == 0)
                    {
                        Console.WriteLine($"{DateTime.Now} : SLEEPING ProcessBetSelections for 10 seconds");
                        Thread.Sleep(10000);
                        return;
                    }

                    foreach (var betSelection in betSelections)
                    {
                        using var transaction = tableauContext.Database.BeginTransaction();

                        var count = tableauContext.BetSelectionAuditSA.Where(c => c.BetSelectionId == betSelection.BetSelectionId).Count();

                        var previousBetSelection = tableauContext.BetSelectionAuditSA.Where(c => c.BetSelectionId == betSelection.BetSelectionId).OrderByDescending(o => o.Id).Take(1).FirstOrDefault();

                        BetSelectionAuditSA betSelectionAudit;

                        if (previousBetSelection != null)
                        {
                            previousBetSelection.IsLastBetSelection = false;

                            tableauContext.BetSelectionAuditSA.Update(previousBetSelection);

                            betSelectionAudit = new BetSelectionAuditSA
                            {
                                RefId = betSelection.Id,
                                BetId = betSelection.BetId,
                                BetSelectionId = betSelection.BetSelectionId,
                                BetSelectionSequence = count + 1,
                                IsLastBetSelection = true,
                                BetSelectionStatusId = betSelection.BetSelectionStatusId,
                                Timestamp = betSelection.Timestamp,
                                PreviousBetSelectionStatusId = previousBetSelection.BetSelectionStatusId,
                                PreviousTimestamp = previousBetSelection.Timestamp
                            };
                        }
                        else
                        {
                            betSelectionAudit = new BetSelectionAuditSA
                            {
                                RefId = betSelection.Id,
                                BetId = betSelection.BetId,
                                BetSelectionId = betSelection.BetSelectionId,
                                BetSelectionSequence = count + 1,
                                IsLastBetSelection = true,
                                BetSelectionStatusId = betSelection.BetSelectionStatusId,
                                Timestamp = betSelection.Timestamp,
                                PreviousBetSelectionStatusId = null,
                                PreviousTimestamp = null
                            };
                        }

                        lastRun.LastId = betSelection.Id;

                        tableauContext.BetSelectionAuditSA.Add(betSelectionAudit);
                        tableauContext.LastRunBetSelectionSA.Update(lastRun);
                        tableauContext.SaveChanges();

                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} : {ex.Message}");
                Thread.Sleep(10000);
            }
        }
    }
}
