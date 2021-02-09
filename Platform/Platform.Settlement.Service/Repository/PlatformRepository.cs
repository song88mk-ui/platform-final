using Microsoft.Extensions.Configuration;
using Platform.Placement.Service.Entities;
using Platform.Settlement.Service.Utility;
using System;

namespace Platform.Settlement.Service.Repository
{
    public class PlatformRepository : IPlatformRepository
    {
        private readonly IConfiguration _configuration;
        public PlatformRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ProcessSettlement(OnSettlementMessage settlement, IMessageBroker messageBroker)
        {
            throw new NotImplementedException();
        }
    }
}
