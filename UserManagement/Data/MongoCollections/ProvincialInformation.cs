using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Data.MongoCollections
{
    public class ProvincialInformation
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public int Value { get; set; }
        public string Description { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public DateTime DateUpdated { get; set; } = DateTime.Now;
    }
}
