using Platform.WebSocket.API2.Messages;

public interface IPlatformEvent
{
    Task OnOddsChange(OddsChangeFixture message);
    Task OnBetStopFixture(BetStopFixture message);
    Task OnBetStopGlobal(OutgoingBetStopGlobal message);
    Task OnBetslipOddsChange(OddsChangeFixture message);
    Task OnBetslipBetStopFixture(BetStopFixture message);
    Task OnTopMarketsOddsChange(OddsChangeFixture message);
    Task OnTopMarketsBetStopFixture(BetStopFixture message);
    Task OnBetStatus(OutgoingBetStatus message);
    Task OnCashOutChange(OutgoingCashOutChange message);
    Task OnWalletChange(OutgoingWalletChange message);
    Task OnPayableChange(OutgoingPayableChange message);
}
