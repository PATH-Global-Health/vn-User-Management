using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Data.MongoCollections
{
    public class SecurityQuestion
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Question { get; set; }
    }
}
