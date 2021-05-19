using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Data.ViewModels
{
    public class UserCreateModel
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string FullName { get; set; } = "";
    }

    public class ChangePasswordModel
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public class UserUpdateModel
    {
        public string FullName { get; set; }
    }

    public class UserInformationModel
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FullName { get; set; }
    }

    public class PermissionQuery
    {
        public string Type { get; set; }
        public List<string> Groups { get; set; }
        public List<string> Roles { get; set; }
    }

    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public PermissionQuery PermissionQuery { get; set; }
    }

    public class ImportUserModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class BasePermissionModel
    {
        public List<UiPermissionModel> UiPermissions { get; set; }
        public List<ResourcePermissionModel> ResourcePermissions { get; set; }
    }
    public class GenerateResetPasswordOTPModel
    {
        [Required]
        public string Username { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public List<AnswerSecurityQuestionModel> Questions { get; set; }
    }
    public class ResetPasswordModel
    {
        public string Username { get; set; }
        public string NewPassword { get; set; }
    }
    public class ConfirmResetPasswordOTPModel
    {
        public string Username { get; set; }
        public string OTP { get; set; }
    }
}
