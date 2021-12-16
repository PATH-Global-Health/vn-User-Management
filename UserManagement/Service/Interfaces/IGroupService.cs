using Data.MongoCollections;
using Data.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IGroupService
    {
        ICollection<GroupOverviewModel> GetAll();
        GroupModel Get(string id);
        ResultModel Create(GroupCreateModel model);
        ResultModel Update(string groupId, GroupUpdateModel model);
        ResultModel Delete(string groupId);

        ResultModel AddUsersByGroupName(string groupName, List<string> userIds);
        ResultModel AddUsers(string groupId, List<string> userIds);
        ResultModel RemoveUser(string groupId, string userId);

        ResultModel AddRoles(string groupId, List<string> roleIds);
        ResultModel RemoveRole(string groupId, string roleId);

        List<RoleModel> GetRoles(string id);
        List<UserInformationModel> GetUsers(string id);
        Task<List<Group>> GetFromCache();
    }
}
