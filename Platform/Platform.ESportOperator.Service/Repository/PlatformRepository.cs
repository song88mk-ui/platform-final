using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Platform.Shared.Data.Platform;
using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.MessageModels;
using System;
using System.Text.Json;
using System.Transactions;

namespace Platform.ESportOperator.Service.Repository
{
    public class PlatformRepository : IPlatformRepository
    {
        private readonly IConfiguration _configuration;

        public PlatformRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ProcessESportDebitRequest(OnESportDebitRequest message, string correlationId)
        {
            AddESportOperatorLog(new ESportOperatorLog
            {
                Direction = "REQUEST",
                ClientId = message.ClientId,
                ClientCode = message.ClientCode,
                URL = message.Endpoint,
                BetId = message.BetId.ToString(),
                Info = JsonSerializer.Serialize(message.Payload),
                OperatorToken = message.OperatorToken,
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow
            });
        }

        public void ProcessESportDebitResponse(OnESportDebitResponse message, string correlationId)
        {
            AddESportOperatorLog(new ESportOperatorLog
            {
                Direction = "RESPONSE",
                ClientId = message.ClientId,
                ClientCode = message.ClientCode,
                URL = message.Endpoint,
                BetId = message.BetId.ToString(),
                Info = JsonSerializer.Serialize(message.Payload),
                OperatorToken = message.OperatorToken,
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow
            });
        }

        public void ProcessESportCreditRequest(OnESportCreditRequest message, string correlationId)
        {
            AddESportOperatorLog(new ESportOperatorLog
            {
                Direction = "REQUEST",
                ClientId = message.ClientId,
                ClientCode = message.ClientCode,
                URL = message.Endpoint,
                BetId = message.BetId.ToString(),
                Info = JsonSerializer.Serialize(message.Payload),
                OperatorToken = message.OperatorToken,
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow
            });
        }

        public void ProcessESportCreditResponse(OnESportCreditResponse message, string correlationId)
        {
            AddESportOperatorLog(new ESportOperatorLog
            {
                Direction = "RESPONSE",
                ClientId = message.ClientId,
                ClientCode = message.ClientCode,
                URL = message.Endpoint,
                BetId = message.BetId.ToString(),
                Info = JsonSerializer.Serialize(message.Payload),
                OperatorToken = message.OperatorToken,
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow
            });
        }

        #region Utilities

        private void AddESportOperatorLog(ESportOperatorLog entity)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(_configuration["ConnectionStrings:Platform"]);

                using var context = new PlatformContext(optionsBuilder.Options);
                try
                {
                    context.ESportOperatorLog.Add(entity);

                    context.SaveChanges();
                    scope.Complete();
                }
                catch (Exception)
                {
                }
            }
        }

        #endregion
    }
}
