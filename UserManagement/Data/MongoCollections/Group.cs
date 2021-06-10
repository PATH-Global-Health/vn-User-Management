using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace Data.MongoCollections
{
    [BsonIgnoreExtraElements]
    public class Group
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string NormalizedName { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// References to User Collection
        /// </summary>
        public List<string> UserIds { get; set; } = new List<string>();
        /// <summary>
        /// References to Role Collection
        /// </summary>
        public List<string> RoleIds { get; set; } = new List<string>();

        public List<string> UiPermissionIds { get; set; } = new List<string>();
        public List<string> ResourcePermissionIds { get; set; } = new List<string>();

        [BsonDateTimeOptions]
        public DateTime DateCreated { get; set; } = DateTime.Now;
        [BsonDateTimeOptions]
        public DateTime DateUpdated { get; set; } = DateTime.Now;
    }
}
