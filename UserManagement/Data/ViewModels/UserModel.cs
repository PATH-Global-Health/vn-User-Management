using Data.Enums;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Data.ViewModels
{
    public class UserCreateModel
    {
        public string Username { get; set; }
        public string Password { get; set; }

        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = "";
        [DataType(DataType.PhoneNumber)]
        public string PhoneNumber { get; set; } = "";
        public string FullName { get; set; } = "";
        public bool? IsElasticSynced { get; set; }
        public string GroupName { get; set; } = "CUSTOMER";
        public bool OnlyUsername { get; set; } = false;
    }

    public class ChangePasswordModel
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public class UserUpdateModel
    {
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string OTP { get; set; }
    }

    public class UserInformationModel
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FullName { get; set; }
        public bool IsConfirmed { get; set; }
        public bool IsDisabled { get; set; }
        public bool? IsElasticSynced { get; set; } = false;
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
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
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
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
    }
    public class ResetPasswordModel
    {
        public string NewPassword { get; set; }
    }
    public class ConfirmResetPasswordOTPModel
    {
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string OTP { get; set; }
    }
    public class ConfirmResetPasswordSecurityQuestionModel
    {
        public string Username { get; set; }
        public string SecurityQuestionId { get; set; }
        public string SecurityQuestionAnswer { get; set; }
    }
    public class OTP
    {
        public string Value { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime ExpiredTime { get; set; }
        public int AccessFailedCount { get; set; } = 0;
    }
    public class ChangeSecurityQuestionAnswerModel
    {
        public string Password { get; set; }
        public AnswerSecurityQuestionModel QuestionAnswer { get; set; }
    }
    public class UserInformationWithPermissionsModel
    {
        public UserInformationModel UserInfo { get; set; }
        public List<Permission> Permissions { get; set; }
    }
    public class UpdateUserProfileViewModel
    {
        public string FullName { get; set; }
        public string Username { get; set; }
        public bool IsConfirmed { get; set; }
        public int Status { get; set; } = -1;
        public string Email { get; set; }
        public string Phone { get; set; }
        public bool IsDelete { get; set; }
    }

    public class CBOCreateModel : UserCreateModel
    {
        public bool HasSendInitialEmail { get; set; }
    }
    public class GetUserStatistic
    {
        public DateTime FromDate { get; set; } = DateTime.UtcNow.Date;
        public DateTime ToDate { get; set; } = DateTime.UtcNow.Date.AddDays(1);
    }
    public class UserStatisticModel
    {
        public long UnverifiedRegisteredUsersNumber { get; set; }
        public long VerifiedRegisteredUsersNumber { get; set; }
    }
}
