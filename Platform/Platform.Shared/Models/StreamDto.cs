using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class StreamDto
    {
        public string Id { get; set; }

        // Pe = Permissions flattened as "Device:Region"
        public List<string> Pe { get; set; }

        public DeliveriesDto Ot { get; set; }
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
}
