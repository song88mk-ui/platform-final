using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class OrderedSportsResponseDTO
    {
        public OrderedSportsResponseDTO()
        {
            Sports = new List<OrderedSport>();
        }
        public List<OrderedSport> Sports { get; set; }
    }

    public class OrderedSport
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
    }
}
