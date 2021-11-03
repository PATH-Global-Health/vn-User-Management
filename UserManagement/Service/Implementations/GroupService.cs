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
    public class GroupService : IGroupService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;

        public GroupService(ApplicationDbContext context, IMapper mapper)
        {
            _dbContext = context;
            _mapper = mapper;
        }

        public ICollection<GroupOverviewModel> GetAll()
        {
            var groups = _dbContext.Groups.Find(i => true).ToList();
            return _mapper.Map<List<Group>, List<GroupOverviewModel>>(groups);
        }
        public GroupModel Get(string id)
        {
            var group = _dbContext.Groups.Find(i => i.Id == id).FirstOrDefault();
            if (group != null)
            {
                var groupModel = _mapper.Map<Group, GroupUpdateModel>(group);

                var result = new GroupModel();
                result.Name = groupModel.Name;
                result.Description = groupModel.Description;
                result.Users = _mapper.Map<List<UserInformation>, List<UserInformationModel>>(group.UserIds.Select(userId => _dbContext.Users.Find(i => i.Id == userId).FirstOrDefault()).ToList());
                return result;
            }
            return null;
        }
        public ResultModel Create(GroupCreateModel model)
        {
            var result = new ResultModel();
            var existedGroup = _dbContext.Groups.Find(i => i.NormalizedName == model.Name.ToUpper()).FirstOrDefault();
            if (existedGroup != null)
            {
                result.ErrorMessage = "Group is existed";
                return result;
            }
            var newGroup = new Group
            {
                Name = model.Name,
                NormalizedName = model.Name.ToUpper(),
                Description = model.Description
            };
            _dbContext.Groups.InsertOne(newGroup);
            result.Succeed = true;

            result.Data = newGroup.Id;

            return result;
        }
        public ResultModel Update(string groupId, GroupUpdateModel model)
        {
            var result = new ResultModel();
            try
            {
                var group = _dbContext.Groups.Find(i => i.Id == groupId).FirstOrDefault();
                if (group != null)
                {
                    group = _mapper.Map(model, group);
                    _dbContext.Groups.FindOneAndReplace(i => i.Id == groupId, group);
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

        public ResultModel Delete(string groupId)
        {
            var result = new ResultModel();
            var session = _dbContext.StartSession(); session.StartTransaction();
            try
            {
                var group = _dbContext.Groups.FindOneAndDelete(session, i => i.Id == groupId);
                if (group != null)
                {
                    group.RoleIds.AsParallel().ForAll(roleId =>
                    {
                        var role = _dbContext.Roles.Find(i => i.Id == roleId).FirstOrDefault();
                        if (role != null)
                        {
                            role.GroupIds.Remove(group.Id);
                            _dbContext.Roles.ReplaceOne(session, i => i.Id == role.Id, role);
                        }
                    });

                    group.UserIds.AsParallel().ForAll(userId =>
                    {
                        var user = _dbContext.Users.Find(i => i.Id == userId).FirstOrDefault();
                        if (user != null)
                        {
                            user.GroupIds.Remove(group.Id);
                            _dbContext.Users.ReplaceOne(session, i => i.Id == user.Id, user);
                        }
                    });
                }

                session.CommitTransaction();
                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
                session.AbortTransaction();
            }
            finally
            {
                session.Dispose();
            }
            return result;
        }

        public ResultModel AddUsers(string groupId, List<string> userIds)
        {
            ResultModel result = new ResultModel();
            try
            {
                var group = _dbContext.Groups.Find(g => g.Id == groupId).FirstOrDefault();
                var currentUserIds = group.UserIds;
                var duplicateUserIds = currentUserIds.Intersect(userIds);

                //Set userids to group
                userIds.RemoveAll(id => duplicateUserIds.Any(duplicateUserId => id == duplicateUserId));
                group.UserIds.AddRange(userIds);
                _dbContext.Groups.ReplaceOne(g => g.Id == groupId, group);

                //Set groupId to users
                foreach (var userId in userIds)
                {
                    var user = _dbContext.Users.Find(i => i.Id == userId).FirstOrDefault();
                    if (!user.GroupIds.Any(gId => gId == groupId))
                    {
                        user.GroupIds.Add(groupId);
                    }
                    user.DateUpdated = DateTime.Now;
                    _dbContext.Users.FindOneAndReplace(i => i.Id == user.Id, user);
                }

                result.Data = userIds;
                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.Succeed = false;
                result.ErrorMessage = e.Message;
            }
            return result;
        }
        public ResultModel RemoveUser(string groupId, string userId)
        {
            ResultModel result = new ResultModel();
            try
            {
                var group = _dbContext.Groups.Find(g => g.Id == groupId).FirstOrDefault();
                group.UserIds.Remove(userId);
                _dbContext.Groups.ReplaceOne(g => g.Id == groupId, group);

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.Succeed = false;
                result.ErrorMessage = e.Message;
            }
            return result;
        }
        public ResultModel AddRoles(string groupId, List<string> roleIds)
        {
            ResultModel result = new ResultModel();
            try
            {
                var group = _dbContext.Groups.Find(g => g.Id == groupId).FirstOrDefault();
                var currentRoleIds = group.RoleIds;
                var duplicateUserIds = currentRoleIds.Intersect(roleIds);

                roleIds.RemoveAll(id => duplicateUserIds.Any(duplicateUserId => id == duplicateUserId));
                group.RoleIds.AddRange(roleIds);
                _dbContext.Groups.ReplaceOne(g => g.Id == groupId, group);

                result.Data = roleIds;
                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.Succeed = false;
                result.ErrorMessage = e.Message;
            }
            return result;
        }
        public ResultModel RemoveRole(string groupId, string roleId)
        {
            ResultModel result = new ResultModel();
            try
            {
                var group = _dbContext.Groups.Find(g => g.Id == groupId).FirstOrDefault();
                group.RoleIds.Remove(roleId);
                _dbContext.Groups.ReplaceOne(g => g.Id == groupId, group);

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.Succeed = false;
                result.ErrorMessage = e.Message;
            }
            return result;
        }

        public List<RoleModel> GetRoles(string id)
        {
            var result = new List<RoleModel>();
            try
            {
                var group = _dbContext.Groups.Find(i => i.Id == id).FirstOrDefault();
                if (group != null)
                {
                    foreach (var roleId in group.RoleIds)
                    {
                        var role = _dbContext.Roles.Find(r => r.Id == roleId).FirstOrDefault();
                        if (role != null)
                        {
                            result.Add(_mapper.Map<Role, RoleModel>(role));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.InnerException != null ? e.InnerException.Message : e.Message);
            }
            return result;
        }

        public List<UserInformationModel> GetUsers(string id)
        {
            var result = new List<UserInformationModel>();
            try
            {
                var group = _dbContext.Groups.Find(i => i.Id == id).FirstOrDefault();
                if (group != null)
                {
                    foreach (var userId in group.UserIds)
                    {
                        var user = _dbContext.Users.Find(r => r.Id == userId).FirstOrDefault();
                        if (user != null)
                        {
                            result.Add(_mapper.Map<UserInformation, UserInformationModel>(user));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.InnerException != null ? e.InnerException.Message : e.Message);
            }
            return result;
        }
    }
}
