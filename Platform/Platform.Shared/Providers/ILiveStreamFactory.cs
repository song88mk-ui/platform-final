namespace Platform.Shared.Providers
{
    public interface ILiveStreamFactory
    {
        ILiveStreamProvider Resolve(int channelType);
    }
}
