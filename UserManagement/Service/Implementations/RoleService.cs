using AutoMapper;
using Data.DataAccess;
using Data.MongoCollections;
using Data.ViewModels;
using MongoDB.Driver;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Service.Implementations
{
    public class RoleService : IRoleService
    {
        private readonly IMapper _mapper;
        private readonly ApplicationDbContext _dbContext;

        public RoleService(IMapper mapper, ApplicationDbContext dbContext)
        {
            _mapper = mapper;
            _dbContext = dbContext;
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
                result.ErrorMessage = "Role is existed";
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
            return result;
        }

        public ResultModel Delete(string roleId)
        {
            var result = new ResultModel();
            _dbContext.Roles.FindOneAndDelete(i => i.Id == roleId);
            result.Succeed = true;

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

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.Succeed = false;
                result.ErrorMessage = e.Message;
            }
            return result;
        }

        public ResultModel Update(RoleModel model)
        {
            var result = new ResultModel();
            try
            {
                var roles = _dbContext.Roles.Find(i => i.Id == model.Id).FirstOrDefault();
                if (roles != null)
                {
                    roles = _mapper.Map(model, roles);
                    _dbContext.Roles.FindOneAndReplace(i => i.Id == model.Id, roles);
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
    }
}
