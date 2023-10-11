
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;

        public BettingRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ProcessBetPlacement(OnBetPlacement message)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                using (var context = new BettingContext(optionsBuilder.Options))
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
