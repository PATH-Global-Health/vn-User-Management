using Data.Enums;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Data.MongoCollections
{
    public class ResourcePermission
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Description { get; set; }

        public string Url { get; set; }
        public string NormalizedUrl { get; set; }
        public string Method { get; set; }
        public string NormalizedMethod { get; set; }

        public PermissionType PermissionType { get; set; } = PermissionType.Deny;


        [BsonDateTimeOptions]
        public DateTime DateCreated { get; set; } = DateTime.Now;
        [BsonDateTimeOptions]
        public DateTime DateUpdated { get; set; } = DateTime.Now;
    }
}
