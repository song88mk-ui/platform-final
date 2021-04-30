namespace Platform.API.Models
{
    public class TopLeaguesRequest
    {
        public int SportId { get; set; }
        public string LanguageCode { get; set; }
    }
    public class TopLeaguesRequest2
    {
        public int SportId { get; set; }
        public string LanguageCode { get; set; }
        public int LeagueId { get; set; }
        public int SegmentId { get; set; }
    }
    public class TempTopLeaguesRequest
    {
        public int SportId { get; set; }
    }
}
