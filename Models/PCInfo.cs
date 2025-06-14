// Models/PCInfo.cs
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SecureNetBackend.Models
{
    public class PCInfo
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string PCName { get; set; }
        public string IP { get; set; }
        public string AdminCode { get; set; }
        public bool IsConnected { get; set; }
    }
}
