using System.Collections.Generic;
using System;

namespace Platform.Shared.Models.Provider.BetGenius
{
    public class BGEvent
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Phase { get; set; }
        public string BookingStatus { get; set; }
        public DateTime StartAt { get; set; }

        public string CompetitionId { get; set; }
        public string CompetitionName { get; set; }

        public int SportId { get; set; }
        public string SportName { get; set; }

        public List<LiveStreamDto> LiveStreams { get; set; }
        public List<VodStreamDto> VodStreams { get; set; }
    }

    public class LiveStreamDto
    {
        public string Id { get; set; }
        public List<PermissionDto> Permissions { get; set; }
        public List<string> Dmas { get; set; }
        public DeliveriesDto Deliveries { get; set; }
    }

    public class PermissionDto
    {
        public string Device { get; set; }
        public int MaxPlayerSizePercentage { get; set; }
        public string Region { get; set; }
    }

    public class DeliveriesDto
    {
        public List<DeliveryProviderDto> Hls { get; set; }
        public List<DeliveryProviderDto> Dash { get; set; }
    }

    public class DeliveryProviderDto
    {
        public string Id { get; set; }
    }

    public class VodStreamDto
    {
        // TODO: Add properties
    }
}
