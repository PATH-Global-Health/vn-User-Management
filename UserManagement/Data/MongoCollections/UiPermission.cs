using Data.Enums;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Data.MongoCollections
{
    public class UiPermission
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Description { get; set; }

        public string Code { get; set; } = Guid.NewGuid().ToString();
        public PermissionType Type { get; set; }


        [BsonDateTimeOptions]
        public DateTime DateCreated { get; set; } = DateTime.Now;
        [BsonDateTimeOptions]
        public DateTime DateUpdated { get; set; } = DateTime.Now;
    }
}
