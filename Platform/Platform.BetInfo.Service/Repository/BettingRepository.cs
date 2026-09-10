using System.Transactions;
using System;
using Platform.Shared.MessageModels;
using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.Betting;
using System.Linq;

namespace Platform.BetInfo.Service.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private readonly IDbContextFactory<BettingContext> _contextFactory;

        public BettingRepository(IDbContextFactory<BettingContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public void ProcessBetPlacement(OnBetPlacement message)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                // CreateDbContext returns a fresh context per call, matching the lifetime the
                // previous inline construction gave: the context is created and disposed inside
                // the ambient TransactionScope, never shared across calls. This repository is
                // registered as a singleton (Program.cs), and the factory is a singleton too,
                // so no scoped DbContext is captured by a longer-lived object.
                using (var context = _contextFactory.CreateDbContext())
                {
                    context.BetInfo.Add(new Shared.Data.Betting.Entities.BetInfo
                    {
                        BetId = message.Bet.BetId,
                        UserAgent = message.UserAgent,
                    });

                    context.SaveChanges();
                    scope.Complete();
                }
            }
        }
    }
}
