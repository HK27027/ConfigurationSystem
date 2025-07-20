using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace CaseSecilStore.Models
{
    public class ConfigurationItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonRequired]
        public string Name { get; set; }

        [BsonRequired]
        public string Type { get; set; }

        [BsonRequired]
        public string Value { get; set; }

        [BsonDefaultValue(true)]
        public bool IsActive { get; set; }=true;

        [BsonRequired]
        public string ApplicationName { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        [BsonDefaultValue("UtcNow")]
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
    }
}
