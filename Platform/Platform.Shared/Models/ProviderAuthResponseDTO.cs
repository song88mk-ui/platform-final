namespace Platform.Shared.Models
{
    public class ProviderAuthResponseDTO
    {
        public string Token { get; set; }
    }

    public class ProviderStakeResponseDTO
    {
        public decimal Stake { get; set; }
    }

    public class ProviderAssessmentResponseDTO
    {
        public bool IsBetAllowed { get; set; }
        public decimal Stake { get; set; }
        public string Message { get; set; }
    }
}
