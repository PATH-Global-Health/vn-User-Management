using AutoMapper;
using Data.Constants;
using Data.DataAccess;
using Data.Enums;
using Data.MongoCollections;
using Data.ViewModels;
using Data.ViewModels.Users;
using LazyCache;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MoreLinq;
using Newtonsoft.Json;
using Service.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Service.Implementations
{
    public class PermissionsService : IPermissionsService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IAppCache _cache;
        private readonly IDistributedCache _distributedCache;

        #region Special Roles
        private const string SUPER_ADMIN_ROLE = "LONGHDT";
        #endregion

        public PermissionsService(ApplicationDbContext dbContext, IMapper mapper,
            IServiceScopeFactory scopeFactory, IAppCache cache, IDistributedCache distributedCache)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _scopeFactory = scopeFactory;
            _cache = cache;
            _distributedCache = distributedCache;
        }

        #region Resource Permission
        public ResultModel AddPermissions(string holderId, HolderType holderType, List<ResourcePermissionCreateModel> permissions)
        {
            ResultModel result = new ResultModel();
            switch (holderType)
            {
                case HolderType.User: result = AddUserPermissions(holderId, permissions); break;
                case HolderType.Role: result = AddRolePermissions(holderId, permissions); break;
                case HolderType.Group: result = AddGroupPermissions(holderId, permissions); break;
            }
            ClearCache();
            return result;
        }
        public ResultModel AddResourcePermissions(string holderId, HolderType holderType, List<Guid> permissionIds)
        {
            ResultModel result = new ResultModel();
            switch (holderType)
            {
                case HolderType.User: result = AddUserResourcePermissions(holderId, permissionIds); break;
                case HolderType.Role: result = AddRoleResourcePermissions(holderId, permissionIds); break;
                case HolderType.Group: result = AddGroupResourcePermissions(holderId, permissionIds); break;
            }
            ClearCache();
            return result;
        }
        public ResultModel AddPermission(string holderId, HolderType holder, ResourcePermissionCreateModel model)
        {
            ResultModel result = new ResultModel();
            if (!IsValidApiPath(model.Url))
            {
                result.ErrorMessage = "Invalid path";
                return result;
            }

            switch (holder)
            {
                case HolderType.User: result = AddUserPermission(holderId, model); break;
                case HolderType.Role: result = AddRolePermission(holderId, model); break;
                case HolderType.Group: result = AddGroupPermission(holderId, model); break;
            }
            ClearCache();
            return result;
        }
        public ResultModel ChangeAPIAuthorizationResourcePermission(string id, bool isAuthorized)
        {
            ResultModel result = new ResultModel();
            if (string.IsNullOrEmpty(id))
            {
                result.ErrorMessage = "Id is not valid";
                return result;
            }
            var resourcePermission = _dbContext.ResourcePermissions.FindOneAndUpdate(x => x.Id == id,
                Builders<ResourcePermission>.Update.Set(x => x.IsAuthorizedAPI, isAuthorized));
            if (resourcePermission == null)
            {
                result.ErrorMessage = "Resource Permission is not existed";
                return result;
            }
            result.Succeed = true;
            ClearCache();
            return result;
        }

        private ResultModel AddUserPermission(string userId, ResourcePermissionCreateModel model)
        {
            var result = new ResultModel();
            try
            {
                var user = _dbContext.Users.Find(i => i.Id == userId).FirstOrDefault();
                if (user == null)
                {
                    result.ErrorMessage = "User is not existed";
                    return result;
                }

                var createPermissionResult = CreatePermission(model);
                if (createPermissionResult.Succeed)
                {
                    user.ResourcePermissionIds.Add((string)createPermissionResult.Data);

                    _dbContext.Users.FindOneAndReplace(i => i.Id == user.Id, user);
                }
                else
                {
                    return createPermissionResult;
                }

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        private ResultModel AddRolePermission(string roleId, ResourcePermissionCreateModel model)
        {
            var result = new ResultModel();
            try
            {
                var role = _dbContext.Roles.Find(i => i.Id == roleId).FirstOrDefault();
                if (role == null)
                {
                    result.ErrorMessage = "Role is not existed";
                    return result;
                }

                var createPermissionResult = CreatePermission(model);
                if (createPermissionResult.Succeed)
                {
                    role.ResourcePermissionIds.Add((string)createPermissionResult.Data);

                    _dbContext.Roles.FindOneAndReplace(i => i.Id == role.Id, role);
                }
                else
                {
                    return createPermissionResult;
                }

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        private ResultModel AddGroupPermission(string groupId, ResourcePermissionCreateModel model)
        {
            var result = new ResultModel();
            try
            {
                var group = _dbContext.Groups.Find(i => i.Id == groupId).FirstOrDefault();
                if (group == null)
                {
                    result.ErrorMessage = "Group is not existed";
                    return result;
                }

                var createPermissionResult = CreatePermission(model);
                if (createPermissionResult.Succeed)
                {
                    group.ResourcePermissionIds.Add((string)createPermissionResult.Data);

                    _dbContext.Groups.FindOneAndReplace(i => i.Id == group.Id, group);
                }
                else
                {
                    return createPermissionResult;
                }

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }



        private ResultModel AddUserPermissions(string userId, List<ResourcePermissionCreateModel> models)
        {
            var result = new ResultModel();
            try
            {
                var user = _dbContext.Users.Find(i => i.Id == userId).FirstOrDefault();
                if (user == null)
                {
                    result.ErrorMessage = "User is not existed";
                    return result;
                }

                var createPermissionResults = models.AsParallel().Select(model => CreatePermission(model));
                if (createPermissionResults.All(i => i.Succeed))
                {
                    user.ResourcePermissionIds.AddRange(createPermissionResults.Select(rs => (string)rs.Data));

                    _dbContext.Users.FindOneAndReplace(i => i.Id == user.Id, user);
                }
                else
                {
                    var createdPermissionIds = createPermissionResults.Where(i => i.Succeed).Select(i => (string)i.Data);
                    createdPermissionIds.AsParallel().ForAll(id =>
                    {
                        _dbContext.ResourcePermissions.DeleteOne(i => i.Id == id);
                    });

                    result.Succeed = false;
                    result.ErrorMessage = "Errors occurred while creating permissions";
                    return result;
                }

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        private ResultModel AddRolePermissions(string roleId, List<ResourcePermissionCreateModel> models)
        {
            var result = new ResultModel();
            try
            {
                var role = _dbContext.Roles.Find(i => i.Id == roleId).FirstOrDefault();
                if (role == null)
                {
                    result.ErrorMessage = "Role is not existed";
                    return result;
                }

                var createPermissionResults = models.AsParallel().Select(model => CreatePermission(model));
                if (createPermissionResults.All(i => i.Succeed))
                {
                    role.ResourcePermissionIds.AddRange(createPermissionResults.Select(rs => (string)rs.Data));

                    _dbContext.Roles.FindOneAndReplace(i => i.Id == role.Id, role);
                }
                else
                {
                    var createdPermissionIds = createPermissionResults.Where(i => i.Succeed).Select(i => (string)i.Data);
                    createdPermissionIds.AsParallel().ForAll(id =>
                    {
                        _dbContext.ResourcePermissions.DeleteOne(i => i.Id == id);
                    });

                    result.Succeed = false;
                    result.ErrorMessage = "Errors occurred while creating permissions";
                    return result;
                }

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        private ResultModel AddGroupPermissions(string groupId, List<ResourcePermissionCreateModel> models)
        {
            var result = new ResultModel();
            try
            {
                var group = _dbContext.Groups.Find(i => i.Id == groupId).FirstOrDefault();
                if (group == null)
                {
                    result.ErrorMessage = "Group is not existed";
                    return result;
                }

                var createPermissionResults = models.AsParallel().Select(model => CreatePermission(model));
                if (createPermissionResults.All(i => i.Succeed))
                {
                    group.ResourcePermissionIds.AddRange(createPermissionResults.Select(rs => (string)rs.Data));

                    _dbContext.Groups.FindOneAndReplace(i => i.Id == group.Id, group);
                }
                else
                {
                    var createdPermissionIds = createPermissionResults.Where(i => i.Succeed).Select(i => (string)i.Data);
                    createdPermissionIds.AsParallel().ForAll(id =>
                    {
                        _dbContext.ResourcePermissions.DeleteOne(i => i.Id == id);
                    });

                    result.Succeed = false;
                    result.ErrorMessage = "Errors occurred while creating permissions";
                    return result;
                }

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        private ResultModel AddUserResourcePermissions(string userId, List<Guid> ids)
        {
            var result = new ResultModel();
            try
            {
                var user = _dbContext.Users.Find(i => i.Id == userId).FirstOrDefault();
                if (user == null)
                {
                    result.ErrorMessage = "User is not existed";
                    return result;
                }

                if (ids.All(id =>
                {
                    return _dbContext.ResourcePermissions.Find(x => x.Id == id.ToString()).Any();
                }))
                {
                    var unduplicates = ids.Select(id => id.ToString()).Except(user.ResourcePermissionIds);
                    user.ResourcePermissionIds.AddRange(unduplicates);
                    _dbContext.Users.FindOneAndReplace(i => i.Id == user.Id, user);
                }
                else
                {
                    result.Succeed = false;
                    result.ErrorMessage = "One of them is not existed";
                    return result;
                }

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        private ResultModel AddRoleResourcePermissions(string roleId, List<Guid> ids)
        {
            var result = new ResultModel();
            try
            {
                var role = _dbContext.Roles.Find(i => i.Id == roleId).FirstOrDefault();
                if (role == null)
                {
                    result.ErrorMessage = "Role is not existed";
                    return result;
                }

                if (ids.All(id =>
                {
                    return _dbContext.ResourcePermissions.Find(x => x.Id == id.ToString()).Any();
                }))
                {
                    var unduplicates = ids.Select(id => id.ToString()).Except(role.ResourcePermissionIds);
                    role.ResourcePermissionIds.AddRange(unduplicates);
                    _dbContext.Roles.FindOneAndReplace(i => i.Id == role.Id, role);
                }
                else
                {
                    result.Succeed = false;
                    result.ErrorMessage = "One of them is not existed";
                    return result;
                }

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        private ResultModel AddGroupResourcePermissions(string groupId, List<Guid> ids)
        {
            var result = new ResultModel();
            try
            {
                var group = _dbContext.Groups.Find(i => i.Id == groupId).FirstOrDefault();
                if (group == null)
                {
                    result.ErrorMessage = "Group is not existed";
                    return result;
                }

                if (ids.All(id =>
                {
                    return _dbContext.ResourcePermissions.Find(x => x.Id == id.ToString()).Any();
                }))
                {
                    var unduplicates = ids.Select(id => id.ToString()).Except(group.ResourcePermissionIds).ToList();
                    group.ResourcePermissionIds.AddRange(unduplicates);
                    _dbContext.Groups.FindOneAndReplace(i => i.Id == group.Id, group);
                }
                else
                {
                    result.Succeed = false;
                    result.ErrorMessage = "One of them is not existed";
                    return result;
                }

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }
        #endregion

        #region Ui Permission
        public ResultModel AddPermissions(string holderId, HolderType holderType, List<UiPermissionCreateModel> permissions)
        {
            ResultModel result = new ResultModel();
            switch (holderType)
            {
                case HolderType.User: result = AddUserPermissions(holderId, permissions); break;
                case HolderType.Role: result = AddRolePermissions(holderId, permissions); break;
                case HolderType.Group: result = AddGroupPermissions(holderId, permissions); break;
            }
            ClearCache();
            return result;
        }
        public ResultModel AddUIPermissions(string holderId, HolderType holderType, List<Guid> uiIds)
        {
            ResultModel result = new ResultModel();
            switch (holderType)
            {
                case HolderType.User: result = AddUserUIPermissions(holderId, uiIds); break;
                case HolderType.Role: result = AddRoleUIPermissions(holderId, uiIds); break;
                case HolderType.Group: result = AddGroupUIPermissions(holderId, uiIds); break;
            }
            ClearCache();
            return result;
        }

        public ResultModel AddPermission(string holderId, HolderType holder, UiPermissionCreateModel model)
        {
            ResultModel result = new ResultModel();
            switch (holder)
            {
                case HolderType.User: result = AddUserPermission(holderId, model); break;
                case HolderType.Role: result = AddRolePermission(holderId, model); break;
                case HolderType.Group: result = AddGroupPermission(holderId, model); break;
            }
            ClearCache();
            return result;
        }

        private ResultModel AddGroupPermission(string groupId, UiPermissionCreateModel model)
        {
            var result = new ResultModel();
            try
            {
                var group = _dbContext.Groups.Find(i => i.Id == groupId).FirstOrDefault();
                if (group == null)
                {
                    result.ErrorMessage = "Group is not existed";
                    return result;
                }

                var createPermissionResult = CreatePermission(model);
                if (createPermissionResult.Succeed)
                {
                    group.UiPermissionIds.Add((string)createPermissionResult.Data);

                    _dbContext.Groups.FindOneAndReplace(i => i.Id == group.Id, group);
                }
                else
                {
                    return createPermissionResult;
                }

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        private ResultModel AddRolePermission(string roleId, UiPermissionCreateModel model)
        {
            var result = new ResultModel();
            try
            {
                var role = _dbContext.Roles.Find(i => i.Id == roleId).FirstOrDefault();
                if (role == null)
                {
                    result.ErrorMessage = "Role is not existed";
                    return result;
                }

                var createPermissionResult = CreatePermission(model);
                if (createPermissionResult.Succeed)
                {
                    role.UiPermissionIds.Add((string)createPermissionResult.Data);

                    _dbContext.Roles.FindOneAndReplace(i => i.Id == role.Id, role);
                }
                else
                {
                    return createPermissionResult;
                }

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        private ResultModel AddUserPermission(string userId, UiPermissionCreateModel model)
        {
            var result = new ResultModel();
            try
            {
                var user = _dbContext.Users.Find(i => i.Id == userId).FirstOrDefault();
                if (user == null)
                {
                    result.ErrorMessage = "User is not existed";
                    return result;
                }

                var createPermissionResult = CreatePermission(model);
                if (createPermissionResult.Succeed)
                {
                    user.UiPermissionIds.Add((string)createPermissionResult.Data);

                    _dbContext.Users.FindOneAndReplace(i => i.Id == user.Id, user);
                }
                else
                {
                    return createPermissionResult;
                }

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        private ResultModel AddGroupPermissions(string groupId, List<UiPermissionCreateModel> models)
        {
            var result = new ResultModel();
            try
            {
                var group = _dbContext.Groups.Find(i => i.Id == groupId).FirstOrDefault();
                if (group == null)
                {
                    result.ErrorMessage = "Group is not existed";
                    return result;
                }

                var createPermissionResults = models.AsParallel().Select(model => CreatePermission(model));

                if (createPermissionResults.All(i => i.Succeed))
                {
                    group.UiPermissionIds.AddRange(createPermissionResults.Select(rs => (string)rs.Data));

                    _dbContext.Groups.FindOneAndReplace(i => i.Id == group.Id, group);
                }
                else
                {
                    var createdPermissionIds = createPermissionResults.Where(i => i.Succeed).Select(i => (string)i.Data);
                    createdPermissionIds.AsParallel().ForAll(id =>
                    {
                        _dbContext.UiPermissions.DeleteOne(i => i.Id == id);
                    });

                    result.Succeed = false;
                    result.ErrorMessage = "Some of the permissions cannot be created";
                    return result;
                }

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        private ResultModel AddRolePermissions(string roleId, List<UiPermissionCreateModel> models)
        {
            var result = new ResultModel();
            try
            {
                var role = _dbContext.Roles.Find(i => i.Id == roleId).FirstOrDefault();
                if (role == null)
                {
                    result.ErrorMessage = "Role is not existed";
                    return result;
                }

                var createPermissionResults = models.AsParallel().Select(model => CreatePermission(model));

                if (createPermissionResults.All(i => i.Succeed))
                {
                    role.UiPermissionIds.AddRange(createPermissionResults.Select(rs => (string)rs.Data));

                    _dbContext.Roles.FindOneAndReplace(i => i.Id == role.Id, role);
                }
                else
                {
                    var createdPermissionIds = createPermissionResults.Where(i => i.Succeed).Select(i => (string)i.Data);
                    createdPermissionIds.AsParallel().ForAll(id =>
                    {
                        _dbContext.UiPermissions.DeleteOne(i => i.Id == id);
                    });

                    result.Succeed = false;
                    result.ErrorMessage = "Some of the permissions cannot be created";
                    return result;
                }

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        private ResultModel AddUserPermissions(string userId, List<UiPermissionCreateModel> models)
        {
            var result = new ResultModel();
            try
            {
                var user = _dbContext.Users.Find(i => i.Id == userId).FirstOrDefault();
                if (user == null)
                {
                    result.ErrorMessage = "User is not existed";
                    return result;
                }

                var createPermissionResults = models.AsParallel().Select(model => CreatePermission(model));

                if (createPermissionResults.All(i => i.Succeed))
                {
                    user.UiPermissionIds.AddRange(createPermissionResults.Select(rs => (string)rs.Data));

                    _dbContext.Users.FindOneAndReplace(i => i.Id == user.Id, user);
                }
                else
                {
                    var createdPermissionIds = createPermissionResults.Where(i => i.Succeed).Select(i => (string)i.Data);
                    createdPermissionIds.AsParallel().ForAll(id =>
                    {
                        _dbContext.UiPermissions.DeleteOne(i => i.Id == id);
                    });

                    result.Succeed = false;
                    result.ErrorMessage = "Some of the permissions cannot be created";
                    return result;
                }

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }
        private ResultModel AddUserUIPermissions(string userId, List<Guid> ids)
        {
            var result = new ResultModel();
            try
            {
                var user = _dbContext.Users.Find(i => i.Id == userId).FirstOrDefault();
                if (user == null)
                {
                    result.ErrorMessage = "User is not existed";
                    return result;
                }

                if (ids.All(id =>
                {
                    return _dbContext.UiPermissions.Find(x => x.Id == id.ToString()).Any();
                }))
                {
                    var unduplicateUIPermissions = ids.Select(id => id.ToString()).Except(user.UiPermissionIds);
                    user.UiPermissionIds.AddRange(unduplicateUIPermissions);
                    _dbContext.Users.FindOneAndReplace(i => i.Id == user.Id, user);
                }
                else
                {
                    result.Succeed = false;
                    result.ErrorMessage = "One of them is not existed";
                    return result;
                }

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        private ResultModel AddRoleUIPermissions(string roleId, List<Guid> ids)
        {
            var result = new ResultModel();
            try
            {
                var role = _dbContext.Roles.Find(i => i.Id == roleId).FirstOrDefault();
                if (role == null)
                {
                    result.ErrorMessage = "Role is not existed";
                    return result;
                }

                if (ids.All(id =>
                {
                    return _dbContext.UiPermissions.Find(x => x.Id == id.ToString()).Any();
                }))
                {
                    var unduplicates = ids.Select(id => id.ToString()).Except(role.UiPermissionIds);
                    role.UiPermissionIds.AddRange(unduplicates);
                    _dbContext.Roles.FindOneAndReplace(i => i.Id == role.Id, role);
                }
                else
                {
                    result.Succeed = false;
                    result.ErrorMessage = "One of them is not existed";
                    return result;
                }

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        private ResultModel AddGroupUIPermissions(string groupId, List<Guid> ids)
        {
            var result = new ResultModel();
            try
            {
                var group = _dbContext.Groups.Find(i => i.Id == groupId).FirstOrDefault();
                if (group == null)
                {
                    result.ErrorMessage = "Group is not existed";
                    return result;
                }

                if (ids.All(id =>
                {
                    return _dbContext.UiPermissions.Find(x => x.Id == id.ToString()).Any();
                }))
                {
                    var unduplicates = ids.Select(id => id.ToString()).Except(group.UiPermissionIds);
                    group.UiPermissionIds.AddRange(unduplicates);
                    _dbContext.Groups.FindOneAndReplace(i => i.Id == group.Id, group);
                }
                else
                {
                    result.Succeed = false;
                    result.ErrorMessage = "One of them is not existed";
                    return result;
                }

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }
        #endregion

        public ResultModel RemovePermission(string permissionId, bool isResourcePermission, string holderId, HolderType holder)
        {
            ResultModel result = new ResultModel();
            switch (holder)
            {
                case HolderType.User: result = RemoveUserPermission(permissionId, holderId, isResourcePermission); break;
                case HolderType.Group: result = RemoveGroupPermission(permissionId, holderId, isResourcePermission); break;
                case HolderType.Role: result = RemoveRolePermission(permissionId, holderId, isResourcePermission); break;
            }
            return result;
        }
        private ResultModel RemoveUserPermission(string permissionId, string userId, bool isResourcePermission)
        {
            var result = new ResultModel();
            try
            {
                var user = _dbContext.Users.Find(i => i.Id == userId).FirstOrDefault();
                if (user == null)
                {
                    result.ErrorMessage = "User is not existed";
                    return result;
                }
                if (isResourcePermission)
                {
                    user.ResourcePermissionIds.Remove(permissionId);
                }
                else
                {
                    user.UiPermissionIds.Remove(permissionId);
                }

                _dbContext.Users.FindOneAndReplace(i => i.Id == user.Id, user);
                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }
        private ResultModel RemoveGroupPermission(string permissionId, string groupId, bool isResourcePermission)
        {
            var result = new ResultModel();
            try
            {
                var group = _dbContext.Groups.Find(i => i.Id == groupId).FirstOrDefault();
                if (group == null)
                {
                    result.ErrorMessage = "Group is not existed";
                    return result;
                }
                if (isResourcePermission)
                {
                    group.ResourcePermissionIds.Remove(permissionId);
                }
                else
                {
                    group.UiPermissionIds.Remove(permissionId);
                }

                _dbContext.Groups.FindOneAndReplace(i => i.Id == group.Id, group);
                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }
        private ResultModel RemoveRolePermission(string permissionId, string roleId, bool isResourcePermission)
        {
            var result = new ResultModel();
            try
            {
                var role = _dbContext.Roles.Find(i => i.Id == roleId).FirstOrDefault();
                if (role == null)
                {
                    result.ErrorMessage = "Role is not existed";
                    return result;
                }
                if (isResourcePermission)
                {
                    role.ResourcePermissionIds.Remove(permissionId);
                }
                else
                {
                    role.UiPermissionIds.Remove(permissionId);
                }

                _dbContext.Roles.FindOneAndReplace(i => i.Id == role.Id, role);
                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        #region Get ResourcePermission
        public List<ResourcePermissionModel> GetResourcePermissions(string holderId, HolderType holder)
        {
            var result = new List<ResourcePermissionModel>();
            switch (holder)
            {
                case HolderType.Group: result = GetGroupResourcePermissions(holderId); break;
                case HolderType.User: result = GetUserResourcePermissions(holderId); break;
                case HolderType.Role: result = GetRoleResourcePermissions(holderId); break;
            }
            return result;
        }
        public List<ResourcePermissionModel> GetUserResourcePermissions(string userId)
        {
            var result = new List<ResourcePermissionModel>();
            try
            {
                var user = _dbContext.Users.Find(i => i.Id == userId).FirstOrDefault();
                if (user != null)
                {
                    var permissionFilters = Builders<ResourcePermission>.Filter.In(i => i.Id, user.ResourcePermissionIds);
                    var permissions = _dbContext.ResourcePermissions.Find(permissionFilters).ToList();

                    if (user.GroupIds.Any())
                    {
                        var groupFilters = Builders<Group>.Filter.In(i => i.Id, user.GroupIds);
                        var groups = _dbContext.Groups.Find(groupFilters).ToList();

                        permissionFilters = Builders<ResourcePermission>.Filter.In(i => i.Id, groups.SelectMany(i => i.ResourcePermissionIds)) & Builders<ResourcePermission>.Filter.Eq(i => i.PermissionType, PermissionType.Allow);
                        var groupPermissions = _dbContext.ResourcePermissions.Find(permissionFilters).ToList();
                        permissions.AddRange(groupPermissions);
                    }

                    if (user.RoleIds.Any())
                    {
                        #region permissions in user's roles
                        var roleFilters = Builders<Role>.Filter.In(i => i.Id, user.RoleIds);
                        var roles = _dbContext.Roles.Find(roleFilters).ToList();

                        permissionFilters = Builders<ResourcePermission>.Filter.In(i => i.Id, roles.SelectMany(i => i.ResourcePermissionIds)) & Builders<ResourcePermission>.Filter.Eq(i => i.PermissionType, PermissionType.Allow);
                        var rolePermissions = _dbContext.ResourcePermissions.Find(permissionFilters).ToList();
                        permissions.AddRange(rolePermissions);
                        #endregion

                        #region permissions in role's groups
                        var groupsOfRolesIds = roles.SelectMany(i => i.GroupIds);

                        if (groupsOfRolesIds.Any())
                        {
                            var groupFilters = Builders<Group>.Filter.In(i => i.Id, groupsOfRolesIds);
                            var groups = _dbContext.Groups.Find(groupFilters).ToList();

                            permissionFilters = Builders<ResourcePermission>.Filter.In(i => i.Id, groups.SelectMany(i => i.ResourcePermissionIds)) & Builders<ResourcePermission>.Filter.Eq(i => i.PermissionType, PermissionType.Allow);
                            var groupPermissions = _dbContext.ResourcePermissions.Find(permissionFilters).ToList();
                            permissions.AddRange(groupPermissions);
                        }

                        #endregion
                    }

                    permissions = permissions.DistinctBy(i => i.Id).ToList();

                    result = _mapper.Map<List<ResourcePermission>, List<ResourcePermissionModel>>(permissions);
                }
                else
                {
                    throw new Exception("UserId is not exist!");
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.InnerException != null ? e.InnerException.Message : e.Message);
            }
            return result;
        }
        public List<ResourcePermissionModel> GetGroupResourcePermissions(string groupId)
        {
            var result = new List<ResourcePermissionModel>();
            try
            {
                var group = _dbContext.Groups.Find(i => i.Id == groupId).FirstOrDefault();
                if (group != null)
                {
                    var permissionFilters = Builders<ResourcePermission>.Filter.In(i => i.Id, group.ResourcePermissionIds);

                    var permissions = _dbContext.ResourcePermissions.Find(permissionFilters).ToList();

                    result = _mapper.Map<List<ResourcePermission>, List<ResourcePermissionModel>>(permissions);
                }
                else
                {
                    throw new Exception("GroupId is not exist!");
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.InnerException != null ? e.InnerException.Message : e.Message);
            }
            return result;
        }
        public List<ResourcePermissionModel> GetRoleResourcePermissions(string roleId)
        {
            var result = new List<ResourcePermissionModel>();
            try
            {
                var role = _dbContext.Roles.Find(i => i.Id == roleId).FirstOrDefault();
                if (role != null)
                {
                    var permissionFilters = Builders<ResourcePermission>.Filter.In(i => i.Id, role.ResourcePermissionIds);
                    var permissions = _dbContext.ResourcePermissions.Find(permissionFilters).ToList();

                    if (role.GroupIds.Any())
                    {
                        var groupFilters = Builders<Group>.Filter.In(i => i.Id, role.GroupIds);
                        var groups = _dbContext.Groups.Find(groupFilters).ToList();

                        permissionFilters = Builders<ResourcePermission>.Filter.In(i => i.Id, groups.SelectMany(i => i.ResourcePermissionIds)) & Builders<ResourcePermission>.Filter.Eq(i => i.PermissionType, PermissionType.Allow);
                        var groupPermissions = _dbContext.ResourcePermissions.Find(permissionFilters).ToList();
                        permissions.AddRange(groupPermissions);
                    }

                    permissions = permissions.DistinctBy(i => i.Id).ToList();

                    result = _mapper.Map<List<ResourcePermission>, List<ResourcePermissionModel>>(permissions);
                }
                else
                {
                    throw new Exception("RoleId is not exist!");
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.InnerException != null ? e.InnerException.Message : e.Message);
            }
            return result;
        }
        #endregion

        #region Get UiPermission
        public List<UiPermissionModel> GetUiPermissions(string holderId, HolderType holder)
        {
            var result = new List<UiPermissionModel>();
            switch (holder)
            {
                case HolderType.User: result = GetUserUiPermissions(holderId); break;
                case HolderType.Group: result = GetGroupUiPermissions(holderId); break;
                case HolderType.Role: result = GetRoleUiPermissions(holderId); break;
            }
            return result;
        }
        public List<UiPermissionModel> GetUserUiPermissions(string userId)
        {
            var result = new List<UiPermissionModel>();
            try
            {
                var user = _dbContext.Users.Find(i => i.Id == userId).FirstOrDefault();
                if (user != null)
                {
                    var permissionFilters = Builders<UiPermission>.Filter.In(i => i.Id, user.UiPermissionIds);
                    var permissions = _dbContext.UiPermissions.Find(permissionFilters).ToList();

                    if (user.GroupIds.Any())
                    {
                        var groupFilters = Builders<Group>.Filter.In(i => i.Id, user.GroupIds);
                        var groups = _dbContext.Groups.Find(groupFilters).ToList();

                        permissionFilters = Builders<UiPermission>.Filter.In(i => i.Id, groups.SelectMany(i => i.UiPermissionIds)) & Builders<UiPermission>.Filter.Eq(i => i.Type, PermissionType.Allow);
                        var groupPermissions = _dbContext.UiPermissions.Find(permissionFilters).ToList();
                        permissions.AddRange(groupPermissions);
                    }

                    if (user.RoleIds.Any())
                    {
                        #region permissions in user's roles
                        var roleFilters = Builders<Role>.Filter.In(i => i.Id, user.RoleIds);
                        var roles = _dbContext.Roles.Find(roleFilters).ToList();

                        permissionFilters = Builders<UiPermission>.Filter.In(i => i.Id, roles.SelectMany(i => i.UiPermissionIds)) & Builders<UiPermission>.Filter.Eq(i => i.Type, PermissionType.Allow);
                        var rolePermissions = _dbContext.UiPermissions.Find(permissionFilters).ToList();
                        permissions.AddRange(rolePermissions);
                        #endregion

                        #region permissions in role's groups
                        var groupsOfRolesIds = roles.SelectMany(i => i.GroupIds);

                        if (groupsOfRolesIds.Any())
                        {
                            var groupFilters = Builders<Group>.Filter.In(i => i.Id, groupsOfRolesIds);
                            var groups = _dbContext.Groups.Find(groupFilters).ToList();

                            permissionFilters = Builders<UiPermission>.Filter.In(i => i.Id, groups.SelectMany(i => i.UiPermissionIds)) & Builders<UiPermission>.Filter.Eq(i => i.Type, PermissionType.Allow);
                            var groupPermissions = _dbContext.UiPermissions.Find(permissionFilters).ToList();
                            permissions.AddRange(groupPermissions);
                        }

                        #endregion
                    }

                    permissions = permissions.DistinctBy(i => i.Id).ToList();

                    result = _mapper.Map<List<UiPermission>, List<UiPermissionModel>>(permissions);
                }
                else
                {
                    throw new Exception("UserId is not exist!");
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.InnerException != null ? e.InnerException.Message : e.Message);
            }
            return result;
        }
        public List<UiPermissionModel> GetGroupUiPermissions(string groupId)
        {
            var result = new List<UiPermissionModel>();
            try
            {
                var group = _dbContext.Groups.Find(i => i.Id == groupId).FirstOrDefault();
                if (group != null)
                {
                    var permissionFilters = Builders<UiPermission>.Filter.In(i => i.Id, group.UiPermissionIds);

                    var permissions = _dbContext.UiPermissions.Find(permissionFilters).ToList();

                    result = _mapper.Map<List<UiPermission>, List<UiPermissionModel>>(permissions);
                }
                else
                {
                    throw new Exception("GroupId is not exist!");
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.InnerException != null ? e.InnerException.Message : e.Message);
            }
            return result;
        }
        public List<UiPermissionModel> GetRoleUiPermissions(string roleId)
        {
            var result = new List<UiPermissionModel>();
            try
            {
                var role = _dbContext.Roles.Find(i => i.Id == roleId).FirstOrDefault();
                if (role != null)
                {
                    var permissionFilters = Builders<UiPermission>.Filter.In(i => i.Id, role.UiPermissionIds);
                    var permissions = _dbContext.UiPermissions.Find(permissionFilters).ToList();

                    if (role.GroupIds.Any())
                    {
                        var groupFilters = Builders<Group>.Filter.In(i => i.Id, role.GroupIds);
                        var groups = _dbContext.Groups.Find(groupFilters).ToList();

                        permissionFilters = Builders<UiPermission>.Filter.In(i => i.Id, groups.SelectMany(i => i.UiPermissionIds)) & Builders<UiPermission>.Filter.Eq(i => i.Type, PermissionType.Allow);
                        var groupPermissions = _dbContext.UiPermissions.Find(permissionFilters).ToList();
                        permissions.AddRange(groupPermissions);
                    }

                    permissions = permissions.DistinctBy(i => i.Id).ToList();

                    result = _mapper.Map<List<UiPermission>, List<UiPermissionModel>>(permissions);
                }
                else
                {
                    throw new Exception("RoleId is not exist!");
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.InnerException != null ? e.InnerException.Message : e.Message);
            }
            return result;
        }
        #endregion

        #region Get All
        public ResultModel GetAllUiPermission()
        {
            var result = new ResultModel();
            try
            {
                var permissions = _dbContext.UiPermissions.Find(i => true).ToList();
                result.Data = _mapper.Map<List<UiPermission>, List<UiPermissionModel>>(permissions);
                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        public ResultModel GetAllResourcePermission()
        {
            var result = new ResultModel();
            try
            {
                var permissions = _dbContext.ResourcePermissions.Find(i => true).ToList();
                result.Data = _mapper.Map<List<ResourcePermission>, List<ResourcePermissionModel>>(permissions);
                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }
        #endregion

        public ResultModel Validate(ResourcePermissionValidationModel model, string userId)
        {
            var caches = GetFromCache().Result;
            var result = new ResultModel();
            #region Unauthorized API
            var segments = GetSegments(model.ApiPath);
            var transformSegments = TransformSegments(segments, false);
            string formattedUrl = string.Join("/", transformSegments);

            // from DB
            //var unAuthorizedPermissionFilters = Builders<ResourcePermission>.Filter.Eq(x => x.IsAuthorizedAPI, false)
            //    & Builders<ResourcePermission>.Filter.Eq(x => x.NormalizedMethod, model.Method.ToUpper())
            //    & Builders<ResourcePermission>.Filter.Eq(x => x.PermissionType, PermissionType.Allow)
            //    & Builders<ResourcePermission>.Filter.Eq(x => x.NormalizedUrl, formattedUrl.ToUpper())
            //    ;

            //var unAuthorizedPermissionCount = _dbContext.ResourcePermissions
            //    .Find(unAuthorizedPermissionFilters).CountDocuments();

            // from Cache
            var unAuthorizedPermissionCount = caches.Where(x => x.IsAuthorizedAPI == false
            && x.NormalizedMethod == model.Method.ToUpper()
            && x.PermissionType == PermissionType.Allow
            && x.NormalizedUrl == formattedUrl.ToUpper()
            ).Count();

            result.Succeed = unAuthorizedPermissionCount > 0;
            if (result.Succeed)
            {
                result.Data = "success unauthorized";
                return result;
            } //return if API is not authorized API
            #endregion
            #region Authorized API
            //validate token
            using var scope = _scopeFactory.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            result = userService.ValidateTokenCredential(userId, model.TokenCredential).Result;
            if (!result.Succeed)
            {
                result.Data = $"failed validate token: {result.ErrorMessage}";
                return result;
            }
            var user = (UserInformation)result.Data;

            //validate permission
            if (user != null)
            {
                if (IsSpecialUser(user.NormalizedUsername))
                {
                    result.Data = "success superadmin";
                    result.Succeed = true;
                    return result;
                }
                var groupService = scope.ServiceProvider.GetRequiredService<IGroupService>();
                //var groupFilters = Builders<Group>.Filter.In(i => i.Id, user.GroupIds);
                //var groupsOfUser = _dbContext.Groups.Find(groupFilters).ToList();
                var groupsOfUser = groupService.GetFromCache().Result.Where(x => user.GroupIds.Contains(x.Id)).ToList();

                var roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();
                //var roleFilters = Builders<Role>.Filter.In(i => i.Id, user.RoleIds);
                //var rolesOfUser = _dbContext.Roles.Find(roleFilters).ToList();
                var rolesOfUser = roleService.GetFromCache().Result.Where(x => user.RoleIds.Contains(x.Id)).ToList();

                var permissionIds = new List<string>();
                groupsOfUser.ForEach(group =>
                {
                    group.ResourcePermissionIds.ForEach(s =>
                    {
                        if (!permissionIds.Any(p => p == s))
                        {
                            permissionIds.Add(s);
                        }
                    });
                });

                rolesOfUser.ForEach(role =>
                {
                    role.ResourcePermissionIds.ForEach(s =>
                    {
                        if (!permissionIds.Any(p => p == s))
                        {
                            permissionIds.Add(s);
                        }
                    });
                });

                user.ResourcePermissionIds.ForEach(s =>
                {
                    if (!permissionIds.Any(p => p == s))
                    {
                        permissionIds.Add(s);
                    }
                });
                //var permissionFilters = Builders<ResourcePermission>.Filter.Eq(x => x.IsAuthorizedAPI, true)
                //    & Builders<ResourcePermission>.Filter.In(p => p.Id, permissionIds)
                //      & Builders<ResourcePermission>.Filter.Eq(x => x.NormalizedMethod, model.Method.ToUpper())
                //      & Builders<ResourcePermission>.Filter.Eq(x => x.PermissionType, PermissionType.Allow)
                //      & Builders<ResourcePermission>.Filter.Eq(x => x.NormalizedUrl, formattedUrl.ToUpper())
                //      ;
                //var permissionCount = _dbContext.ResourcePermissions.Find(permissionFilters).CountDocuments();

                //from Cache
                var permissionCount = caches.Where(x => x.IsAuthorizedAPI == true
                    && permissionIds.Contains(x.Id)
                    && x.NormalizedMethod == model.Method.ToUpper()
                    && x.PermissionType == PermissionType.Allow
                    && x.NormalizedUrl == formattedUrl.ToUpper()
                    ).Count();
                result.Data = "success authorized";
                result.Succeed = permissionCount > 0;
                return result;
            }

            return result;
            #endregion
        }

        private bool IsSpecialUser(string normalizedUsername)
        {
            return normalizedUsername == SUPER_ADMIN_ROLE;
        }

        private List<string> GetSegments(string apiPath)
        {
            var segments = apiPath.Split("/").ToList();
            return segments;
        }

        private bool SegmentsEqual(List<string> segment1, List<string> segment2)
        {
            segment1 = TransformSegments(segment1);
            segment2 = TransformSegments(segment2);

            return segment1.SequenceEqual(segment2);
        }

        private List<string> TransformSegments(List<string> segments, bool isToUpper = true)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var transformedSegment = segments[i].Replace($"/", "");
                if (!string.IsNullOrEmpty(transformedSegment))
                {
                    segments[i] = WebUtility.UrlDecode(segments[i]);

                    var isInt = int.TryParse(transformedSegment, out var o);
                    var isGuid = Guid.TryParse(transformedSegment, out var g);
                    var isDate = DateTime.TryParseExact(transformedSegment, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d);

                    if ((isInt || isGuid) || isDate || (segments[i].Contains("{") && segments[i].Contains("}")))
                    {
                        //segments[i] = "{ROUTEPARAM}";
                        segments[i] = "{P}";
                        if (i != segments.Count - 1)
                        {
                            segments[i] += $"/";
                        }
                        else
                        {
                            if (segments[i].Contains("/"))
                            {
                                segments[i] = segments[i].Replace("/", "");
                            }
                        }
                    }
                    else
                    {
                        segments[i] = isToUpper ? segments[i].ToUpper() : segments[i];
                    }
                }
            }

            return segments;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiPath"></param>
        /// <returns>true if input format follows : /{something}/{anything}/{everything}/....</returns>
        private bool IsValidApiPath(string apiPath)
        {
            return Uri.TryCreate(apiPath, UriKind.Relative, out var _);
        }

        #region Create Permissions
        public ResultModel CreatePermission(ResourcePermissionCreateModel model)
        {
            var result = new ResultModel();
            try
            {
                var permission = _mapper.Map<ResourcePermissionCreateModel, ResourcePermission>(model);
                var _ = _dbContext.ResourcePermissions.InsertOneAsync(permission);

                result.Succeed = true;
                result.Data = permission.Id;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.Message;
            }
            ClearCache();
            return result;
        }


        public ResultModel CreatePermissions(List<ResourcePermissionCreateModel> models)
        {
            var result = new ResultModel();
            try
            {
                var permissions = _mapper.Map<List<ResourcePermissionCreateModel>, List<ResourcePermission>>(models);
                var _ = _dbContext.ResourcePermissions.InsertManyAsync(permissions);

                result.Succeed = true;
                result.Data = permissions.Select(i => i.Id);
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.Message;
            }
            return result;
        }

        public ResultModel CreatePermission(UiPermissionCreateModel model)
        {
            var result = new ResultModel();
            try
            {
                var permission = _mapper.Map<UiPermissionCreateModel, UiPermission>(model);
                var _ = _dbContext.UiPermissions.InsertOneAsync(permission);

                result.Succeed = true;
                result.Data = permission.Id;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.Message;
            }
            ClearCache();
            return result;
        }

        public ResultModel CreatePermissions(List<UiPermissionCreateModel> models)
        {
            var result = new ResultModel();
            try
            {
                var permissions = _mapper.Map<List<UiPermissionCreateModel>, List<UiPermission>>(models);
                var _ = _dbContext.UiPermissions.InsertManyAsync(permissions);

                result.Succeed = true;
                result.Data = permissions.Select(i => i.Id);
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.Message;
            }
            ClearCache();
            return result;
        }

        #endregion

        //Tools
        public async Task<string> FixUrlFormat()
        {
            var resources = await _dbContext.ResourcePermissions.Find(x => true).ToListAsync();
            foreach (var resource in resources)
            {
                var segments = GetSegments(resource.Url);
                var transformSegments = TransformSegments(segments, false);
                string formattedUrl = string.Join("/", transformSegments);
                resource.Url = formattedUrl;
                resource.NormalizedUrl = formattedUrl.ToUpper();
                await _dbContext.ResourcePermissions.ReplaceOneAsync(x => x.Id == resource.Id, resource);
            }
            ClearCache();
            return "success";
        }
        public async Task<string> MergeDuplicateUIPermission()
        {
            var uiPermissionGroups = _dbContext.UiPermissions.Aggregate()
                            .Group(e => e.Name, g => new { Key = g.Key, Data = g.Select(x => x.Id).ToList() })
                            .ToList();
            uiPermissionGroups.RemoveAll(x => x.Data.Count == 1);

            foreach (var uiPermissionGroup in uiPermissionGroups)
            {
                var firstUiPermissionId = uiPermissionGroup.Data.FirstOrDefault();

                uiPermissionGroup.Data.Remove(firstUiPermissionId);

                foreach (var id in uiPermissionGroup.Data)
                {
                    var userFilter = Builders<UserInformation>.Filter.AnyEq(x => x.UiPermissionIds, id);
                    var users = await _dbContext.Users.Find(userFilter).ToListAsync();
                    Parallel.ForEach(users, user =>
                    {
                        user.UiPermissionIds.Remove(id);
                        user.UiPermissionIds.Add(firstUiPermissionId);
                        _dbContext.Users.ReplaceOne(x => x.Id == user.Id, user);
                    });

                    var groupFilter = Builders<Group>.Filter.AnyEq(x => x.UiPermissionIds, id);
                    var groups = await _dbContext.Groups.Find(groupFilter).ToListAsync();
                    Parallel.ForEach(groups, group =>
                    {
                        group.UiPermissionIds.Remove(id);
                        group.UiPermissionIds.Add(firstUiPermissionId);
                        _dbContext.Groups.ReplaceOne(x => x.Id == group.Id, group);
                    });

                    var roleFilter = Builders<Role>.Filter.AnyEq(x => x.UiPermissionIds, id);
                    var roles = await _dbContext.Roles.Find(roleFilter).ToListAsync();
                    Parallel.ForEach(roles, role =>
                    {
                        role.UiPermissionIds.Remove(id);
                        role.UiPermissionIds.Add(firstUiPermissionId);
                        _dbContext.Roles.ReplaceOne(x => x.Id == role.Id, role);
                    });

                    await _dbContext.UiPermissions.DeleteOneAsync(x => x.Id == id);
                }
            }
            return "success";
        }
        public async Task<List<ResourcePermission>> GetFromCache()
        {
            var cacheContent = await _distributedCache.GetStringAsync(CacheConstants.RESOURCE_PERMISSION);
            if (cacheContent != null)
            {
                return JsonConvert.DeserializeObject<List<ResourcePermission>>(cacheContent);
            }
            else
            {
                var model = await _dbContext.ResourcePermissions.Find(x => true).ToListAsync();
                var content = JsonConvert.SerializeObject(model);
                //var options = new DistributedCacheEntryOptions()
                //    .SetAbsoluteExpiration(DateTime.Now.AddHours(24))
                //    .SetSlidingExpiration(TimeSpan.FromHours(6));
                await _distributedCache.SetStringAsync(CacheConstants.RESOURCE_PERMISSION, content);
                return model;
            }
        }
        public void ClearCache()
        {
            _distributedCache.Remove(CacheConstants.RESOURCE_PERMISSION);
            _distributedCache.Remove(CacheConstants.USER);
            _distributedCache.Remove(CacheConstants.ROLE);
            _distributedCache.Remove(CacheConstants.GROUP);
        }
    }
}
