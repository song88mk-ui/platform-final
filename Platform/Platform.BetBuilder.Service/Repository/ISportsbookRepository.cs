using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Platform.Shared.Data.Sportsbook;
using Platform.Shared.Data.Sportsbook.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Platform.BetBuilder.Service.Repository
{
    public interface ISportsbookRepository
    {

        Task<BaseResponseDTO<BetBuilderMarketSelectionResponseDTO>> GetBetBuilderSelectionsOdds();
    }
}
