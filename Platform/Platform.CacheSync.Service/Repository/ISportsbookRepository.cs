using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.CacheSync.Service.Repository
{
    public interface ISportsbookRepository
    {
        Task<BaseResponseDTO<bool>> DeleteCachePreload(CachePreloadRequestDTO request, string baseUri);
    }
}
