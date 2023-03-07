using Platform.AgentReports.API.Models;
using Platform.AgentReports.API.Models.Response;
using Platform.Shared.Enums;
using System.Threading.Tasks;

namespace Platform.AgentReports.API.Repository
{
    public interface IReportsRepository
    {
        Task<BaseResponse<OutstandingBetFixtureResponse>> GetOutstandingBetFixtures(long agentId, long fixtureId, int marketTemplateId, int selectionTemplateId, int page, int pageSize, CurrencyCode currency);
    }
}
