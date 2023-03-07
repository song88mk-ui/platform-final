using Platform.AgentReports.API.Models;
using Platform.AgentReports.API.Models.Response;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.AgentReports.API.Repository
{
    public interface IReportsRepository
    {
        Task<BaseResponseDTO<OutstandingBetFixtureResponse>> GetOutstandingBetFixtures(long agentId, int page, int pageSize, CurrencyCode currency);
    }
}
