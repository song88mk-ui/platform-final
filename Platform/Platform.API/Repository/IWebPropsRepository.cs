using Platform.API.Models;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.API.Repository
{
    public interface IWebPropsRepository
    {
        Task<BaseResponseDTO<WebPropsConfigResponseDTO>> GetClientFeatureFlags(string clientCode);
    }
}
