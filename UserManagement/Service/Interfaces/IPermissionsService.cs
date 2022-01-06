using Data.Enums;
using Data.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IPermissionsService
    {
        ResultModel CreatePermission(ResourcePermissionCreateModel model);
        ResultModel CreatePermission(UiPermissionCreateModel model);

        ResultModel RemovePermission(string permissionId, bool isResourcePermission, string holderId, HolderType holder);

        ResultModel AddPermission(string holderId, HolderType holder, ResourcePermissionCreateModel model);
        ResultModel AddPermission(string holderId, HolderType holder, UiPermissionCreateModel model);
        ResultModel AddResourcePermissions(string holderId, HolderType holderType, List<Guid> permissionIds);
        ResultModel ChangeAPIAuthorizationResourcePermission(string id, bool isAuthorized);

        List<ResourcePermissionModel> GetResourcePermissions(string holderId, HolderType holder);
        List<UiPermissionModel> GetUiPermissions(string holderId, HolderType holder);
        List<UiPermissionModel> GetUserUiPermissions(string userId);
        ResultModel GetAllResourcePermission();
        ResultModel GetAllUiPermission();

        ResultModel Validate(ResourcePermissionValidationModel model, string userId);

        ResultModel AddPermissions(string holderId, HolderType holderType, List<UiPermissionCreateModel> permissions);
        ResultModel AddPermissions(string holderId, HolderType holderType, List<ResourcePermissionCreateModel> permissions);
        ResultModel AddUIPermissions(string holderId, HolderType holderType, List<Guid> uiIds);

        ResultModel CreatePermissions(List<UiPermissionCreateModel> models);
        ResultModel CreatePermissions(List<ResourcePermissionCreateModel> models);
        Task<string> FixUrlFormat();
        Task<string> MergeDuplicateUIPermission();
    }
}
