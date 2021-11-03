﻿using AutoMapper;
using Data.DataAccess;
using Data.Enums;
using Data.MongoCollections;
using Data.ViewModels;
using MongoDB.Driver;
using MoreLinq;
using Service.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Service.Implementations
{
    public class PermissionsService : IPermissionsService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;

        #region Special Roles
        private const string SUPER_ADMIN_ROLE = "SUPERADMIN";
        #endregion

        public PermissionsService(ApplicationDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
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
            var result = new ResultModel();

            var user = _dbContext.Users.Find(i => i.Id == userId).FirstOrDefault();
            if (user != null)
            {
                if (IsSpecialUser(user))
                {
                    result.Succeed = true;
                    return result;
                }

                var _lock = new object();
                Parallel.ForEach(Partitioner.Create(user.ResourcePermissionIds), (permissionId, state) =>
                {
                    var permission = _dbContext.ResourcePermissions.Find(i => i.Id == permissionId).FirstOrDefault();
                    if (permission == null) return;

                    var validUri = SegmentsEqual(GetSegments(model.ApiPath), GetSegments(permission.NormalizedUrl));
                    var validMethod = model.Method.ToUpper() == permission.NormalizedMethod;
                    var allowedPermission = permission.PermissionType == PermissionType.Allow;

                    if (validUri && validMethod && allowedPermission)
                    {
                        lock (_lock)
                        {
                            result.Succeed = true;
                            state.Stop();
                        }
                    }
                });

                return result;
            }

            return result;
        }

        private bool IsSpecialUser(UserInformation user)
        {
            return user.NormalizedUsername == SUPER_ADMIN_ROLE;
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

        private List<string> TransformSegments(List<string> segments)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var transformedSegment = segments[i].Replace($"/", "");
                if (!string.IsNullOrEmpty(transformedSegment))
                {
                    segments[i] = WebUtility.UrlDecode(segments[i]);

                    var isInt = int.TryParse(transformedSegment, out var o);
                    var isGuid = Guid.TryParse(transformedSegment, out var g);

                    if ((isInt || isGuid) || (segments[i].Contains("{") && segments[i].Contains("}")))
                    {
                        segments[i] = "{ROUTEPARAM}";
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
                        segments[i] = segments[i].ToUpper();
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
            return result;
        }

        #endregion


    }
}
