using MongoDB.Bson.Serialization.Attributes;

namespace Platform.Shared.Data.Sportsbook.Entities
{
    public class Maintenance
    {
        [BsonId]
        public string ma { get; set; }
        public bool isMa { get; set; }
        public bool isBeSt { get; set; }
    }
}
