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

        Task<PagingModel> GetAll(string name, int? pageSize, int? pageIndex);
        Task<ResultModel> Create(UserCreateModel model);
        Task<ResultModel> Login(LoginModel model);
        Task<ResultModel> AnonymousLogin();
        Task<ResultModel> ChangePasswordAsync(ChangePasswordModel model, string userId);
        Task<ResultModel> UpdateUser(UserUpdateModel model, string userId);
        Task<ResultModel> ResetDefaultPasswordAsync(string username);
        ResultModel UpdateInformation(UserUpdateModel model, string userId);
        UserInformationModel GetUserInformation(string userId);
        ResultModel ImportUsers(List<ImportUserModel> model);

        Task<ResultModel> SendOTPVerification(string phoneNumber);
        Task<ResultModel> VerifyOTPOfPhoneNumber(VerifyOTPOfPhoneNumberRequest request);
        Task<ResultModel> ChangeSecurityQuestionAnswer(ChangeSecurityQuestionAnswerModel model, string username);
        Task<ResultModel> SendUpdateUserOTP(SendOTPRequest request, string username);
        Task<ResultModel> GenerateResetPasswordOTP(GenerateResetPasswordOTPModel model);
        Task<ResultModel> ConfirmResetPasswordOTP(ConfirmResetPasswordOTPModel model);
        Task<ResultModel> ConfirmResetPasswordSecurityQuestion(ConfirmResetPasswordSecurityQuestionModel model);
        Task<ResultModel> ResetPassword(ResetPasswordModel model, string username);
        Task<ResultModel> GetUserInfoAsync(string username);

        List<RoleModel> GetRoles(string userId);
        List<GroupModel> GetGroups(string userId);

        ResultModel DisableUser(string userId);
        ResultModel EnableUser(string userId);

        Task<ResultModel> ValidateTokenCredential(string userId, string hashedCredential);
        void UpdateTokenCredentail();
        Task<ResultModel> LoginWithFacebookAsync(string accessToken);
        Task<ResultModel> LoginWithGoogleAsync(string idToken);
        Task<ResultModel> IsConfirmdUser(string username);
        void TestRabbitMQ(string username);
        Task<ResultModel> CreateOrUpdateUserElasticSearch(string fullname, string username, string password, string email);
    }
}
