﻿using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace Data.MongoCollections
{
    public class UserInformation
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Username { get; set; }
        public string NormalizedUsername { get; set; }
        public string HashedPassword { get; set; }

        public string Email { get; set; }
        public string NormalizedEmail { get; set; }
        public string PhoneNumber { get; set; }
        public string FullName { get; set; }

        public SecurityQuestion SecurityQuestion { get; set; }
        public string SecurityQuestionAnswer { get; set; }

        /// <summary>
        /// References to Role Collection
        /// </summary>
        public List<string> RoleIds { get; set; } = new List<string>();
        /// <summary>
        /// References to Group Collection
        /// </summary>
        public List<string> GroupIds { get; set; } = new List<string>();
        /// <summary>
        /// References to Provincial Info
        /// </summary>
        public List<string> ProvincialInformation { get; set; } = new List<string>();

        public List<UiPermission> UiPermissions { get; set; } = new List<UiPermission>();
        public List<ResourcePermission> ResourcePermissions { get; set; } = new List<ResourcePermission>();

        [BsonDateTimeOptions]
        public DateTime DateCreated { get; set; } = DateTime.Now;
        [BsonDateTimeOptions]
        public DateTime DateUpdated { get; set; } = DateTime.Now;
    }
}
