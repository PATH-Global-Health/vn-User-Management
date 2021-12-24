using AutoMapper;
using Data.Constants;
using Data.DataAccess;
using Data.MongoCollections;
using Data.ViewModels;
using LazyCache;
using Microsoft.Extensions.Caching.Distributed;
using MongoDB.Driver;
using Newtonsoft.Json;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Implementations
{
    public class RoleService : IRoleService
    {
        private readonly IMapper _mapper;
        private readonly ApplicationDbContext _dbContext;
        private readonly IAppCache _cache;
        private readonly IDistributedCache _distributedCache;

        public RoleService(IMapper mapper, ApplicationDbContext dbContext, IAppCache cache, IDistributedCache distributedCache)
        {
            _mapper = mapper;
            _dbContext = dbContext;
            _cache = cache;
            _distributedCache = distributedCache;
        }

        public ResultModel AddUsers(string roleId, List<string> userIds)
        {
            var result = new ResultModel();
            var role = _dbContext.Roles.Find(i => i.Id == roleId).FirstOrDefault();
            if (role != null)
            {
                var currentUserIds = role.UserIds;
                var existedUserIds = role.UserIds.Intersect(userIds);
                userIds.RemoveAll(id => existedUserIds.Any(existedId => existedId == id));
                role.UserIds.AddRange(userIds);
                _dbContext.Roles.FindOneAndReplace(i => i.Id == roleId, role);

                foreach (var userId in userIds)
                {
                    var user = _dbContext.Users.Find(i => i.Id == userId).FirstOrDefault();
                    if (!user.RoleIds.Any(rId => rId == roleId))
                    {
                        user.RoleIds.Add(role.Id);
                    }
                    user.DateUpdated = DateTime.Now;
                    _dbContext.Users.ReplaceOne(i => i.Id == userId, user);
                }

                result.Succeed = true;
            }
            return result;
        }

        public ResultModel Create(RoleCreateModel model)
        {
            var result = new ResultModel();
            var existedRole = _dbContext.Roles.Find(i => i.NormalizedName == model.Name.ToUpper()).FirstOrDefault();
            if (existedRole != null)
            {
                result.ErrorMessage = ErrorConstants.EXISTED_ROLE;
                return result;
            }
            var newRole = new Role
            {
                Name = model.Name,
                NormalizedName = model.Name.ToUpper(),
                Description = model.Description
            };
            _dbContext.Roles.InsertOne(newRole);
            result.Succeed = true;
            result.Data = newRole.Id;
            ClearCache();
            return result;
        }

        public ResultModel Delete(string roleId)
        {
            var result = new ResultModel();
            try
            {
                var role = _dbContext.Roles.FindOneAndDelete(i => i.Id == roleId);
                if (role != null)
                {
                    role.GroupIds.AsParallel().ForAll(groupId =>
                    {
                        var group = _dbContext.Groups.Find(i => i.Id == groupId).FirstOrDefault();
                        if (group != null)
                        {
                            group.RoleIds.Remove(role.Id);
                            _dbContext.Groups.ReplaceOne(i => i.Id == group.Id, group);
                        }
                    });

                    role.UserIds.AsParallel().ForAll(userId =>
                    {
                        var user = _dbContext.Users.Find(i => i.Id == userId).FirstOrDefault();
                        if (user != null)
                        {
                            user.RoleIds.Remove(role.Id);
                            _dbContext.Users.ReplaceOne(i => i.Id == user.Id, user);
                        }
                    });
                }
                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            ClearCache();
            return result;
        }

        public RoleModel Get(string id)
        {
            var role = _dbContext.Roles.Find(i => i.Id == id).FirstOrDefault();
            if (role != null)
            {
                return _mapper.Map<Role, RoleModel>(role);
            }
            return null;
        }

        public ICollection<RoleModel> GetAll()
        {
            var roles = _dbContext.Roles.Find(i => true).SortBy(i => i.Name).ToList();
            return _mapper.Map<List<Role>, List<RoleModel>>(roles);
        }

        public List<GroupModel> GetGroups(string roleId)
        {
            var role = _dbContext.Roles.Find(u => u.Id == roleId).FirstOrDefault();
            List<GroupModel> result = new List<GroupModel>();

            foreach (var groupId in role.GroupIds)
            {
                var group = _dbContext.Groups.Find(u => u.Id == groupId).FirstOrDefault();
                result.Add(_mapper.Map<Group, GroupModel>(group));
            }
            return result;
        }

        public List<UserInformationModel> GetUsers(string roleId)
        {
            var role = _dbContext.Roles.Find(u => u.Id == roleId).FirstOrDefault();
            List<UserInformationModel> result = new List<UserInformationModel>();
            foreach (var userId in role.UserIds)
            {
                var user = _dbContext.Users.Find(u => u.Id == userId).FirstOrDefault();
                result.Add(_mapper.Map<UserInformation, UserInformationModel>(user));
            }
            return result;
        }

        public ResultModel RemoveUser(string roleId, string userId)
        {
            ResultModel result = new ResultModel();
            try
            {
                var role = _dbContext.Roles.Find(g => g.Id == roleId).FirstOrDefault();
                role.UserIds.Remove(userId);
                _dbContext.Roles.ReplaceOne(g => g.Id == roleId, role);
                var user = _dbContext.Users.Find(x => x.Id == userId).FirstOrDefault();
                user.RoleIds.Remove(roleId);
                _dbContext.Users.ReplaceOne(x => x.Id == userId, user);
                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.Succeed = false;
                result.ErrorMessage = e.Message;
            }
            return result;
        }

        public ResultModel Update(string roleId, RoleUpdateModel model)
        {
            var result = new ResultModel();
            try
            {
                var roles = _dbContext.Roles.Find(i => i.Id == roleId).FirstOrDefault();
                if (roles != null)
                {
                    roles = _mapper.Map(model, roles);
                    _dbContext.Roles.FindOneAndReplace(i => i.Id == roleId, roles);
                    result.Succeed = true;
                }
                else
                {
                    result.ErrorMessage = "Record is not existed";
                }
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }
        public async Task<List<Role>> GetFromCache()
        {
            var cacheContent = await _distributedCache.GetStringAsync(CacheConstants.ROLE);
            if (cacheContent != null)
            {
                return JsonConvert.DeserializeObject<List<Role>>(cacheContent);
            }
            else
            {
                var model = await _dbContext.Roles.Find(x => true).Project(
                    x => new Role
                    {
                        Id = x.Id,
                        ResourcePermissionIds = x.ResourcePermissionIds,
                    }
                    ).ToListAsync();
                var content = JsonConvert.SerializeObject(model);
                await _distributedCache.SetStringAsync(CacheConstants.ROLE, content);
                return model;
            }
        }
        public void ClearCache() => _distributedCache.Remove(CacheConstants.ROLE);
    }
}
