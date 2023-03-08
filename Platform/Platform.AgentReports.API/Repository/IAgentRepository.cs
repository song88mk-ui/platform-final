using Platform.AgentReports.API.Models.Response;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.AgentReports.API.Repository
{
    public interface IAgentRepository
    {
        Task<BaseResponseDTO<UserDetailsResponse>> GetUserDetails(long userId);
    }
}
