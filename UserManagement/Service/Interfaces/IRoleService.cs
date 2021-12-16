using Data.MongoCollections;
using Data.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IRoleService
    {
        ICollection<RoleModel> GetAll();
        RoleModel Get(string id);
        ResultModel Create(RoleCreateModel model);
        ResultModel Update(string roleId, RoleUpdateModel model);
        ResultModel Delete(string roleId);

        ResultModel AddUsers(string roleId, List<string> userIds);
        ResultModel RemoveUser(string roleId, string userId);
        List<UserInformationModel> GetUsers(string roleId);
        List<GroupModel> GetGroups(string roleId);
        Task<List<Role>> GetFromCache();
    }
}
