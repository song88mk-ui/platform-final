using MongoDB.Bson.Serialization.Attributes;

namespace Platform.Shared.Data.CasinoBetting.Entities
{
    public class BettingStatus
    {
        [BsonId]
        public string BeSt { get; set; }
        public bool IsBeSt { get; set; }
    }
}
