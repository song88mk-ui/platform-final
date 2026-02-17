using System.Collections.Generic;
using System;
using System.Linq;

namespace Platform.Shared.Providers
{
    public class LiveStreamFactory : ILiveStreamFactory
    {
        private readonly IEnumerable<ILiveStreamProvider> _providers;

        public LiveStreamFactory(IEnumerable<ILiveStreamProvider> providers)
        {
            _providers = providers;
        }

        public ILiveStreamProvider Resolve(int channelType)
        {
            var provider = _providers.FirstOrDefault(p => (int)p.ChannelType == channelType);

            if (provider == null)
                throw new NotSupportedException($"ChannelType {channelType} is not supported");

            return provider;
        }
    }
}
