using Data.MongoCollections;
using MongoDB.Driver;
using System.Collections.Generic;
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

        public IMongoCollection<ApiModule> ApiModules => _db.GetCollection<ApiModule>("apiModules");


        public IClientSessionHandle StartSession()
        {
            var session = _mongoClient.StartSession();
            return session;
        }

        public void CreateCollectionsIfNotExists()
        {
            var collectionNames = _db.ListCollectionNames().ToList();

            if (!collectionNames.Any(name => name == "apiModules"))
            {
                _db.CreateCollection("apiModules");
            }
            if (!collectionNames.Any(name => name == "groups"))
            {
                _db.CreateCollection("groups");

                this.Groups.InsertMany(new List<Group> {
                    new Group{
                        Id = "90e7e595-6cc7-4fe9-8921-03ec2971be0b",
                        NormalizedName = "ADMIN",
                        Name = "ADMIN",
                        UserIds = new List<string>
                            {
                                "90e7e595-6cc7-4fe9-8921-03ec2971be0b",
                            },
                    },
                    new Group{
                        Id = "90e7e595-6cc7-4fe9-8921-03ec2971be0c",
                        NormalizedName = "CDC",
                        Name = "CDC",
                    },
                    new Group{
                        Id = "90e7e595-6cc7-4fe9-8921-03ec2971be0d",
                        NormalizedName = "CBO",
                        Name = "CBO",
                    },
                    new Group{
                        Id = "90e7e595-6cc7-4fe9-8921-03ec2971be0e",
                        NormalizedName = "EMPLOYEE",
                        Name = "EMPLOYEE",
                    },
                    new Group{
                        Id = "90e7e595-6cc7-4fe9-8921-03ec2971be0f",
                        NormalizedName = "CUSTOMER",
                        Name = "CUSTOMER",
                    },
                });
            }
            if (!collectionNames.Any(name => name == "roles"))
            {
                _db.CreateCollection("roles");

                this.Roles.InsertMany(new List<Role> {
                    new Role{
                        Id = "90e7e595-6cc7-4fe9-8921-03ec2971be0b",
                        NormalizedName = "ADMIN",
                        Name = "ADMIN",
                        UserIds = new List<string>
                            {
                                "90e7e595-6cc7-4fe9-8921-03ec2971be0b",
                            },
                    },
                });
            }
            if (!collectionNames.Any(name => name == "users"))
            {
                _db.CreateCollection("users");

                this.Users.InsertMany(new List<UserInformation> {
                new UserInformation{
                    Id = "90e7e595-6cc7-4fe9-8921-03ec2971be0b",
                    Username = "super_admin",
                    NormalizedUsername = "SUPER_ADMIN",
                    IsConfirmed = true,
                    RoleIds = new List<string> {
                        "90e7e595-6cc7-4fe9-8921-03ec2971be0b",
                    },
                    HashedPassword = "AQAAAAEAACcQAAAAEGAZv1YKvBRakmLJwE6Hio3FCgJDc7iOZumx1gwQDu34qYlkKfkvIGLcoMYoI2UwHg==",
                    HashedCredential = "AQAAAAEAACcQAAAAEMhn6ucCttYdwvdfue8PMZXJ/Plde3OD3mSH2i94SzgS1wtxxT+wWxpgF9F1XhMVHQ==",
                },
                });
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
