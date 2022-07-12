using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ClientFixtureSortOrderResponseDTO
    {
        public ClientFixtureSortOrderResponseDTO()
        {
            Fixtures = new List<ClientFixtureSortOrderDTO>();
        }
        public List<ClientFixtureSortOrderDTO> Fixtures { get; set; }
    }
    public class ClientFixtureSortOrderDTO
    {
        public int SportId { get; set; }
        public int SegmentId { get; set; }
        public int LeagueId { get; set; }
        public int SortOrder { get; set; }
    }
}
