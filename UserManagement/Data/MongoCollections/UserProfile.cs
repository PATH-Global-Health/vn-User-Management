using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Data.MongoCollections
{
    public class UserProfile
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Code { get; set; }
        public string FullName { get; set; }
        public string OwnerId { get; set; }
        /// <summary>
        /// Resource App where the owner is
        /// </summary>
        public string OwnerSource { get; set; }
        public DateTime DateOfBirth { get; set; }
        public bool HasYearOfBirthOnly { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public Address Address { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public DateTime DateUpdated { get; set; } = DateTime.Now;
    }

    public class Address
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Room { get; set; }
        public string Floor { get; set; }
        /// <summary>
        /// Block , Towers , ...
        /// </summary>
        public string Block { get; set; }
        public string Name { get; set; }

        public string ProvinceValue { get; set; }
        public string DistrictValue { get; set; }
        public string WardValue { get; set; }
        /// <summary>
        /// Khu phố , thôn, ...
        /// </summary>
        public string Quarter { get; set; }
        public string StreetHouseNumber { get; set; }
        public string LocationType { get; set; }
    }
}
