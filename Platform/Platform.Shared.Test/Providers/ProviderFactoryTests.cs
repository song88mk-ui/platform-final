using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Providers;

namespace Platform.Shared.Test.Providers
{
    // Covers BetProviderFactory and LiveStreamFactory, the two resolvers that turn a bet's
    // ChannelType into the provider that handles it. CLAUDE.md makes them the extension point:
    // providers are added by registering an IBetProvider, never by an if at the call site, so
    // the resolution rule is the contract every future provider relies on.
    //
    // Both classes resolve by scanning the injected IEnumerable and throw NotSupportedException
    // when nothing matches. The throw matters as much as the match: a silent null would surface
    // as a NullReferenceException somewhere further down the placement path, with no clue which
    // channel was unhandled.
    public class ProviderFactoryTests
    {
        private sealed class StubBetProvider : IBetProvider
        {
            public StubBetProvider(ChannelType channelType) => ChannelType = channelType;

            public ChannelType ChannelType { get; }

            public Task<BetProviderResult> ValidateAsync(OnBetPlacement bet, CancellationToken cancellationToken) => throw new NotImplementedException();
            public Task<BetProviderResult> ValidateCashoutAsync(OnCashoutBetSettlement message, Bet bet, List<BetSelection> selections, bool isFullCashout, decimal cashoutPercent, decimal prevCashoutAmount) => throw new NotImplementedException();
            public void ProcessCancellation(Guid betId, string limitId, string clientCode, string correlationId, bool isBackOfficeTrigger = false) => throw new NotImplementedException();
            public Task ProcessSettlement(OnBetSettlement bet, string correlationId) => throw new NotImplementedException();
            public Task ProcessCashout(OnBetSettlement bet, CashoutType cashoutType, string correlationId) => throw new NotImplementedException();
            public Task<ProviderStakeResult> GetMaxStakeAsync(BetRequestDTO request) => throw new NotImplementedException();
            public Task<bool> IsAvailableAsync(BetRequestDTO request) => throw new NotImplementedException();
        }

        private sealed class StubLiveStreamProvider : ILiveStreamProvider
        {
            public StubLiveStreamProvider(ChannelType channelType) => ChannelType = channelType;

            public ChannelType ChannelType { get; }

            public Task<string> GetLiveStream(string fixtureId, string liveStream, string region, string device) => throw new NotImplementedException();
        }

        #region BetProviderFactory

        [Fact]
        public void BetProviderFactory_ResolvesTheProviderMatchingTheChannelType()
        {
            var bg = new StubBetProvider(ChannelType.BG);
            var od = new StubBetProvider(ChannelType.OD);

            var factory = new BetProviderFactory(new IBetProvider[] { bg, od });

            Assert.Same(bg, factory.Resolve((int)ChannelType.BG));
            Assert.Same(od, factory.Resolve((int)ChannelType.OD));
        }

        [Fact]
        public void BetProviderFactory_UnregisteredChannelType_Throws()
        {
            var factory = new BetProviderFactory(new IBetProvider[] { new StubBetProvider(ChannelType.BG) });

            Assert.Throws<NotSupportedException>(() => factory.Resolve((int)ChannelType.EM));
        }

        [Fact]
        public void BetProviderFactory_UnregisteredChannelType_NamesTheValueInTheMessage()
        {
            // Without the value the exception says nothing about which channel was unhandled.
            var factory = new BetProviderFactory(new IBetProvider[] { new StubBetProvider(ChannelType.BG) });

            var exception = Assert.Throws<NotSupportedException>(() => factory.Resolve(99));

            Assert.Contains("99", exception.Message);
        }

        [Fact]
        public void BetProviderFactory_NoProvidersRegistered_Throws()
        {
            var factory = new BetProviderFactory(Array.Empty<IBetProvider>());

            Assert.Throws<NotSupportedException>(() => factory.Resolve((int)ChannelType.BG));
        }

        [Fact]
        public void BetProviderFactory_DuplicateChannelType_ResolvesTheFirstRegistered()
        {
            // Registration order decides. Pinned so that adding a second provider for a channel
            // is a visible behaviour change rather than a coin toss.
            var first = new StubBetProvider(ChannelType.BG);
            var second = new StubBetProvider(ChannelType.BG);

            var factory = new BetProviderFactory(new IBetProvider[] { first, second });

            Assert.Same(first, factory.Resolve((int)ChannelType.BG));
        }

        [Fact]
        public void BetProviderFactory_ResolvesByNumericValueNotEnumIdentity()
        {
            // The signature takes an int, so a raw channel id from the database resolves without
            // being cast through the enum first.
            var bg = new StubBetProvider(ChannelType.BG);

            var factory = new BetProviderFactory(new IBetProvider[] { bg });

            Assert.Same(bg, factory.Resolve(0));
        }

        #endregion

        #region LiveStreamFactory

        [Fact]
        public void LiveStreamFactory_ResolvesTheProviderMatchingTheChannelType()
        {
            var bg = new StubLiveStreamProvider(ChannelType.BG);
            var sr = new StubLiveStreamProvider(ChannelType.SR);

            var factory = new LiveStreamFactory(new ILiveStreamProvider[] { bg, sr });

            Assert.Same(bg, factory.Resolve((int)ChannelType.BG));
            Assert.Same(sr, factory.Resolve((int)ChannelType.SR));
        }

        [Fact]
        public void LiveStreamFactory_UnregisteredChannelType_Throws()
        {
            var factory = new LiveStreamFactory(new ILiveStreamProvider[] { new StubLiveStreamProvider(ChannelType.BG) });

            var exception = Assert.Throws<NotSupportedException>(() => factory.Resolve((int)ChannelType.OD));

            Assert.Contains(((int)ChannelType.OD).ToString(), exception.Message);
        }

        [Fact]
        public void LiveStreamFactory_NoProvidersRegistered_Throws()
        {
            var factory = new LiveStreamFactory(Array.Empty<ILiveStreamProvider>());

            Assert.Throws<NotSupportedException>(() => factory.Resolve((int)ChannelType.BG));
        }

        #endregion
    }
}
