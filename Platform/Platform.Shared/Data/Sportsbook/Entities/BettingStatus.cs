using MongoDB.Bson.Serialization.Attributes;

namespace Platform.Shared.Data.Sportsbook.Entities
{
    public class BettingStatus
    {
        [BsonId]
        public string BeSo { get; set; }
        public bool IsBeSo { get; set; }
    }
}
