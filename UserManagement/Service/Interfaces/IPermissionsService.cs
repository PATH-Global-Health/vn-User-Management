using System.Collections.Generic;
using Data.Enums;
using Data.ViewModels;

namespace Service.Interfaces
{
    public interface IPermissionsService
    {
        ResultModel CreatePermission(ResourcePermissionCreateModel model);
        ResultModel CreatePermission(UiPermissionCreateModel model);

        ResultModel RemovePermission(string permissionId, bool isResourcePermission, string holderId, HolderType holder);

        ResultModel AddPermission(string holderId, HolderType holder, ResourcePermissionCreateModel model);
        ResultModel AddPermission(string holderId, HolderType holder, UiPermissionCreateModel model);

        List<ResourcePermissionModel> GetResourcePermissions(string holderId, HolderType holder);
        List<UiPermissionModel> GetUiPermissions(string holderId, HolderType holder);
        ResultModel GetAllResourcePermission();
        ResultModel GetAllUiPermission();

        ResultModel Validate(ResourcePermissionValidationModel model, string userId);
        List<UiPermissionModel> GetUserUiPermissions(string userId);
    }
}
