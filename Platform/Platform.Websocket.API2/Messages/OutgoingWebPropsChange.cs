namespace Platform.WebSocket.API2.Messages
{
    // Deliberately a bare signal, no flag values - platform-web re-fetches and compares on
    // receipt instead of applying pushed values, to avoid flicker from a burst of rapid saves.
    public class OutgoingWebPropsChange
    {
        public bool Ch { get; set; }
    }
}
