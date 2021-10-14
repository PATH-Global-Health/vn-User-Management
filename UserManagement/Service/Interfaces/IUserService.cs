using Data.ViewModels;
using Data.ViewModels.Users;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IUserService
    {
        bool IsUsernameAvailable(string username);
        bool IsEmailAvailable(string email);
        bool IsPhoneNumberAvailable(string phoneNumber);

        Task<List<UserInformationModel>> GetAllAsync(string keyword);
        Task<ResultModel> Create(UserCreateModel model);
        Task<ResultModel> Login(LoginModel model);
        Task<ResultModel> AnonymousLogin();
        ResultModel ChangePassword(ChangePasswordModel model, string userId);
        Task<ResultModel> UpdateUser(UserUpdateModel model, string userId);
        ResultModel ResetDefaultPassword(string username);
        ResultModel UpdateInformation(UserUpdateModel model, string userId);
        UserInformationModel GetUserInformation(string userId);
        ResultModel ImportUsers(List<ImportUserModel> model);

        Task<ResultModel> SendOTPVerification(string phoneNumber);
        Task<ResultModel> VerifyOTPOfPhoneNumber(VerifyOTPOfPhoneNumberRequest request);
        Task<ResultModel> ChangeSecurityQuestionAnswer(ChangeSecurityQuestionAnswerModel model, string username);
        Task<ResultModel> GenerateResetPasswordOTP(GenerateResetPasswordOTPModel model);
        Task<ResultModel> ConfirmResetPasswordOTP(ConfirmResetPasswordOTPModel model);
        Task<ResultModel> ConfirmResetPasswordSecurityQuestion(ConfirmResetPasswordSecurityQuestionModel model);
        Task<ResultModel> ResetPassword(ResetPasswordModel model, string username);
        Task<ResultModel> GetUserInfoAsync(string username);

        List<RoleModel> GetRoles(string userId);
        List<GroupModel> GetGroups(string userId);
        Task<ResultModel> LoginWithFacebookAsync(string accessToken);
        Task<ResultModel> LoginWithGoogleAsync(string idToken);
    }
}
