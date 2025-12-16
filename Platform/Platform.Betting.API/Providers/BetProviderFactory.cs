namespace Platform.Betting.API.Providers
{
    public interface IBetProviderFactory
    {
        IBetProvider Resolve(int channelType);
    }
}
