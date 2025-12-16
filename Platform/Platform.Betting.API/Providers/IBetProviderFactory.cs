using Platform.Shared.Enums;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Platform.Betting.API.Providers
{
    public class BetProviderFactory : IBetProviderFactory
    {
        private readonly IEnumerable<IBetProvider> _providers;

        public BetProviderFactory(IEnumerable<IBetProvider> providers)
        {
            _providers = providers;
        }

        public IBetProvider Resolve(int channelType)
        {
            var provider = _providers.FirstOrDefault(p => (int)p.ChannelType == channelType);

            if (provider == null)
                throw new NotSupportedException($"ChannelType {channelType} is not supported");

            return provider;
        }
    }
}
