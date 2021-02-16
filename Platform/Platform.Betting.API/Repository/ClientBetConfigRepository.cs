using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Data.Platform;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace Platform.Betting.API.Repository
{
    public class ClientBetConfigRepository : IClientBetConfigRepository
    {
        private readonly PlatformContext _platformContext;
        private readonly IMessageBroker _messageBroker;
        public ClientBetConfigRepository(PlatformContext platformContext, IMessageBroker messageBroker)
        {
            _platformContext = platformContext;
            _messageBroker = messageBroker;
            _messageBroker.Connect();
        }

        public Task<IActionResult> ProcessConfig(BackOfficeBetConfigDTO request)
        {
            throw new NotImplementedException();
        }
    }
}
 
