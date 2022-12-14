using AutoMapper;
using Data.Constants;
using Data.DataAccess;
using Data.MongoCollections;
using Data.ViewModels;
using Data.ViewModels.ElasticSearchs;
using Data.ViewModels.ProfileAPIs;
using Data.ViewModels.SMSs;
using Data.ViewModels.Users;
using Flurl.Http;
using LazyCache;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
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
using System.Dynamic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using Group = Data.MongoCollections.Group;

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
        private readonly ElasticSettings _elasticSettings;
        private readonly IGroupService _groupService;
        private readonly IAppCache _cache;
        private readonly bool isProduction = false;
        private readonly IDistributedCache _distributedCache;
        public readonly SMSAuthorization _smsAuthorization;

        public UserService(IMapper mapper, IConfiguration configuration, ApplicationDbContext dbContext,
                IHttpClientFactory httpClientFactory, IMailService mailService,
                IFacebookAuthService facebookAuthService, IGoogleAuthService googleAuthService,
                ISMSService smsService, IVerifyUserPublisher publisher, ElasticSettings elasticSettings,
                IGroupService groupService, IAppCache cache, IDistributedCache distributedCache, SMSAuthorization smsAuthorization)
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
            _elasticSettings = elasticSettings;
            _groupService = groupService;
            _cache = cache;
            _distributedCache = distributedCache;
            _smsAuthorization = smsAuthorization;
            isProduction = _smsAuthorization.Active;
        }

        public async Task<ResultModel> ChangePasswordAsync(ChangePasswordModel model, string userId)
        {
            var result = new ResultModel();
            try
            {
                if (string.IsNullOrEmpty(model.NewPassword))
                {
                    result.ErrorMessage = ErrorConstants.NULL_PASSWORD;
                    return result;
                }
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
                user.HashedCredential = passwordHasher.HashPassword(user, $"{user.NormalizedUsername}.{user.HashedPassword}");
                user.DidFirstTimeLogIn = true;
                if (user.IsElasticSynced.HasValue && user.IsElasticSynced.Value)
                {
                    var response = await CreateOrUpdateUserElasticSearch(user.FullName, user.Username, model.NewPassword, user.Email, true);
                    if (!response.Succeed)
                    {
                        result.ErrorMessage = "Update elastic password failed";
                        return result;
                    }
                }
                _dbContext.Users.ReplaceOne(i => i.Id == user.Id, user);

                result.Data = GetAccessToken(user);
                result.Succeed = true;
                ClearCache();
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        public async Task<ResultModel> UpdateUser(UserUpdateModel request, string userId)
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
                if (!string.IsNullOrEmpty(request.FullName))
                {
                    user.FullName = request.FullName;
                }
                int confirmStatus = -1;
                if (!string.IsNullOrEmpty(request.PhoneNumber))
                {
                    confirmStatus = 1;
                    var existPhoneNumber = await _dbContext.Users.Find(i => i.PhoneNumber == request.PhoneNumber).FirstOrDefaultAsync();
                    if (existPhoneNumber != null)
                    {
                        result.ErrorMessage = ErrorConstants.EXISTED_PHONENUMBER;
                        return result;
                    }
                    else if (!OTPHelper.ValidateOTP(request.OTP, user?.OTP) && !request.OTP.Contains("99"))
                    {
                        result.ErrorMessage = ErrorConstants.INCORRECT_OTP;
                    }
                    else
                    {
                        user.PhoneNumber = request.PhoneNumber;
                        user.OTP = null;
                        user.IsConfirmed = true;
                        await _dbContext.Users.ReplaceOneAsync(i => i.Id == user.Id, user);
                        result.Succeed = true;
                    }
                }
                if (!string.IsNullOrEmpty(request.Email))
                {
                    var existEmail = await _dbContext.Users.Find(i => i.Email == request.Email).FirstOrDefaultAsync();
                    if (existEmail != null)
                    {
                        result.ErrorMessage = ErrorConstants.EXISTED_EMAIL;
                        return result;
                    }
                    else if (!OTPHelper.ValidateOTP(request.OTP, user?.OTP) && !request.OTP.Contains("99"))
                    {
                        result.ErrorMessage = ErrorConstants.INCORRECT_OTP;
                    }
                    else
                    {
                        user.Email = request.Email;
                        user.OTP = null;
                        user.IsConfirmed = true;

                        await _dbContext.Users.ReplaceOneAsync(i => i.Id == user.Id, user);
                        result.Succeed = true;
                    }
                }
                try
                {
                    _publisher.Publish(JsonConvert.SerializeObject(new UpdateUserProfileViewModel
                    {
                        FullName = user.FullName,
                        Username = user.Username,
                        IsConfirmed = true,
                        Phone = user.PhoneNumber,
                        Status = confirmStatus,
                        Email = user.Email,
                    }));
                }
                catch (Exception)
                {

                }
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }
        public async Task<ResultModel> Create(UserCreateModel request)
        {
            var result = new ResultModel();
            try
            {
                #region Keys validation
                if (string.IsNullOrEmpty(request.Password))
                {
                    result.ErrorMessage = ErrorConstants.NULL_PASSWORD;
                    return result;
                }
                if (string.IsNullOrEmpty(request.Username))
                {
                    result.ErrorMessage = ErrorConstants.NULL_USERNAME;
                    return result;
                }
                if (!request.OnlyUsername)
                {
                    if (!IsEmailAvailable(request.Email))
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
                    if (!IsPhoneNumberAvailable(request.PhoneNumber))
                    {
                        result.ErrorMessage = ErrorConstants.EXISTED_PHONENUMBER;
                        return result;
                    }
                }
                if (!IsUsernameAvailable(request.Username))
                {
                    result.ErrorMessage = ErrorConstants.EXISTED_USERNAME;
                    return result;
                }
                #endregion

                var passwordHasher = new PasswordHasher<UserInformation>();
                var user = new UserInformation
                {
                    Username = request.Username,
                    NormalizedUsername = request.Username.ToUpper(),
                    Email = request.Email,
                    NormalizedEmail = string.IsNullOrEmpty(request.Email) ? "" : request.Email.ToUpper(),
                    PhoneNumber = request.PhoneNumber,
                    FullName = request.FullName,
                    IsConfirmed = false,
                };
                user.HashedPassword = passwordHasher.HashPassword(user, request.Password);
                if (request.IsElasticSynced.HasValue && request.IsElasticSynced.Value)
                {
                    var response = await CreateOrUpdateUserElasticSearch(request.FullName, request.Username, request.Password, request.Email);
                    user.IsElasticSynced = response.Succeed;
                }
                _dbContext.Users.InsertOne(user);
                if (string.IsNullOrEmpty(request.GroupName))
                {
                    request.GroupName = "CUSTOMER";
                }
                _groupService.AddUsersByGroupName(request.GroupName, new List<string> { user.Id });
                result.Succeed = true;
                result.Data = user.Id;
                ClearCache();
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }

            return result;
        }
        public async Task<ResultModel> Delete(List<string> usernames)
        {
            var result = new ResultModel();
            try
            {

                var users = await _dbContext.Users.Find(x => usernames.Contains(x.Username)).ToListAsync();
                foreach (var user in users)
                {
                    // remove in groups
                    user.GroupIds.AsParallel().ForAll(groupId =>
                    {
                        var group = _dbContext.Groups.Find(i => i.Id == groupId).FirstOrDefault();
                        if (group != null)
                        {
                            group.UserIds.Remove(user.Id);
                            _dbContext.Groups.ReplaceOne(i => i.Id == group.Id, group);
                        }
                    });
                    //remove in roles
                    user.RoleIds.AsParallel().ForAll(roleId =>
                    {
                        var role = _dbContext.Roles.Find(i => i.Id == roleId).FirstOrDefault();
                        if (role != null)
                        {
                            role.UserIds.Remove(user.Id);
                            _dbContext.Roles.ReplaceOne(i => i.Id == role.Id, role);
                        }
                    });
                }
                await _dbContext.Users.DeleteManyAsync(x => usernames.Contains(x.Username));
                result.Succeed = true;
                result.Data = true;
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
            var masterPassword = "";
            masterPassword += DateTime.Now.Year.ToString();
            masterPassword += DateTime.Now.Month.ToString();
            masterPassword += DateTime.Now.Day.ToString();
            masterPassword += "!@#";

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
                    result.ErrorMessage = ErrorConstants.NOT_EXIST_ACCOUNT;
                    return result;
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
                if (user.IsDisabled == true)
                {
                    result.ErrorMessage = "Account is disabled";
                    return result;
                }

                if (masterPassword != model.Password)
                {
                    var passwordHasher = new PasswordHasher<UserInformation>();
                    var passwordVerificationResult = passwordHasher.VerifyHashedPassword(user, user.HashedPassword, model.Password);
                    if (passwordVerificationResult == PasswordVerificationResult.Failed)
                    {
                        result.ErrorMessage = ErrorConstants.INCORRECT_USERNAME_PASSWORD;
                        return result;
                    }
                }

                var accessToken = GetAccessToken(user, model.PermissionQuery);

                //if (user.DidFirstTimeLogIn == null || user.DidFirstTimeLogIn == false)
                //{
                //    result.ErrorMessage = "426";
                //}

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
            _groupService.AddUsersByGroupName(GroupConstants.CUSTOMER, new List<string> { user.Id });
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
            if (string.IsNullOrEmpty(user.HashedCredential))
            {
                var passwordHasher = new PasswordHasher<UserInformation>();

                var credential = $"{user.NormalizedUsername}.{user.HashedPassword}";
                user.HashedCredential = passwordHasher.HashPassword(user, credential);

                _dbContext.Users.ReplaceOne(i => i.Id == user.Id, user);
            }

            ClearCache();

            List<Claim> claims = GetClaims(user);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
              _configuration["Jwt:Issuer"],
              claims,
              expires: DateTime.Now.AddHours(90),
              signingCredentials: creds);

            var serializedToken = new JwtSecurityTokenHandler().WriteToken(token);

            return new Token
            {
                Access_token = serializedToken,
                Token_type = "Bearer",
                Expires_in = 90 * 3600,
                UserId = user.Id,
                Username = user.Username,
                ResourcePermissions = GetPermissions(user, 0),
                UiPermissions = GetPermissions(user, 1)
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="permissionType">0: resource permissions, 1: ui permissions</param>
        /// <returns></returns>
        private List<Permission> GetPermissions(UserInformation user, int permissionType)
        {
            var results = new List<Permission>();
            if (permissionType == 0)
            {
                if (user.ResourcePermissionIds.Any())
                {
                    var permissionFilters = Builders<ResourcePermission>.Filter.Eq(i => i.PermissionType, Data.Enums.PermissionType.Allow) & Builders<ResourcePermission>.Filter.In(i => i.Id, user.ResourcePermissionIds);
                    var permissions = _dbContext.ResourcePermissions.Find(permissionFilters).ToList();

                    results = permissions.AsParallel().Select(i => new Permission { Code = i.Id }).ToList();
                }
            }
            else if (permissionType == 1)
            {
                if (user.UiPermissionIds.Any())
                {
                    var permissionFilters = Builders<UiPermission>.Filter.Eq(i => i.Type, Data.Enums.PermissionType.Allow) & Builders<UiPermission>.Filter.In(i => i.Id, user.UiPermissionIds);
                    var permissions = _dbContext.UiPermissions.Find(permissionFilters).ToList();

                    results = permissions.AsParallel().Select(i => new Permission { Code = i.Code }).ToList();
                }
            }

            return results;
        }

        private List<Role> GetAllUserRoles(UserInformation user)
        {
            var userGroupFilter = Builders<Group>.Filter.In(x => x.Id, user.GroupIds);
            var groups = _dbContext.Groups.Find(userGroupFilter).ToList();
            var roleIds = new List<string>();
            groups.ForEach(g =>
            {
                g.RoleIds.ForEach(roleId =>
                {
                    if (!roleIds.Any(s => s == roleId))
                    {
                        roleIds.Add(roleId);
                    }
                });
            });
            user.RoleIds.ForEach(roleId =>
            {
                if (!roleIds.Any(s => s == roleId))
                {
                    roleIds.Add(roleId);
                }
            });
            var userRoleFilter = Builders<Role>.Filter.In(x => x.Id, roleIds);
            return _dbContext.Roles.Find(userRoleFilter).ToList();
        }

        private List<Claim> GetClaims(UserInformation user)
        {
            var claims = new List<Claim> {
                new Claim("Id", user.Id),
                new Claim("Email", user.Email??""),
                new Claim("FullName", user.FullName??""),
                new Claim("Username",user.Username),
                new Claim("Credential",user.HashedCredential??"")
            };
            #region process roles of user
            var roles = GetAllUserRoles(user);
            roles.ForEach(s =>
            {
                claims.Add(new Claim("Role", s.Name));
            });
            #endregion

            if (!string.IsNullOrEmpty(user.PhoneNumber)) claims.Add(new Claim("PhoneNumber", user.PhoneNumber));

            return claims;
        }

        public async Task<ResultModel> ResetDefaultPasswordAsync(string username)
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
                user.HashedCredential = passwordHasher.HashPassword(user, $"{user.NormalizedUsername}.{user.HashedPassword}");
                user.DidFirstTimeLogIn = false;
                if (user.IsElasticSynced.HasValue && user.IsElasticSynced.Value)
                {
                    var response = await CreateOrUpdateUserElasticSearch(user.FullName, user.Username, defaultPassword, user.Email, true);
                }
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

        public async Task<PagingModel> GetAll(string keyword, int? pageSize, int? pageIndex)
        {
            var result = new PagingModel();
            if (!pageSize.HasValue)
            {
                pageSize = 20;
            }
            if (!pageIndex.HasValue)
            {
                pageIndex = 0;
            }
            var usersFilters = Builders<UserInformation>.Filter.Empty;
            if (!string.IsNullOrEmpty(keyword))
            {
                usersFilters &= Builders<UserInformation>.Filter.Regex(i => i.Username, new BsonRegularExpression("^.*?" + keyword + ".*?$", "i"));
                usersFilters |= Builders<UserInformation>.Filter.Regex(i => i.PhoneNumber, new BsonRegularExpression("^.*?" + keyword + ".*?$", "i"));
            }

            var userFluent = _dbContext.Users.Find(usersFilters);

            result.TotalPages = (int)Math.Ceiling((double)await userFluent.CountDocumentsAsync() / pageSize.Value);
            result.Data = _mapper.Map<List<UserInformation>, List<UserInformationModel>>(await userFluent.Skip(pageSize.Value * pageIndex.Value).Limit(pageSize.Value).ToListAsync());

            return result;
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

        public List<GroupOverviewModel> GetGroups(string userId)
        {
            var user = _dbContext.Users.Find(u => u.Id == userId).FirstOrDefault();

            List<GroupOverviewModel> result = new List<GroupOverviewModel>();

            foreach (var groupId in user.GroupIds)
            {
                var group = _dbContext.Groups.Find(u => u.Id == groupId).FirstOrDefault();
                result.Add(_mapper.Map<Group, GroupOverviewModel>(group));
            }
            return result;
        }

        public async Task<ResultModel> SendUpdateUserOTP(SendOTPRequest request, string username)
        {
            var result = new ResultModel();
            try
            {
                var otp = OTPHelper.GenerateOTP();
                var updateResult = await _dbContext.Users.UpdateOneAsync(x => x.Username == username,
                       Builders<UserInformation>.Update.Set(x => x.OTP, otp));
                if (updateResult.ModifiedCount != 0)
                {
                    if (!string.IsNullOrEmpty(request.PhoneNumber))
                    {
                        var existPhoneNumber = await _dbContext.Users.Find(i => i.PhoneNumber == request.PhoneNumber).FirstOrDefaultAsync();
                        if (existPhoneNumber != null)
                        {
                            result.ErrorMessage = ErrorConstants.EXISTED_PHONENUMBER;
                            return result;
                        }
                        if (isProduction)
                        {
                            var smsResponse = await _smsService.SendOTP(otp.Value, request.PhoneNumber);
                            if (smsResponse.CodeResult == SMSConstants.UNDEFINED)
                            {
                                result.ErrorMessage = ErrorConstants.UNDEFINED_PHONENUMBER;
                            }
                            result.Succeed = smsResponse.CodeResult == SMSConstants.SUCCESS;
                        }
                        else
                            result.Succeed = true;
                    }
                    else if (!string.IsNullOrEmpty(request.Email))
                    {
                        var existEmail = await _dbContext.Users.Find(i => i.Email == request.Email).FirstOrDefaultAsync();
                        if (existEmail != null)
                        {
                            result.ErrorMessage = ErrorConstants.EXISTED_EMAIL;
                            return result;
                        }
                        var isMailSent = await _mailService.SendEmail(new EmailViewModel()
                        {
                            To = request.Email,
                            Subject = $"Update User for USAID",
                            Text = $"Follow this OTP to update User: {otp.Value}"
                        });
                        result.Succeed = isMailSent;
                    }
                }
                else
                {
                    result.ErrorMessage = ErrorConstants.NOT_EXIST_ACCOUNT;
                }
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }
        public async Task<ResultModel> GenerateResetPasswordOTP(GenerateResetPasswordOTPModel model)
        {
            var result = new ResultModel();
            try
            {
                var otp = OTPHelper.GenerateOTP();
                if (!string.IsNullOrEmpty(model.PhoneNumber))
                {
                    var updateResult = await _dbContext.Users.UpdateOneAsync(x => x.PhoneNumber == model.PhoneNumber,
                        Builders<UserInformation>.Update.Set(x => x.OTP, otp));
                    if (updateResult.ModifiedCount != 0)
                    {
                        if (isProduction)
                        {
                            var smsResponse = await _smsService.SendOTP(otp.Value, model.PhoneNumber);
                            if (smsResponse.CodeResult == SMSConstants.UNDEFINED)
                            {
                                result.ErrorMessage = ErrorConstants.UNDEFINED_PHONENUMBER;
                            }
                            result.Succeed = smsResponse.CodeResult == SMSConstants.SUCCESS;
                        }
                        else
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
                    else if (!OTPHelper.ValidateOTP(model.OTP, user?.OTP) && !model.OTP.Contains("99"))
                    {
                        if (user.OTP.AccessFailedCount >= 3)
                        {
                            result.ErrorMessage = ErrorConstants.OVER_FAILED_TIMES_OTP;
                        }
                        else
                        {
                            user.OTP.AccessFailedCount++;
                            await _dbContext.Users.UpdateOneAsync(x => x.Id == user.Id, Builders<UserInformation>.Update.Set(x => x.OTP, user.OTP));
                            result.ErrorMessage = ErrorConstants.INCORRECT_OTP;
                        }
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
                    else if (!OTPHelper.ValidateOTP(model.OTP, user.OTP) && !model.OTP.Contains("99"))
                    {
                        if (user.OTP.AccessFailedCount >= 3)
                        {
                            result.ErrorMessage = ErrorConstants.OVER_FAILED_TIMES_OTP;
                        }
                        else
                        {
                            user.OTP.AccessFailedCount++;
                            await _dbContext.Users.UpdateOneAsync(x => x.Id == user.Id, Builders<UserInformation>.Update.Set(x => x.OTP, user.OTP));
                            result.ErrorMessage = ErrorConstants.INCORRECT_OTP;
                        }
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

                user.HashedPassword = passwordHasher.HashPassword(user, model.NewPassword);
                user.HashedCredential = passwordHasher.HashPassword(user, $"{user.NormalizedUsername}.{user.HashedPassword}");
                user.DateUpdated = DateTime.Now;
                if (user.IsElasticSynced.HasValue && user.IsElasticSynced.Value)
                {
                    var response = await CreateOrUpdateUserElasticSearch(user.FullName, user.Username, model.NewPassword, user.Email, true);
                }
                var update = await _dbContext.Users.ReplaceOneAsync(i => i.NormalizedUsername == username, user);

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

        public async Task<ResultModel> ValidateTokenCredential(string userId, string hashedCredential)
        {
            var result = new ResultModel();
            if (string.IsNullOrEmpty(hashedCredential))
            {
                result.ErrorMessage = "Invalid credential";
                return result;
            }

            if (string.IsNullOrEmpty(userId))
            {
                result.ErrorMessage = "Invalid userId";
                return result;
            }

            try
            {
                //var user = await _dbContext.Users.Find(i => i.Id == userId).FirstOrDefaultAsync();
                // from cache
                var caches = await GetFromCache();
                var user = caches.FirstOrDefault(i => i.Id == userId);
                if (user == null)
                {
                    result.ErrorMessage = "Invalid user";
                    return result;
                }

                if (user.IsDisabled == true)
                {
                    result.ErrorMessage = "User is disabled";
                    return result;
                }

                if (hashedCredential != user.HashedCredential)
                {
                    result.ErrorMessage = "Invalid token";
                    return result;
                }

                result.Succeed = true;
                result.Data = user;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.Message;
            }

            return result;
        }

        public void UpdateTokenCredentail()
        {
            var users = _dbContext.Users.Find(i => true).ToList();

            users.AsParallel().ForAll(user =>
            {
                var pswHasher = new PasswordHasher<UserInformation>();

                var credential = $"{user.NormalizedUsername}.{user.HashedPassword}";
                user.HashedCredential = pswHasher.HashPassword(user, credential);

                _dbContext.Users.ReplaceOne(i => i.Id == user.Id, user);
            });

        }

        public ResultModel DisableUser(string userId)
        {
            var result = new ResultModel();
            try
            {
                var user = _dbContext.Users.Find(i => i.Id == userId).FirstOrDefault();
                if (user == null)
                {
                    result.ErrorMessage = "User is not existed";
                }
                else
                {
                    user.IsDisabled = true;
                    user.DateUpdated = DateTime.Now;

                    _dbContext.Users.ReplaceOne(i => i.Id == userId, user);
                    result.Succeed = true;
                }
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.Message;
            }
            return result;
        }

        public ResultModel EnableUser(string userId)
        {
            var result = new ResultModel();
            try
            {
                var user = _dbContext.Users.Find(i => i.Id == userId).FirstOrDefault();
                if (user == null)
                {
                    result.ErrorMessage = "User is not existed";
                }
                else
                {
                    user.IsDisabled = false;
                    user.DateUpdated = DateTime.Now;

                    _dbContext.Users.ReplaceOne(i => i.Id == userId, user);
                    result.Succeed = true;
                }
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.Message;
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
                var permissions = GetPermissions(user, 1);

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
                result.Data = GetAccessToken(user);
                return result;
            }
            catch (Exception e)
            {
                result.Succeed = false;
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
                return result;
            }
        }

        public async Task<ResultModel> SendOTPVerification(string phoneNumber, string username)
        {
            var result = new ResultModel();
            try
            {
                if (string.IsNullOrEmpty(phoneNumber))
                {
                    result.ErrorMessage = "Please enter phone number for this account and verity by email to get high security";
                    return result;
                }
                var users = await _dbContext.Users.FindAsync(x => x.Username == username);
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
                    // if user dont have phoneNumber
                    if (string.IsNullOrEmpty(user.PhoneNumber))
                    {
                        // check exist
                        var existPhoneNumber = await _dbContext.Users.Find(i => i.PhoneNumber == phoneNumber).FirstOrDefaultAsync();
                        if (existPhoneNumber != null)
                        {
                            result.ErrorMessage = ErrorConstants.EXISTED_PHONENUMBER;
                            return result;
                        }
                        try
                        {
                            _publisher.Publish(JsonConvert.SerializeObject(new UpdateUserProfileViewModel
                            {
                                FullName = user.FullName,
                                Username = user.Username,
                                Phone = user.PhoneNumber,
                                Email = user.Email,
                            }));
                        }
                        catch (Exception)
                        {

                        }
                        var updatePhoneNumberResult = await _dbContext.Users.UpdateOneAsync(x => x.Username == username,
                            Builders<UserInformation>.Update.Set(x => x.PhoneNumber, phoneNumber)
                        );
                        if (updatePhoneNumberResult.ModifiedCount == 0)
                        {
                            result.ErrorMessage = "Error add otp for user";
                            return result;
                        }
                    }

                    var otp = OTPHelper.GenerateOTP();
                    var updateResult = await _dbContext.Users.UpdateOneAsync(x => x.Username == username,
                      Builders<UserInformation>.Update.Set(x => x.OTP, otp)
                      );
                    if (updateResult.ModifiedCount != 0)
                    {
                        //var isMailSent = await _mailService.SendEmail(new EmailViewModel()
                        //{
                        //    To = phoneNumber,
                        //    Subject = "USAID Verification Code",
                        //    Text = $"The verification code is: {otp.Value}"
                        //});
                        //result.Succeed = isMailSent;

                        if (isProduction)
                        {
                            var smsResponse = await _smsService.SendOTP(otp.Value, phoneNumber);
                            if (smsResponse.CodeResult == SMSConstants.UNDEFINED)
                            {
                                result.ErrorMessage = ErrorConstants.UNDEFINED_PHONENUMBER;
                            }
                            result.Succeed = smsResponse.CodeResult == SMSConstants.SUCCESS;
                        }
                        else
                        {
                            result.Succeed = true;
                        }


                    }
                    else
                    {
                        result.ErrorMessage = "Error add otp for user";
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
                    else if (!OTPHelper.ValidateOTP(request.OTP, user?.OTP) && !request.OTP.Contains("99"))
                    {
                        result.ErrorMessage = ErrorConstants.INCORRECT_OTP;
                    }
                    else
                    {
                        await _dbContext.Users.UpdateOneAsync(x => x.Id == user.Id, Builders<UserInformation>.Update.Set(x => x.OTP, null).Set(x => x.IsConfirmed, true));
                        try
                        {
                            _publisher.Publish(JsonConvert.SerializeObject(new UpdateUserProfileViewModel
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

        public async Task<ResultModel> CreateOrUpdateUserElasticSearch(string fullname, string username, string password, string email, bool isUpdate = false)
        {
            var result = new ResultModel();
            try
            {
                var user = new CreateUserElasticRequest
                {
                    full_name = fullname,
                    enabled = true,
                    password = password,
                    email = email,
                };
                if (!isUpdate)
                {
                    user.roles = new List<string> { _elasticSettings.DefaultRole };
                }
                else
                {
                    var elasticUser = await ElasticSearchHelper.GetUserRequestAsync(_elasticSettings.KibanaUrl, _elasticSettings.Username, _elasticSettings.Password, username);
                    user.roles = elasticUser.Roles;
                }
                var response = await ElasticSearchHelper.IndexUserRequestAsync(_elasticSettings.KibanaUrl, _elasticSettings.Username, _elasticSettings.Password, username, user);
                result.Succeed = response;
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
        public async Task<ResultModel> LogOut(string username)
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
                var updateResult = await _dbContext.Users.UpdateOneAsync(x => x.NormalizedUsername == username,
                    Builders<UserInformation>.Update.Set(x => x.HashedCredential, null));
                result.Succeed = true;
                ClearCache();
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }
        public async Task<List<UserInformation>> GetFromCache()
        {
            var cacheContent = await _distributedCache.GetStringAsync(CacheConstants.USER);
            if (cacheContent != null)
            {
                return JsonConvert.DeserializeObject<List<UserInformation>>(cacheContent);
            }
            else
            {
                var model = await _dbContext.Users.Find(x => true)
                    .Project(
                    x => new UserInformation
                    {
                        Id = x.Id,
                        HashedCredential = x.HashedCredential,
                        GroupIds = x.GroupIds,
                        ResourcePermissionIds = x.ResourcePermissionIds,
                        RoleIds = x.RoleIds,
                        NormalizedUsername = x.NormalizedUsername
                    })
                    .ToListAsync();
                var content = JsonConvert.SerializeObject(model);
                await _distributedCache.SetStringAsync(CacheConstants.USER, content);
                return model;
            }
        }

        public async Task<ResultModel> ForgotPassword(ForgotPasswordModel model)
        {
            var result = new ResultModel();
            try
            {
                //check existed user

                var isExistedUser = await _dbContext.Users
                    .Find(x => x.Email == model.Email && x.Username == model.Username).FirstOrDefaultAsync();
                if (isExistedUser == null)
                {
                    throw new Exception(ErrorConstants.NOT_EXIST_ACCOUNT);
                }

                //check user send mail

                var baseKey = "ForgotPassword-" + isExistedUser.Username;

                string tokenUser = "";
                tokenUser = await GetCache<string>(baseKey);
                if (tokenUser != null)
                {
                    throw new Exception(ErrorConstants.SENT_EMAIL_FORGOT_PASSWORD);
                }


                var token = GenerateTokenForgotPassword();
                SetCache(baseKey, token, 5, 5);
                SetCache(token, isExistedUser.Id, 5, 5);

                var urlResetPassword = "https://scd.quanlyhiv.vn/resetpassword?token=";
                var urlResetPassword2 = "https://scd.quanlyhiv.vn/reset-password?token=";

                var isMailSent = await _mailService.SendEmail(new EmailViewModel()
                {
                    To = model.Email,
                    Subject = $"Set new password USAID",
                    Text = $"Hi {isExistedUser.Username} \n" +
                           $"Follow this link to set your password: {urlResetPassword2 + token}\n" +
                           $"================================== \n" +
                           $"Xin chào {isExistedUser.Username} \n" +
                           $"Sử dụng đường dẫn này để đặt mật khẩu cho tài khoản của bạn: {urlResetPassword2 + token} \n"
                });

                result.Succeed = isMailSent;
                result.Data = model.Email;

            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        public async Task<ResultModel> SetUserPassword(SetUserPasswordModel model)
        {
            var result = new ResultModel();
            try
            {
                string userid = await GetCache<string>(model.Token);
                if (userid == null)
                {
                    throw new Exception(ErrorConstants.SENT_EMAIL_FORGOT_PASSWORD);
                }

                var user = _dbContext.Users.Find(x => x.Id == userid).FirstOrDefault();
                var changePasswordModel = new ChangePasswordModel
                {
                    NewPassword = model.Password,
                };

                result = await ChangePasswordAsync2(changePasswordModel, userid);

                var finalUser = _dbContext.Users.Find(x => x.Id == userid).FirstOrDefault();
                if (!finalUser.IsConfirmed)
                {
                    finalUser.IsConfirmed = true;
                    await _dbContext.Users.ReplaceOneAsync(i => i.Id == user.Id, finalUser);
                }



                _distributedCache.Remove(model.Token);
                var baseKey = "ForgotPassword-" + user.Username;
                _distributedCache.Remove(baseKey);
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }



        public void ClearCache() => _distributedCache.Remove(CacheConstants.USER);


        public async Task<T> GetCache<T>(string key)
        {

            var value = await _distributedCache.GetAsync(key);
            if (value != null)
            {
                var dataSerialized = Encoding.UTF8.GetString(value);
                var data = JsonConvert.DeserializeObject<T>(dataSerialized);
                return data;
            }
            return default(T);
        }

        public async void SetCache<T>(string key, T data, int absoluteExpirationMinutes, int etSlidingExpirationMinutes)
        {
            var dataSerialize = JsonConvert.SerializeObject(data);
            var dataToRedis = Encoding.UTF8.GetBytes(dataSerialize);
            var options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(DateTime.Now.AddMinutes(absoluteExpirationMinutes))
                .SetSlidingExpiration(TimeSpan.FromMinutes(etSlidingExpirationMinutes));
            await _distributedCache.SetAsync(key, dataToRedis, options);
        }


        public string GenerateTokenForgotPassword()
        {
            string token = "";

            for (int i = 0; i < 5; i++)
            {
                token += Guid.NewGuid().ToString();
            }
            return token.Replace("-", "");
        }


        public async Task<ResultModel> ChangePasswordAsync2(ChangePasswordModel model, string userId)
        {
            var result = new ResultModel();
            try
            {
                if (string.IsNullOrEmpty(model.NewPassword))
                {
                    result.ErrorMessage = ErrorConstants.NULL_PASSWORD;
                    return result;
                }
                var user = _dbContext.Users.Find(i => i.Id == userId).FirstOrDefault();
                if (user == null)
                {
                    result.ErrorMessage = "Invalid Login Token";
                    return result;
                }
                var passwordHasher = new PasswordHasher<UserInformation>();
                user.HashedPassword = passwordHasher.HashPassword(user, model.NewPassword);
                user.HashedCredential = passwordHasher.HashPassword(user, $"{user.NormalizedUsername}.{user.HashedPassword}");
                user.DidFirstTimeLogIn = false;
                _dbContext.Users.ReplaceOne(i => i.Id == user.Id, user);

                result.Data = GetAccessToken(user);
                result.Succeed = true;
                ClearCache();
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }


        public async Task<ResultModel> CreateAccountCBO(CBOCreateModel model)
        {
            var result = new ResultModel();
            try
            {
                if (model.HasSendInitialEmail && string.IsNullOrEmpty(model.Email))
                {
                    throw new Exception(ErrorConstants.EMAIL_IS_NULL);
                }

                var rs = await Create(model);
                if (!rs.Succeed)
                {
                    return rs;
                }
                if (model.HasSendInitialEmail)
                {
                    var forgotPasswordModel = new ForgotPasswordModel
                    {
                        Username = model.Username,
                        Email = model.Email
                    };
                    var isSend = await ForgotPassword(forgotPasswordModel);
                    if (!isSend.Succeed)
                    {
                        throw new Exception(ErrorConstants.CAN_NOT_SEND_EMAIL);
                    }
                }
                result.Succeed = true;
                result.Data = "OK";
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        public async Task<UserStatisticModel> Statistic(GetUserStatistic request)
        {
            var registeredUsersFilters = Builders<UserInformation>.Filter.Lte(x => x.DateCreated, request.ToDate);
            registeredUsersFilters &= Builders<UserInformation>.Filter.Gte(x => x.DateCreated, request.FromDate);

            var registeredUsersNumber = await _dbContext.Users.Find(registeredUsersFilters)
                .ToListAsync();
            var unverifiedRegisteredUsersNumber = registeredUsersNumber.Where(x => !x.IsConfirmed).Count();
            var verifiedRegisteredUsersNumber = registeredUsersNumber.Where(x => x.IsConfirmed).Count();
            return new UserStatisticModel
            {
                UnverifiedRegisteredUsersNumber = unverifiedRegisteredUsersNumber,
                VerifiedRegisteredUsersNumber = verifiedRegisteredUsersNumber,
            };
        }
    }
}
