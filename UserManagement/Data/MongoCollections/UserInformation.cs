using Data.ViewModels;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace Data.MongoCollections
{
    [BsonIgnoreExtraElements]
    public class UserInformation
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Username { get; set; }
        public string NormalizedUsername { get; set; }
        public string HashedPassword { get; set; }
        public string HashedCredential { get; set; }

        public string Email { get; set; }
        public string NormalizedEmail { get; set; }
        public string PhoneNumber { get; set; }
        public string FullName { get; set; }
        public string SecurityQuestionId { get; set; }
        public string SecurityQuestionAnswer { get; set; }
        public OTP OTP { get; set; }
        public bool IsConfirmed { get; set; }
        public bool IsAnonymous { get; set; } = false;

        public List<string> RoleIds { get; set; } = new List<string>();
        public List<string> GroupIds { get; set; } = new List<string>();
        /// <summary>
        /// References to Provincial Info
        /// </summary>
        public List<string> ProvincialInformation { get; set; } = new List<string>();
        public bool? DidFirstTimeLogIn { get; set; }

        public List<string> UiPermissionIds { get; set; } = new List<string>();
        public List<string> ResourcePermissionIds { get; set; } = new List<string>();

        [BsonDateTimeOptions]
        public DateTime DateCreated { get; set; } = DateTime.Now;
        [BsonDateTimeOptions]
        public DateTime DateUpdated { get; set; } = DateTime.Now;

        public bool? IsDisabled { get; set; } = false;
    }
}
