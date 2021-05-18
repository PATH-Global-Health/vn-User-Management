using Data.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IUserService
    {
        bool IsUsernameAvailable(string username);
        bool IsEmailAvailable(string email);
        bool IsPhoneNumberAvailable(string phoneNumber);

        List<UserInformationModel> GetAll();
        ResultModel Create(UserCreateModel model);
        Task<ResultModel> Login(string username, string password, PermissionQuery permissionQuerie);
        ResultModel ChangePassword(ChangePasswordModel model, string userId);
        ResultModel ResetDefaultPassword(string username);
        ResultModel UpdateInformation(UserUpdateModel model, string userId);
        UserInformationModel GetUserInformation(string userId);
        ResultModel ImportUsers(List<ImportUserModel> model);

        List<RoleModel> GetRoles(string userId);
        List<GroupModel> GetGroups(string userId);
    }
}
