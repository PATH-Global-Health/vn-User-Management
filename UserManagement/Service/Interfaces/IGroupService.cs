using Data.ViewModels;
using System.Collections.Generic;

namespace Service.Interfaces
{
    public interface IGroupService
    {
        ICollection<GroupUpdateModel> GetAll();
        GroupModel Get(string id);
        ResultModel Create(GroupCreateModel model);
        ResultModel Update(GroupUpdateModel model);
        ResultModel Delete(string groupId);

        ResultModel AddUsers(string groupId, List<string> userIds);
        ResultModel RemoveUser(string groupId, string userId);

        ResultModel AddRoles(string groupId, List<string> roleIds);
        ResultModel RemoveRole(string groupId, string roleId);

        List<RoleModel> GetRoles(string id);
        List<UserInformationModel> GetUsers(string id);
    }
}
