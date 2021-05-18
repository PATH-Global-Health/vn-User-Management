using Data.MongoCollections;
using MongoDB.Driver;

namespace Data.DataAccess
{
    public class ApplicationDbContext
    {
        private readonly IMongoDatabase _db;
        private IMongoClient _mongoClient;

        public ApplicationDbContext(IMongoClient client, string databaseName)
        {
            _db = client.GetDatabase(databaseName);
            _mongoClient = client;
        }

        public IMongoCollection<Group> Groups => _db.GetCollection<Group>("groups");
        public IMongoCollection<ResourcePermission> ResourcePermissions => _db.GetCollection<ResourcePermission>("resourcePermissions");
        public IMongoCollection<UiPermission> UiPermissions => _db.GetCollection<UiPermission>("uiPermissions");
        public IMongoCollection<Role> Roles => _db.GetCollection<Role>("roles");
        public IMongoCollection<UserInformation> Users => _db.GetCollection<UserInformation>("users");
        public IMongoCollection<UserProfile> UserProfiles => _db.GetCollection<UserProfile>("userProfiles");
        public IMongoCollection<ProvincialInformation> ProvincialInformation => _db.GetCollection<ProvincialInformation>("provincialInformation");
    }
}
