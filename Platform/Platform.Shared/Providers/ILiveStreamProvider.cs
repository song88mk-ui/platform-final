using Platform.Shared.Enums;
using System.Threading.Tasks;

namespace Platform.Shared.Providers
{
    public interface ILiveStreamProvider
    {
        ChannelType ChannelType { get; }

        Task<bool> ValidateLiveStream(string fixtureId, string countryCode, string device);
    }
}
