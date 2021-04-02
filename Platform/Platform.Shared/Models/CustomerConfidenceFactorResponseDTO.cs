using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class CustomerConfidenceFactorResponseDTO
    {
        public CustomerConfidenceFactorResponseDTO()
        {
            Values = new List<CustomerConfidenceFactor>();
        }
        public List<CustomerConfidenceFactor> Values { get; set; }
    }

    public class CustomerConfidenceFactor
    {
        public double Timestamp { get; set; }
        public double CurrentCCF { get; set; }
        public double PreviousCCF { get; set; }
        public bool IsSpecificSport { get; set; }
        public string SportId { get; set; }
        public string SportName { get; set; }
        public string AppliedTo { get; set; }
    }
}
