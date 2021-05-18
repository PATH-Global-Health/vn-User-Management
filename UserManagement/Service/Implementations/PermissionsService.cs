using AutoMapper;
using Data.DataAccess;
using Data.Enums;
using Data.MongoCollections;
using Data.ViewModels;
using MongoDB.Driver;
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

        public PermissionsService(ApplicationDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        #region Resource Permission
        public ResultModel AddPermission(string holderId, HolderType holder, ResourcePermissionCreateModel model)
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

                var newPermission = _mapper.Map<ResourcePermissionCreateModel, ResourcePermission>(model);
                var existedPermission = user.ResourcePermissions.FirstOrDefault(i => i.PermissionType == model.PermissionType
                                                                        && i.NormalizedUrl == model.Url.ToUpper().Trim()
                                                                        && i.NormalizedMethod == model.Method.ToUpper().Trim());
                if (existedPermission != null)
                {
                    result.ErrorMessage = $"Permission existed with Id : {existedPermission.Id}";
                    return result;
                }

                user.ResourcePermissions.Add(newPermission);
                _dbContext.Users.FindOneAndReplace(i => i.Id == user.Id, user);

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

                var newPermission = _mapper.Map<ResourcePermissionCreateModel, ResourcePermission>(model);
                var existedPermission = role.ResourcePermissions.FirstOrDefault(i => i.PermissionType == model.PermissionType
                                                                        && i.NormalizedUrl == model.Url.ToUpper().Trim()
                                                                        && i.NormalizedMethod == model.Method.ToUpper().Trim());
                if (existedPermission != null)
                {
                    result.ErrorMessage = $"Permission existed with Id : {existedPermission.Id}";
                    return result;
                }

                role.ResourcePermissions.Add(newPermission);
                _dbContext.Roles.FindOneAndReplace(i => i.Id == role.Id, role);

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

                var newPermission = _mapper.Map<ResourcePermissionCreateModel, ResourcePermission>(model);
                var existedPermission = group.ResourcePermissions.FirstOrDefault(i => i.PermissionType == model.PermissionType
                                                                        && i.NormalizedUrl == model.Url.ToUpper().Trim()
                                                                        && i.NormalizedMethod == model.Method.ToUpper().Trim());
                if (existedPermission != null)
                {
                    result.ErrorMessage = $"Permission existed with Id : {existedPermission.Id}";
                    return result;
                }

                group.ResourcePermissions.Add(newPermission);
                _dbContext.Groups.FindOneAndReplace(i => i.Id == group.Id, group);

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

                var newPermission = _mapper.Map<UiPermissionCreateModel, UiPermission>(model);
                var existedPermission = group.UiPermissions.FirstOrDefault(i => i.Type == model.PermissionType
                                                                        && i.Code == model.Code);
                if (existedPermission != null)
                {
                    result.ErrorMessage = $"Permission existed with Id : {existedPermission.Id}";
                    return result;
                }

                group.UiPermissions.Add(newPermission);
                _dbContext.Groups.FindOneAndReplace(i => i.Id == group.Id, group);

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

                var newPermission = _mapper.Map<UiPermissionCreateModel, UiPermission>(model);
                var existedPermission = role.UiPermissions.FirstOrDefault(i => i.Type == model.PermissionType
                                                                        && i.Code == model.Code);
                if (existedPermission != null)
                {
                    result.ErrorMessage = $"Permission existed with Id : {existedPermission.Id}";
                    return result;
                }

                role.UiPermissions.Add(newPermission);
                _dbContext.Roles.FindOneAndReplace(i => i.Id == role.Id, role);

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

                var newPermission = _mapper.Map<UiPermissionCreateModel, UiPermission>(model);
                var existedPermission = user.UiPermissions.FirstOrDefault(i => i.Type == model.PermissionType
                                                                        && i.Code == model.Code);
                if (existedPermission != null)
                {
                    result.ErrorMessage = $"Permission existed with Id : {existedPermission.Id}";
                    return result;
                }

                user.UiPermissions.Add(newPermission);
                _dbContext.Users.FindOneAndReplace(i => i.Id == user.Id, user);

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
                    user.ResourcePermissions.RemoveAll(i => i.Id == permissionId);
                }
                else
                {
                    user.UiPermissions.RemoveAll(i => i.Id == permissionId);
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
                    group.ResourcePermissions.RemoveAll(i => i.Id == permissionId);
                }
                else
                {
                    group.UiPermissions.RemoveAll(i => i.Id == permissionId);
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
                    role.ResourcePermissions.RemoveAll(i => i.Id == permissionId);
                }
                else
                {
                    role.UiPermissions.RemoveAll(i => i.Id == permissionId);
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
                    result = _mapper.Map<List<ResourcePermission>, List<ResourcePermissionModel>>(user.ResourcePermissions);
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
                    result = _mapper.Map<List<ResourcePermission>, List<ResourcePermissionModel>>(group.ResourcePermissions);
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
                    result = _mapper.Map<List<ResourcePermission>, List<ResourcePermissionModel>>(role.ResourcePermissions);
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
                    result = _mapper.Map<List<UiPermission>, List<UiPermissionModel>>(user.UiPermissions);
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
                    result = _mapper.Map<List<UiPermission>, List<UiPermissionModel>>(group.UiPermissions);
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
                    result = _mapper.Map<List<UiPermission>, List<UiPermissionModel>>(role.UiPermissions);
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
                var validUri = SegmentsEqual(model.Uri.Segments.ToList(), new Uri(user.ResourcePermissions[2].NormalizedUrl).Segments.ToList());



                Parallel.ForEach(Partitioner.Create(user.ResourcePermissions), (permission, state) =>
                {
                    var validUri = SegmentsEqual(model.Uri.Segments.ToList(), new Uri(permission.NormalizedUrl).Segments.ToList());
                    var validMethod = model.Method.ToUpper() == permission.NormalizedMethod;
                    var allowedPermission = permission.PermissionType == PermissionType.Allow;

                    if (validUri && validMethod && allowedPermission)
                    {
                        result.Succeed = true;
                        state.Stop();
                    }
                });

                return result;
            }

            return result;
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
    }
}
