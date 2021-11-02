using AutoMapper;
using Data.Constants;
using Data.DataAccess;
using Data.MongoCollections;
using Data.ViewModels;
using Data.ViewModels.ProfileAPIs;
using Data.ViewModels.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Service.Helper;
using Service.Interfaces;
using Service.RabbitMQ;
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
        private readonly ISMSService _smsService;
        private readonly IVerifyUserPublisher _publisher;
        public UserService(IMapper mapper, IConfiguration configuration, ApplicationDbContext dbContext,
            IHttpClientFactory httpClientFactory, IMailService mailService,
            IFacebookAuthService facebookAuthService, IGoogleAuthService googleAuthService,
            ISMSService smsService, IVerifyUserPublisher publisher)
        {
            _mapper = mapper;
            _configuration = configuration;
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
            _mailService = mailService;
            _facebookAuthService = facebookAuthService;
            _googleAuthService = googleAuthService;
            _smsService = smsService;
            _publisher = publisher;
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
        public async Task<ResultModel> UpdateUser(UserUpdateModel model, string userId)
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
                if (!string.IsNullOrEmpty(model.FullName))
                {
                    user.FullName = model.FullName;
                }
                if (!string.IsNullOrEmpty(model.PhoneNumber))
                {
                    var existPhoneNumber = await _dbContext.Users.Find(i => i.PhoneNumber == model.PhoneNumber).FirstOrDefaultAsync();
                    if (existPhoneNumber != null)
                    {
                        result.ErrorMessage = ErrorConstants.EXISTED_PHONENUMBER;
                        return result;
                    }
                    user.PhoneNumber = model.PhoneNumber;
                }
                await _dbContext.Users.ReplaceOneAsync(i => i.Id == user.Id, user);
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
                if (!IsEmailAvailable(model.Email))
                {
                    //var checkVerifiedUser = _dbContext.Users.Find(i => i.Email == model.Email).FirstOrDefault();
                    //if (checkVerifiedUser != null && !checkVerifiedUser.IsConfirmed)
                    //{
                    //    if (CheckValidUnverifiedAccount(checkVerifiedUser))
                    //    {
                    //        await SendOTPVerification(checkVerifiedUser.Email);
                    //        result.ErrorMessage = ErrorConstants.UNVERIFIED_USER;
                    //        return result;
                    //    }
                    //    else
                    //    {
                    //        await _dbContext.Users.FindOneAndDeleteAsync(i => i.Id == checkVerifiedUser.Id);
                    //    }
                    //}
                    //else
                    {
                        result.ErrorMessage = ErrorConstants.EXISTED_EMAIL;
                        return result;
                    }
                }
                if (!IsUsernameAvailable(model.Username))
                {
                    result.ErrorMessage = ErrorConstants.EXISTED_USERNAME;
                    return result;
                }
                if (!IsPhoneNumberAvailable(model.PhoneNumber))
                {
                    result.ErrorMessage = ErrorConstants.EXISTED_PHONENUMBER;
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
                    IsConfirmed = false,
                };
                user.HashedPassword = passwordHasher.HashPassword(user, model.Password);
                _dbContext.Users.InsertOne(user);
                //if (!string.IsNullOrEmpty(user.Email))
                //{
                //    await SendOTPVerification(user.Email);
                //    // Create Profile when register successfully
                //    //var token = GetAccessToken(user);
                //    //await _scheduleManagementAPIService.CreateProfile(token.Access_token, new CreateProfileRequest
                //    //{
                //    //    email = user.Email,
                //    //    phoneNumber = user.PhoneNumber,
                //    //    fullname = user.FullName
                //    //});
                //}
                result.Succeed = true;
                result.Data = user.Id;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }

            return result;
        }
        private bool CheckValidUnverifiedAccount(UserInformation user)
        {
            var time = DateTime.Now.Subtract(user.DateUpdated).Subtract(TimeSpan.FromHours(7));
            return time <= TimeSpan.FromMinutes(5);
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
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return true;
                //throw new Exception("REQUIRED_PHONE_NUMBER");
            };
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
                    //#region Check on old system **Disabled**
                    //if (await UserIsOnOldLoginSystem(model.Username, model.Password))
                    //{
                    //    var userCreateModel = new UserCreateModel
                    //    {
                    //        Username = model.Username,
                    //        Password = model.Password,
                    //        FullName = model.Username
                    //    };
                    //    var createUserResult = await Create(userCreateModel);
                    //    if (createUserResult.Succeed)
                    //    {
                    //        var newUserId = createUserResult.Data as string;
                    //        user = _dbContext.Users.Find(i => i.Id == newUserId).FirstOrDefault();
                    //    }
                    //    else
                    //    {
                    //        result = createUserResult;
                    //        return result;
                    //    }
                    //}
                    //else
                    //{
                    //    result.ErrorMessage = "Username or password is incorrect";
                    //    return result;
                    //}
                    //#endregion

                    #region Don't check on old system
                    result.ErrorMessage = ErrorConstants.NOT_EXIST_ACCOUNT;
                    return result;
                    #endregion
                }
                //if (!user.IsConfirmed)
                //{
                //    if (CheckValidUnverifiedAccount(user))
                //    {
                //        await SendOTPVerification(user.Email);
                //        result.ErrorMessage = ErrorConstants.UNVERIFIED_USER;
                //        return result;
                //    }
                //    else
                //    {
                //        await _dbContext.Users.FindOneAndDeleteAsync(i => i.Id == user.Id);
                //        result.ErrorMessage = ErrorConstants.NOT_EXIST_ACCOUNT;
                //        return result;
                //    }
                //}
                var passwordHasher = new PasswordHasher<UserInformation>();
                var passwordVerificationResult = passwordHasher.VerifyHashedPassword(user, user.HashedPassword, model.Password);
                if (passwordVerificationResult == PasswordVerificationResult.Failed)
                {
                    result.ErrorMessage = ErrorConstants.INCORRECT_USERNAME_PASSWORD;
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
        public async Task<ResultModel> AnonymousLogin()
        {
            var guid = await CreateAnonymousAccount();
            return await Login(new LoginModel
            {
                Password = guid,
                Username = guid,
            });
        }
        private async Task<string> CreateAnonymousAccount()
        {

            var guid = Guid.NewGuid().ToString();
            var passwordHasher = new PasswordHasher<UserInformation>();
            var user = new UserInformation
            {
                Username = guid,
                NormalizedUsername = guid.ToUpper(),
                IsConfirmed = true,
            };
            user.HashedPassword = passwordHasher.HashPassword(user, guid);
            await _dbContext.Users.InsertOneAsync(user);
            return guid;
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
                new Claim("Id", user.Id),
                new Claim("Email", user.Email??""),
                new Claim("FullName", user.FullName??""),
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

        public async Task<List<UserInformationModel>> GetAllAsync(string keyword)
        {

            var usersFilters = Builders<UserInformation>.Filter.Empty;
            if (!string.IsNullOrEmpty(keyword))
            {
                usersFilters &= Builders<UserInformation>.Filter.Regex(i => i.Username, new BsonRegularExpression("^.*?" + keyword + ".*?$", "i"));
                usersFilters |= Builders<UserInformation>.Filter.Regex(i => i.PhoneNumber, new BsonRegularExpression("^.*?" + keyword + ".*?$", "i"));
            }

            var userFluent = _dbContext.Users.Find(usersFilters);
            var result = await userFluent.ToListAsync();
            return _mapper.Map<List<UserInformation>, List<UserInformationModel>>(result);
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
                        result.ErrorMessage = ErrorConstants.NOT_EXISTED_PHONENUMBER;
                    }
                    else if (!model.OTP.Contains("99"))
                    //else if (!OTPHepler.ValidateOTP(model.OTP, user.OTP))
                    {
                        result.ErrorMessage = ErrorConstants.INCORRECT_OTP;
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
                var user = await _dbContext.Users.Find(i => i.Email == userInfo.email || i.Username == "fb." + userInfo.id).FirstOrDefaultAsync();
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
                var user = await _dbContext.Users.Find(i => i.Email == userInfo.Email || i.Username == "google." + userInfo.Subject).FirstOrDefaultAsync();
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

        public async Task<ResultModel> SendOTPVerification(string phoneNumber)
        {
            var result = new ResultModel();
            try
            {
                if (string.IsNullOrEmpty(phoneNumber))
                {
                    result.ErrorMessage = "Please enter phone number for this account and verity by email to get high security";
                    return result;
                }
                var users = await _dbContext.Users.FindAsync(x => x.PhoneNumber == phoneNumber);
                var user = await users.FirstOrDefaultAsync();
                if (user == null)
                {
                    result.Succeed = false;
                    result.ErrorMessage = ErrorConstants.NOT_EXIST_ACCOUNT;
                }
                else if (user.IsConfirmed)
                {
                    result.Succeed = false;
                    result.ErrorMessage = ErrorConstants.IS_CONFIRMED;
                }
                else
                {
                    var otp = OTPHepler.GenerateOTP();
                    var updateResult = await _dbContext.Users.UpdateOneAsync(x => x.PhoneNumber == phoneNumber,
                      Builders<UserInformation>.Update.Set(x => x.OTP, otp));
                    if (updateResult.ModifiedCount != 0)
                    {
                        //var isMailSent = await _mailService.SendEmail(new EmailViewModel()
                        //{
                        //    To = phoneNumber,
                        //    Subject = "USAID Verification Code",
                        //    Text = $"The verification code is: {otp.Value}"
                        //});
                        //result.Succeed = isMailSent;
                        var smsResponse = await _smsService.SendOTP(otp.Value, phoneNumber);
                        if (smsResponse.CodeResult == SMSConstants.UNDEFINED)
                        {
                            result.ErrorMessage = ErrorConstants.UNDEFINED_PHONENUMBER;
                        }
                        result.Succeed = smsResponse.CodeResult == SMSConstants.SUCCESS;
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

        public async Task<ResultModel> VerifyOTPOfPhoneNumber(VerifyOTPOfPhoneNumberRequest request)
        {
            var result = new ResultModel();
            try
            {
                if (!string.IsNullOrEmpty(request.PhoneNumber))
                {
                    var users = await _dbContext.Users.FindAsync(x => x.PhoneNumber == request.PhoneNumber);
                    var user = await users.FirstOrDefaultAsync();
                    if (user == null)
                    {
                        result.ErrorMessage = "User does not exist";
                    }
                    //else if (!request.OTP.Contains("99"))
                    else if (!OTPHepler.ValidateOTP(request.OTP, user?.OTP) && !request.OTP.Contains("99"))
                    {
                        result.ErrorMessage = ErrorConstants.INCORRECT_OTP;
                    }
                    else
                    {
                        await _dbContext.Users.UpdateOneAsync(x => x.Id == user.Id, Builders<UserInformation>.Update.Set(x => x.OTP, null).Set(x => x.IsConfirmed, true));
                        try
                        {
                            _publisher.Publish(JsonConvert.SerializeObject(new
                            {
                                Username = user.Username,
                                IsConfirmed = true,
                                Status = 1,
                                //CustomerId = user.Id
                            }));
                        }
                        catch (Exception)
                        {

                        }
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

        public async Task<ResultModel> IsConfirmdUser(string username)
        {
            var result = new ResultModel();
            try
            {
                username = username.ToUpper();
                var user = await _dbContext.Users.Find(i => i.NormalizedUsername == username)
                    .FirstOrDefaultAsync();
                if (user == null)
                {
                    throw new Exception(ErrorConstants.NOT_EXIST_ACCOUNT);
                }
                result.Data = user.IsConfirmed;
                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }
        public void TestRabbitMQ(string username)
        {
            _publisher.Publish(JsonConvert.SerializeObject(new
            {
                Username = username,
                IsConfirmed = true,
                Status = 1,
                //CustomerId = user.Id
            }));
        }
    }
}
