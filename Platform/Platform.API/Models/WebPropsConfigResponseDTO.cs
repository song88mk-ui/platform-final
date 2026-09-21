namespace Platform.API.Models
{
    // Shape of webprops-api's GET /api/v1/config/{clientCode} response, trimmed to the fields
    // this API actually needs (BO's Client Site Config screen writes the full document - see
    // WebPropsRepository.UpdateClientFeatureFlagsAsync in backoffice-final).
    public class WebPropsConfigResponseDTO
    {
        public WebPropsFeaturesDTO Features { get; set; }
    }

    public class WebPropsFeaturesDTO
    {
        public bool FullCashout { get; set; }
        public bool PartialCashout { get; set; }
    }
}
