using Platform.AgentReports.API.Models.Response;
using Platform.Shared.Models;

namespace Platform.AgentReports.API.Repository
{
    public interface IAgentRepository
    {
        BaseResponseDTO<UserDetailsResponse> GetUserDetails(long userId);
    }
}
