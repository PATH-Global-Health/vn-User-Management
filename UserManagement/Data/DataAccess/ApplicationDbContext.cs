using Data.MongoCollections;
using MongoDB.Driver;
using System.Linq;

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
        public IMongoCollection<Role> Roles => _db.GetCollection<Role>("roles");
        public IMongoCollection<UserInformation> Users => _db.GetCollection<UserInformation>("users");

        public IMongoCollection<ResourcePermission> ResourcePermissions => _db.GetCollection<ResourcePermission>("resourcePermissions");
        public IMongoCollection<UiPermission> UiPermissions => _db.GetCollection<UiPermission>("uiPermissions");

        public IMongoCollection<ProvincialInformation> ProvincialInformation => _db.GetCollection<ProvincialInformation>("provincialInformation");
                public IMongoCollection<SecurityQuestion> SecurityQuestions => _db.GetCollection<SecurityQuestion>("securityQuestions");

        public IClientSessionHandle StartSession()
        {
            var session = _mongoClient.StartSession();
            return session;
        }

        public void CreateCollectionsIfNotExists()
        {
            var collectionNames = _db.ListCollectionNames().ToList();

            if (!collectionNames.Any(name => name == "groups"))
            {
                _db.CreateCollection("groups");
            }
            if (!collectionNames.Any(name => name == "roles"))
            {
                _db.CreateCollection("roles");
            }
            if (!collectionNames.Any(name => name == "users"))
            {
                _db.CreateCollection("users");
            }

            if (!collectionNames.Any(name => name == "resourcePermissions"))
            {
                _db.CreateCollection("resourcePermissions");
            }
            if (!collectionNames.Any(name => name == "uiPermissions"))
            {
                _db.CreateCollection("uiPermissions");
            }

            if (!collectionNames.Any(name => name == "provincialInformation"))
            {
                _db.CreateCollection("provincialInformation");
            }
        }

        public void SeedData()
        {

        }
    }
}
