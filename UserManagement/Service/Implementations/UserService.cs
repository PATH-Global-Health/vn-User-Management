using AutoMapper;
using Data.DataAccess;
using Data.MongoCollections;
using Data.ViewModels;
using Data.ViewModels.ProfileAPIs;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Service.Helper;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Service.Implementations
{
    public class UserService : IUserService
    {
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IFacebookAuthService _facebookAuthService;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IMailService _mailService;
        private readonly IScheduleManagementAPIService _scheduleManagementAPIService;
        public UserService(IMapper mapper, IConfiguration configuration, ApplicationDbContext dbContext, IHttpClientFactory httpClientFactory, IMailService mailService, IFacebookAuthService facebookAuthService, IGoogleAuthService googleAuthService, IScheduleManagementAPIService scheduleManagementAPIService)
        {
            _mapper = mapper;
            _configuration = configuration;
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
            _mailService = mailService;
            _facebookAuthService = facebookAuthService;
            _googleAuthService = googleAuthService;
            _scheduleManagementAPIService = scheduleManagementAPIService;
        }

        public ResultModel ChangePassword(ChangePasswordModel model, string userId)
        {
            var result = new ResultModel();
            try
            {
                var user = _dbContext.Users.Find(i => i.Id == userId).FirstOrDefault();
                if (user == null)
                {
                    result.ErrorMessage = "Invalid Login Token";
                    return result;
                }
                var passwordHasher = new PasswordHasher<UserInformation>();
                var passwordVerificationResult = passwordHasher.VerifyHashedPassword(user, user.HashedPassword, model.OldPassword);
                if (passwordVerificationResult == PasswordVerificationResult.Failed)
                {
                    result.ErrorMessage = "Current Password is incorrect";
                    return result;
                }

                user.HashedPassword = passwordHasher.HashPassword(user, model.NewPassword);
                _dbContext.Users.ReplaceOne(i => i.Id == user.Id, user);

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        public async Task<ResultModel> Create(UserCreateModel model)
        {
            var result = new ResultModel();
            try
            {
                #region Keys validation
                if (!IsUsernameAvailable(model.Username))
                {
                    result.ErrorMessage = "Username is not available";
                    return result;
                }
                if (!IsEmailAvailable(model.Email))
                {
                    result.ErrorMessage = "Email is not available";
                    return result;
                }
                if (!IsPhoneNumberAvailable(model.PhoneNumber))
                {
                    result.ErrorMessage = "Phone Number is not available";
                    return result;
                }
                #endregion

                var passwordHasher = new PasswordHasher<UserInformation>();
                var user = new UserInformation
                {
                    Username = model.Username,
                    NormalizedUsername = model.Username.ToUpper(),
                    Email = model.Email,
                    NormalizedEmail = string.IsNullOrEmpty(model.Email) ? "" : model.Email.ToUpper(),
                    PhoneNumber = model.PhoneNumber,
                    FullName = model.FullName,
                };
                user.HashedPassword = passwordHasher.HashPassword(user, model.Password);

                _dbContext.Users.InsertOne(user);

                // Create Profile when register successfully
                var token = GetAccessToken(user);
                await _scheduleManagementAPIService.CreateProfile(token.Access_token, new CreateProfileRequest
                {
                    email = user.Email,
                    phoneNumber = user.PhoneNumber,
                    fullname = user.FullName
                });

                result.Succeed = true;
                result.Data = user.Id;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }

            return result;
        }

        public UserInformationModel GetUserInformation(string userId)
        {
            var user = _dbContext.Users.Find(u => u.Id == userId).FirstOrDefault();
            if (user == null)
            {
                throw new Exception("User is not exist!");
            }
            var result = _mapper.Map<UserInformation, UserInformationModel>(user);
            return result;
        }

        public bool IsEmailAvailable(string email)
        {
            if (string.IsNullOrEmpty(email)) return true;
            email = email.ToUpper();
            var user = _dbContext.Users.Find(i => i.NormalizedEmail == email).FirstOrDefault();
            return user == null;
        }

        public bool IsPhoneNumberAvailable(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber)) return true;
            var user = _dbContext.Users.Find(i => i.PhoneNumber == phoneNumber).FirstOrDefault();
            return user == null;
        }

        public bool IsUsernameAvailable(string username)
        {
            if (string.IsNullOrEmpty(username)) return false;
            username = username.ToUpper();
            var user = _dbContext.Users.Find(i => i.NormalizedUsername == username).FirstOrDefault();
            return user == null;
        }

        public async Task<ResultModel> Login(LoginModel model)
        {
            var result = new ResultModel();
            try
            {
                UserInformation user = null;
                if (!string.IsNullOrEmpty(model.Email))
                {
                    model.Email = model.Email?.ToUpper();
                    user = _dbContext.Users.Find(i => i.NormalizedEmail == model.Email).FirstOrDefault();
                }
                else if (!string.IsNullOrEmpty(model.PhoneNumber))
                {
                    user = _dbContext.Users.Find(i => i.PhoneNumber == model.PhoneNumber).FirstOrDefault();
                }
                else if (!string.IsNullOrEmpty(model.Username))
                {
                    model.Username = model.Username?.ToUpper();
                    user = _dbContext.Users.Find(i => i.NormalizedUsername == model.Username).FirstOrDefault();
                }
                if (user == null)
                {
                    #region Check on old system **Disabled**
                    if (await UserIsOnOldLoginSystem(model.Username, model.Password))
                    {
                        var userCreateModel = new UserCreateModel
                        {
                            Username = model.Username,
                            Password = model.Password,
                            FullName = model.Username
                        };
                        var createUserResult = await Create(userCreateModel);
                        if (createUserResult.Succeed)
                        {
                            var newUserId = createUserResult.Data as string;
                            user = _dbContext.Users.Find(i => i.Id == newUserId).FirstOrDefault();
                        }
                        else
                        {
                            result = createUserResult;
                            return result;
                        }
                    }
                    else
                    {
                        result.ErrorMessage = "Username or password is incorrect";
                        return result;
                    }
                    #endregion

                    #region Don't check on old system
                    //result.ErrorMessage = "Username or password is incorrect";
                    //return result;
                    #endregion
                }
                var passwordHasher = new PasswordHasher<UserInformation>();
                var passwordVerificationResult = passwordHasher.VerifyHashedPassword(user, user.HashedPassword, model.Password);
                if (passwordVerificationResult == PasswordVerificationResult.Failed)
                {
                    result.ErrorMessage = "Username or password is incorrect";
                    return result;
                }
                var accessToken = GetAccessToken(user, model.PermissionQuery);

                result.Data = accessToken;
                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;

        }

        private async Task<bool> UserIsOnOldLoginSystem(string username, string password)
        {
            var httpClient = _httpClientFactory.CreateClient();

            var requestBodyContent = new StringContent($"grant_type=password&username={username}&password={password}", Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.vkhealth.vn/token", requestBodyContent);

            return response.StatusCode == System.Net.HttpStatusCode.OK;
        }

        private Token GetAccessToken(UserInformation user, PermissionQuery permissionQuery = null)
        {
            List<Claim> claims = GetClaims(user);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
              _configuration["Jwt:Issuer"],
              claims,
              expires: DateTime.Now.AddDays(90),
              signingCredentials: creds);

            var serializedToken = new JwtSecurityTokenHandler().WriteToken(token);

            return new Token
            {
                Access_token = serializedToken,
                Token_type = "Bearer",
                Expires_in = 90 * 3600,
                UserId = user.Id,
                Username = user.Username,
                Permissions = permissionQuery != null ? GetPermissions(permissionQuery, user) : new List<Permission>(),
            };
        }

        private List<Permission> GetPermissions(PermissionQuery permissionQuery, UserInformation user)
        {
            var permissions = new List<Permission>();
            if (permissionQuery.Type == "UiPermission")
            {
                var permissionFilters = Builders<UiPermission>.Filter.In(i => i.Id, user.UiPermissionIds);

                permissions = _dbContext.UiPermissions.Find(permissionFilters).ToEnumerable().Select(i => new Permission { Code = i.Code }).ToList();
            }
            return permissions;
        }

        private List<Claim> GetClaims(UserInformation user)
        {
            var claims = new List<Claim> {
                new Claim("Id",user.Id),
                new Claim("Email", user.Email),
                new Claim("FullName", user.FullName),
                new Claim("Username",user.Username)
            };

            foreach (var roleId in user.RoleIds)
            {
                var role = _dbContext.Roles.Find(r => r.Id == roleId).FirstOrDefault();
                claims.Add(new Claim("Role", role.Name));
            }

            if (!string.IsNullOrEmpty(user.PhoneNumber)) claims.Add(new Claim("PhoneNumber", user.PhoneNumber));

            return claims;
        }

        public ResultModel ResetDefaultPassword(string username)
        {
            var result = new ResultModel();
            var defaultPassword = "Zaq@123ABC";
            try
            {
                username = username.ToUpper();
                var user = _dbContext.Users.Find(i => i.NormalizedUsername == username).FirstOrDefault();
                if (user == null)
                {
                    result.ErrorMessage = "Username or password is incorrect";
                    return result;
                }
                var passwordHasher = new PasswordHasher<UserInformation>();
                user.HashedPassword = passwordHasher.HashPassword(user, defaultPassword);
                _dbContext.Users.ReplaceOne(i => i.Id == user.Id, user);

                result.Data = defaultPassword;
                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        public ResultModel UpdateInformation(UserUpdateModel model, string userId)
        {
            var result = new ResultModel();
            try
            {
                var user = _dbContext.Users.Find(u => u.Id == userId).FirstOrDefault();
                user.FullName = model.FullName;
                _dbContext.Users.FindOneAndReplace(i => i.Id == userId, user);
                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.Succeed = false;
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        public ResultModel ImportUsers(List<ImportUserModel> model)
        {
            var result = new ResultModel();
            try
            {
                var users = model.Select(i => new UserInformation
                {
                    Username = i.Username,
                    HashedPassword = i.Password,
                    Email = i.Username,
                    NormalizedEmail = i.Username.ToUpper(),
                    NormalizedUsername = i.Username.ToUpper()
                });
                _dbContext.Users.InsertMany(users);

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        public List<UserInformationModel> GetAll()
        {
            var users = _dbContext.Users.Find(i => true).ToList();
            return _mapper.Map<List<UserInformation>, List<UserInformationModel>>(users);
        }

        public List<RoleModel> GetRoles(string userId)
        {
            var user = _dbContext.Users.Find(u => u.Id == userId).FirstOrDefault();
            List<RoleModel> result = new List<RoleModel>();

            foreach (var roleId in user.RoleIds)
            {
                var role = _dbContext.Roles.Find(u => u.Id == roleId).FirstOrDefault();
                result.Add(_mapper.Map<Role, RoleModel>(role));
            }
            return result;
        }

        public List<GroupModel> GetGroups(string userId)
        {
            var user = _dbContext.Users.Find(u => u.Id == userId).FirstOrDefault();

            List<GroupModel> result = new List<GroupModel>();

            foreach (var groupId in user.GroupIds)
            {
                var group = _dbContext.Groups.Find(u => u.Id == groupId).FirstOrDefault();
                result.Add(_mapper.Map<Group, GroupModel>(group));
            }
            return result;
        }

        public async Task<ResultModel> GenerateResetPasswordOTP(GenerateResetPasswordOTPModel model)
        {
            var result = new ResultModel();
            try
            {
                var otp = OTPHepler.GenerateOTP();
                if (!string.IsNullOrEmpty(model.PhoneNumber))
                {
                    var updateResult = await _dbContext.Users.UpdateOneAsync(x => x.PhoneNumber == model.PhoneNumber,
                        Builders<UserInformation>.Update.Set(x => x.OTP, otp));
                    if (updateResult.ModifiedCount != 0)
                    {
                        result.Succeed = true;
                    }
                    else
                    {
                        result.ErrorMessage = "Phone Number does not match";
                    }
                }
                else if (!string.IsNullOrEmpty(model.Email))
                {
                    var updateResult = await _dbContext.Users.UpdateOneAsync(x => x.Email == model.Email,
                      Builders<UserInformation>.Update.Set(x => x.OTP, otp));
                    if (updateResult.ModifiedCount != 0)
                    {
                        var isMailSent = await _mailService.SendEmail(new EmailViewModel()
                        {
                            To = model.Email,
                            Subject = "Reset Password for USAID",
                            Text = $"Follow this OTP to reset USAID password: {otp.Value}"
                        });
                        result.Succeed = isMailSent;
                    }
                    else
                    {
                        result.ErrorMessage = "Email does not match";
                    }
                }

            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        public async Task<ResultModel> ConfirmResetPasswordOTP(ConfirmResetPasswordOTPModel model)
        {
            var result = new ResultModel();
            try
            {

                if (!string.IsNullOrEmpty(model.PhoneNumber))
                {
                    var user = await _dbContext.Users.Find(i => i.PhoneNumber == model.PhoneNumber).FirstOrDefaultAsync();
                    if (user == null)
                    {
                        result.ErrorMessage = "PhoneNumber does not exist";
                    }
                    else if (!OTPHepler.ValidateOTP(model.OTP, user.OTP))
                    {
                        result.ErrorMessage = "OTP is incorrect or expired";
                    }
                    else
                    {
                        await _dbContext.Users.UpdateOneAsync(x => x.Id == user.Id, Builders<UserInformation>.Update.Set(x => x.OTP, null));
                        var accessToken = GetAccessToken(user);
                        result.Data = accessToken;
                        result.Succeed = true;
                    }
                }
                else if (!string.IsNullOrEmpty(model.Email))
                {
                    var user = await _dbContext.Users.Find(i => i.Email == model.Email).FirstOrDefaultAsync();
                    if (user == null)
                    {
                        result.ErrorMessage = "Email does not exist";
                    }
                    else if (!OTPHepler.ValidateOTP(model.OTP, user.OTP))
                    {
                        result.ErrorMessage = "OTP is incorrect or expired";
                    }
                    else
                    {
                        await _dbContext.Users.UpdateOneAsync(x => x.Id == user.Id, Builders<UserInformation>.Update.Set(x => x.OTP, null));
                        var accessToken = GetAccessToken(user);
                        result.Data = accessToken;
                        result.Succeed = true;
                    }
                }
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        public async Task<ResultModel> ConfirmResetPasswordSecurityQuestion(ConfirmResetPasswordSecurityQuestionModel model)
        {
            var result = new ResultModel();
            try
            {
                model.Username = model.Username.ToUpper();
                var user = await _dbContext.Users.Find(i => i.NormalizedUsername == model.Username).FirstOrDefaultAsync();
                if (user == null)
                {
                    result.ErrorMessage = "User does not exist";
                    return result;
                }

                if (string.IsNullOrEmpty(user.SecurityQuestionId))
                {
                    result.ErrorMessage = "User has no security question";
                }
                else
                {
                    if (model.SecurityQuestionId.Equals(user.SecurityQuestionId) && model.SecurityQuestionAnswer.Equals(user.SecurityQuestionAnswer))
                    {
                        var accessToken = GetAccessToken(user);
                        result.Data = accessToken;
                        result.Succeed = true;
                        result.Succeed = true;
                    }
                    else
                    {
                        result.ErrorMessage = "Security Question doesn't match";
                    }
                }
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }
        public async Task<ResultModel> ResetPassword(ResetPasswordModel model, string username)
        {
            var result = new ResultModel();
            try
            {
                username = username.ToUpper();
                var user = await _dbContext.Users.Find(i => i.NormalizedUsername == username).FirstOrDefaultAsync();

                var passwordHasher = new PasswordHasher<UserInformation>();
                var update = await _dbContext.Users.UpdateOneAsync(i => i.NormalizedUsername == username, Builders<UserInformation>.Update.Set(x => x.HashedPassword, passwordHasher.HashPassword(user, model.NewPassword)));

                result.Succeed = update.ModifiedCount == 1;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        public async Task<ResultModel> ChangeSecurityQuestionAnswer(ChangeSecurityQuestionAnswerModel model, string username)
        {
            var result = new ResultModel();
            try
            {
                username = username.ToUpper();
                var user = await _dbContext.Users.Find(i => i.NormalizedUsername == username).FirstOrDefaultAsync();

                var passwordHasher = new PasswordHasher<UserInformation>();
                var passwordVerificationResult = passwordHasher.VerifyHashedPassword(user, user.HashedPassword, model.Password);
                if (passwordVerificationResult == PasswordVerificationResult.Failed)
                {
                    result.ErrorMessage = "Password is incorrect";
                    return result;
                }
                var securityQuestion = await _dbContext.SecurityQuestions.Find(x => x.Id == model.QuestionAnswer.Id).FirstOrDefaultAsync();
                if (securityQuestion == null)
                {
                    result.ErrorMessage = "Security Question does not exist";
                    return result;
                }
                await _dbContext.Users.UpdateOneAsync(x => x.Id == user.Id, Builders<UserInformation>.Update.Set(x => x.SecurityQuestionId, model.QuestionAnswer.Id)
                    .Set(x => x.SecurityQuestionAnswer, model.QuestionAnswer.Answer));

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        public async Task<ResultModel> GetUserInfoAsync(string username)
        {
            var result = new ResultModel();
            try
            {
                username = username.ToUpper();
                var user = await _dbContext.Users.Find(i => i.NormalizedUsername == username).FirstOrDefaultAsync();
                if (user == null)
                {
                    throw new Exception("User is not exist!");
                }
                var userInfo = _mapper.Map<UserInformation, UserInformationModel>(user);
                var permissions = GetPermissions(new PermissionQuery() { Type = "UiPermission" }, user);

                result.Data = new UserInformationWithPermissionsModel() { Permissions = permissions, UserInfo = userInfo };
                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }
        public async Task<ResultModel> LoginWithFacebookAsync(string accessToken)
        {
            var result = new ResultModel();
            try
            {
                var userInfo = await _facebookAuthService.GetUserInfoResult(accessToken);
                if (userInfo == null)
                {
                    result.ErrorMessage = "AccessToken Failed";
                    return result;
                }
                var user = await _dbContext.Users.Find(i => i.Email == userInfo.email).FirstOrDefaultAsync();
                if (user == null)
                {
                    var userCreateModel = new UserCreateModel
                    {
                        Username = "fb." + userInfo.id,
                        Password = userInfo.id,
                        FullName = userInfo.name,
                        Email = userInfo.email ?? ""
                    };
                    var createUserResult = await Create(userCreateModel);
                    if (createUserResult.Succeed)
                    {
                        var newUserId = createUserResult.Data as string;
                        user = await _dbContext.Users.Find(i => i.Id == newUserId).FirstOrDefaultAsync();
                    }
                    else
                    {
                        result = createUserResult;
                        return result;
                    }
                }
                result.Succeed = true;
                result.Data = GetAccessToken(user, new PermissionQuery() { Type = "UiPermission" }); ;
                return result;
            }
            catch (Exception e)
            {
                result.Succeed = false;
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
                return result;
            }
        }

        public async Task<ResultModel> LoginWithGoogleAsync(string idToken)
        {
            var result = new ResultModel();
            try
            {
                var userInfo = await _googleAuthService.GetUserInfoResult(idToken);
                if (userInfo == null)
                {
                    result.ErrorMessage = "AccessToken Failed";
                    return result;
                }
                var user = await _dbContext.Users.Find(i => i.Email == userInfo.Email).FirstOrDefaultAsync();
                if (user == null)
                {
                    var userCreateModel = new UserCreateModel
                    {
                        Username = "google." + userInfo.Subject,
                        Password = userInfo.Subject,
                        FullName = userInfo.Name ?? userInfo.Email,
                        Email = userInfo.Email ?? ""
                    };
                    var createUserResult = await Create(userCreateModel);
                    if (createUserResult.Succeed)
                    {
                        var newUserId = createUserResult.Data as string;
                        user = await _dbContext.Users.Find(i => i.Id == newUserId).FirstOrDefaultAsync();
                    }
                    else
                    {
                        result = createUserResult;
                        return result;
                    }
                }
                result.Succeed = true;
                result.Data = GetAccessToken(user, new PermissionQuery() { Type = "UiPermission" });
                return result;
            }
            catch (Exception e)
            {
                result.Succeed = false;
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
                return result;
            }
        }
    }
}
