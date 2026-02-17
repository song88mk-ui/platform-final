using Platform.Shared.Enums;
using System.Threading.Tasks;

namespace Platform.Shared.Providers
{
    public interface ILiveStreamProvider
    {
        ChannelType ChannelType { get; }

        Task<string> GetLiveStream(string fixtureId, string liveStream, string region, string device);
    }
}
