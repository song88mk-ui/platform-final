using Platform.API.Models;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Platform.API.Repository
{
    public interface IPlatformRepository
    {
        Task<BaseResponseDTO<WalletBalanceResponseDTO>> GetWalletBalance(string clientId, string operatorToken);
        Task<BaseResponseDTO<BetResponseDTO>> PlaceBet(string apiKey, string clientId, string operatorToken, BetRequest request);
        Task<BaseResponseDTO<MaxStakeResponseDTO>> GetMaxStake(string apiKey, string clientId, string operatorToken, BetRequest request);
        Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistory(string apiKey, string clientId, string langCode, string operatorToken, BetHistoryRequest request);
        Task<BaseResponseDTO<BetConfigFEResponseDTO>> GetBetConfig(string apiKey, string clientId, BetConfigRequest request);
        Task<BaseResponseDTO<ClientResponseDTO>> AddOrUpdateClient(ClientRequestDTO request);
        Task<BaseResponseDTO<bool>> ProcessStatusUpdate(ClientStatusRequestDTO request);
        Task<BaseResponseDTO<LivestreamResponseDTO>> GetLivestream(string apiKey, string clientId, string operatorToken, string IPCountryCode, LivestreamRequest request);
        Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetTopLeagues(string apiKey, string clientId, string langCode, TopLeaguesRequest request);
        Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetAllLeagues(string apiKey, string clientId, string langCode, TopLeaguesRequest request);
        Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetAllLeagues2(string apiKey, string clientId, TopLeaguesRequest2 request);
        Task<BaseResponseDTO<TopFixturesIdsResponseDTO>> GetTopFixtureIds(string apiKey, string clientId, string langCode);
        Task<BaseResponseDTO<MarketTemplateGroupResponseDTO>> GetMarketTemplateGroups(string apiKey, string clientId);
        Task<BaseResponseDTO<List<SportSelectorMarketTemplateDTO>>> GetSelectorMarketTemplates(string apiKey, string clientId);
        Task<BaseResponseDTO<TWResponseDTO>> GetUserFavorites2(string apiKey, string clientId, string langCode, string operatorToken, string ipCountryCode);
        Task<BaseResponseDTO<UserFavoriteResponseDTO>> GetUserFavorites(string apiKey, string clientId, string langCode, string operatorToken, string ipCountryCode);
        Task<BaseResponseDTO<UserFavoriteResponseDTO>> AddUserFavorites(string apiKey, string clientId, string langCode, string operatorToken, InsertDeleteUserFavoritesRequest request);
        Task<BaseResponseDTO<UserFavoriteResponseDTO>> DeleteUserFavorites(string apiKey, string clientId, string langCode, string operatorToken, InsertDeleteUserFavoritesRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetTopFixtures(string apiKey, string clientId, string langCode, string ipCountryCode);
        Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetLiveFixtureCount(string apiKey, string clientId, string langCode);
        Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetTodayFixtureCount(string apiKey, string clientId, string langCode, TodayFixtureCountRequest request);
        Task<BaseResponseDTO<TWResponseDTO>> GetLiveFixtures(string apiKey, string clientId, string langCode, FixtureRequest request, string ipCountryCode);
        Task<BaseResponseDTO<TWResponseDTO>> GetLiveFixturesByLeague(string apiKey, string clientId, string langCode, FixtureRequest request, string ipCountryCode);
        Task<BaseResponseDTO<TWResponseDTO>> GetTodayFixtures(string apiKey, string clientId, string langCode, TodayFixtureRequest request, string ipCountryCode);
        Task<BaseResponseDTO<TWResponseDTO>> GetTodayFixturesByLeague(string apiKey, string clientId, string langCode, TodayFixtureRequest request, string ipCountryCode);
        Task<BaseResponseDTO<TWResponseDTO>> GetUpcomingFixtures(string apiKey, string clientId, string langCode, UpcomingFixtureRequest request, string ipCountryCode);
        Task<BaseResponseDTO<TWResponseDTO>> GetDailyFixtures(string apiKey, string clientId, string langCode, DailyFixtureRequest request, string ipCountryCode);
        Task<BaseResponseDTO<TWResponseDTO>> GetDailyFixturesByLeague(string apiKey, string clientId, string langCode, DailyFixtureRequest request, string ipCountryCode);
        Task<BaseResponseDTO<TWResponseDTO>> GetTopLeagueFixtures(string apiKey, string clientId, string langCode, TopLeagueFixtureRequest request, string ipCountryCode);
        Task<BaseResponseDTO<TWResponseDTO>> GetSegmentFixtures(string apiKey, string clientId, string langCode, SegmentFixtureRequest request, string ipCountryCode);
        Task<BaseResponseDTO<TWResponseDTO>> GetFixtures(string apiKey, string clientId, string langCode, LeagueFixtureRequest request, string ipCountryCode);
        Task<BaseResponseDTO<SportsResponseDTO>> GetSports(string apiKey, string clientId, string langCode, SportGroup sportGroup);
        Task<BaseResponseDTO<TWResponseDTO>> GetFixtureDetails(string apiKey, string clientId, string langCode, FixtureDetailsRequest request, string ipCountryCode);
        Task<BaseResponseDTO<TWResponseDTO>> GetSelectionDetails(string apiKey, string clientId, string langCode, FixtureMarketSelectionDetailsRequest request, string ipCountryCode);
        Task<BaseResponseDTO<TWResponseDTO>> GetOutrightFixtures(string apiKey, string clientId, string langCode, OutrightFixtureRequest request, string ipCountryCode);
        Task<BaseResponseDTO<TWResponseDTO>> GetOutrightFixturesByLeague(string apiKey, string clientId, string langCode, OutrightFixtureRequest request, string ipCountryCode);
        Task<BaseResponseDTO<TWSearchResponseDTO>> GetSearchResults(string apiKey, string clientId, string langCode, SearchResultRequest request);
        Task<BaseResponseDTO<ClientTopFixtureResponseDTO>> GetClientTopFixtures(string clientRefIds);
        Task<BaseResponseDTO<ClientTopFixtureResponseDTO>> GetClientTopFixtures2(string clientCodes);
        Task<BaseResponseDTO<BackofficeSportsResponseDTO>> GetSportstreeDetails(int clientRefId);
        Task<BaseResponseDTO<ClientTopFixtureMessageResponseDTO>> PostClientTopFixtures(ClientTopFixtureRequest request);
        Task<BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>> AddOrUpdateTopLeagues(BackOfficeClientTopLeague request);
        Task<BaseResponseDTO<BackOfficeTopLeaguesResponseDTO>> GetClientTopLeagues(BackOfficeTopLeaguesRequestDTO request);
        Task<BaseResponseDTO<SportsLookupResponseDTO>> GetClientSportsLookup(BackofficeClientSportsRequestDTO request);
        Task<BaseResponseDTO<BetCashOutResponseDTO>> CashoutBet(string apiKey, string clientId, string operatorToken, CashoutBetRequest request);
        Task<BaseResponseDTO<ExchangeRateResponseDTO>> GetCurrentExchangeRate(string currencyCode);
        Task<BaseResponseDTO<StatusCode>> ProcessBetConfig(BackOfficeBetConfigPayloadDTO request);
        Task<BaseResponseDTO<BetModifyResponseDTO>> CancelBets(BetModifyRequestDTO request);
        Task<BaseResponseDTO<BetModifyResponseDTO>> ManualSettlement(ManualBetSettlementRequest request);
        Task<BaseResponseDTO<ParlayBonusResponseDTO>> GetClientParlayBonus(GetParlayBonusRequestDTO request);
        Task<BaseResponseDTO<StatusCode>> AddOrUpdateParlayBonus(ParlayBonusRequestDTO request);
        Task<BaseResponseDTO<SportGroupResponseDTO>> GetSportGroups();
        Task<BaseResponseDTO<BoostedResponseDTO>> GetBoostedFixtures(string apiKey, string clientId, string langCode, BoostedFixturesRequestDTO request);

    }
}
