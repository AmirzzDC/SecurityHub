// Models/BlacklistItem.cs
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SecureNetBackend.Models
{
    public class BlacklistItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Item { get; set; }
    }
}
