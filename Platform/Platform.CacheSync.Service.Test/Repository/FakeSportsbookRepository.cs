using Platform.CacheSync.Service.Repository;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Platform.CacheSync.Service.Test.Repository
{
    // Records every purge call and returns a configurable status so a test can exercise both the
    // "purge succeeded, relay the message" and the "purge failed, stay silent" branches.
    public class FakeSportsbookRepository : ISportsbookRepository
    {
        public List<CachePreloadRequestDTO> Requests { get; } = new List<CachePreloadRequestDTO>();

        public List<string> BaseUris { get; } = new List<string>();

        public StatusCode StatusToReturn { get; set; } = StatusCode.Success;

        public Task<BaseResponseDTO<bool>> DeleteCachePreload(CachePreloadRequestDTO request, string baseUri, Dictionary<string, string> header = null)
        {
            Requests.Add(request);
            BaseUris.Add(baseUri);

            return Task.FromResult(new BaseResponseDTO<bool>
            {
                StatusCode = StatusToReturn,
                Result = StatusToReturn == StatusCode.Success
            });
        }
    }
}
