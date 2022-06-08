using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace Data.MongoCollections
{
    [BsonIgnoreExtraElements]
    public class Session
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; }
        public string ServiceName { get; set; }
        public string AppName { get; set; }
        [BsonDateTimeOptions]
        public DateTime DateCreated { get; set; } = DateTime.Now;
        [BsonDateTimeOptions]
        public DateTime? DateEnded { get; set; }
    }
}
