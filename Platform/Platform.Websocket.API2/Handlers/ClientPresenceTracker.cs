using System.Collections.Concurrent;

namespace Platform.Websocket.API2.Handlers
{
    // Tracks which client codes currently have at least one connected subscriber,
    // so the odds feeder (OE.Websocket.Service) can skip fan-out for clients with
    // no audience (GM-720 presence-aware publishing).
    //
    // A connection counts towards a client from its first successful Subscribe*
    // call until it disconnects. Unsubscribe calls deliberately do NOT remove
    // presence: over-reporting only costs unnecessary publishes, while
    // under-reporting would silently starve connected users.
    public class ClientPresenceTracker
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _clientsByConnection = new();
        private readonly ConcurrentDictionary<string, int> _connectionCounts = new();

        public void TrackSubscription(string connectionId, string clientCode)
        {
            if (string.IsNullOrEmpty(connectionId) || string.IsNullOrEmpty(clientCode))
                return;

            var codes = _clientsByConnection.GetOrAdd(connectionId, _ => new ConcurrentDictionary<string, byte>());

            if (codes.TryAdd(clientCode.ToLower(), 0))
                _connectionCounts.AddOrUpdate(clientCode.ToLower(), 1, (_, count) => count + 1);
        }

        public void TrackDisconnect(string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId))
                return;

            if (_clientsByConnection.TryRemove(connectionId, out var codes))
                foreach (var code in codes.Keys)
                    _connectionCounts.AddOrUpdate(code, 0, (_, count) => count > 0 ? count - 1 : 0);
        }

        public List<string> GetActiveClientCodes()
        {
            return _connectionCounts.Where(c => c.Value > 0).Select(c => c.Key).ToList();
        }
    }
}
