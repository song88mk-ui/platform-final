namespace Platform.Shared.Providers
{
    public interface IBetProviderFactory
    {
        IBetProvider Resolve(int channelType);
    }
}
