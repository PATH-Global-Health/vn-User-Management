﻿using AutoMapper;
using Data.DataAccess;
using Data.MongoCollections;
using Data.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Service.Implementations
{
    public class UserService : IUserService
    {
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;

        public UserService(IMapper mapper, IConfiguration configuration, ApplicationDbContext dbContext, IHttpClientFactory httpClientFactory)
        {
            _mapper = mapper;
            _configuration = configuration;
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
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

        public ResultModel Create(UserCreateModel model)
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
                    SecurityQuestionId = model.SecurityQuestion.Id,
                    SecurityQuestionAnswer = model.SecurityQuestion.Answer,
                };
                user.HashedPassword = passwordHasher.HashPassword(user, model.Password);

                _dbContext.Users.InsertOne(user);
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

        public async Task<ResultModel> Login(string username, string password, PermissionQuery permissionQuerie)
        {
            var result = new ResultModel();
            try
            {
                username = username.ToUpper();
                var user = _dbContext.Users.Find(i => i.NormalizedUsername == username).FirstOrDefault();
                if (user == null)
                {
                    #region Check on old system **Disabled**
                    if (await UserIsOnOldLoginSystem(username, password))
                    {
                        var userCreateModel = new UserCreateModel
                        {
                            Username = username,
                            Password = password,
                            FullName = username
                        };
                        var createUserResult = Create(userCreateModel);
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
                var passwordVerificationResult = passwordHasher.VerifyHashedPassword(user, user.HashedPassword, password);
                if (passwordVerificationResult == PasswordVerificationResult.Failed)
                {
                    result.ErrorMessage = "Username or password is incorrect";
                    return result;
                }
                var accessToken = GetAccessToken(user);

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
                permissions = permissions.Union(user.UiPermissions.Select(s => new Permission { Code = s.Code })).ToList();
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
                model.Username = model.Username.ToUpper();
                var user = await _dbContext.Users.Find(i => i.NormalizedUsername == model.Username).FirstOrDefaultAsync();
                if (user == null)
                {
                    result.ErrorMessage = "Username is incorrect";
                }
                else
                {
                    if (!string.IsNullOrEmpty(model.PhoneNumber))
                    {
                        if (user.PhoneNumber.Equals(model.PhoneNumber))
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
                        if (user.Email.Equals(model.Email))
                        {
                            result.Succeed = true;
                        }
                        else
                        {
                            result.ErrorMessage = "Email does not match";
                        }
                    }
                    else if (model.SecurityQuestion != null)
                    {
                        if (model.SecurityQuestion.Id.Equals(user.SecurityQuestionId) && model.SecurityQuestion.Answer.Equals(user.SecurityQuestionAnswer))
                        {
                            result.Succeed = true;
                        }
                        else
                        {
                            result.ErrorMessage = "Security Question doesn't match";
                        }
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
                model.Username = model.Username.ToUpper();
                var user = await _dbContext.Users.Find(i => i.NormalizedUsername == model.Username).FirstOrDefaultAsync();
                // 123456 is default
                // check user.ResetPasswordOTP with OTP later
                if (!model.OTP.Equals("123456"))
                {
                    result.ErrorMessage = "OTP is incorrect";
                }
                else
                {
                    var accessToken = GetAccessToken(user);
                    result.Data = accessToken;
                    result.Succeed = true;
                }
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        public async Task<ResultModel> ResetPassword(ResetPasswordModel model)
        {
            var result = new ResultModel();
            try
            {
                //model.Username = model.Username.ToUpper();
                //var user = _dbContext.Users.Find(i => i.NormalizedUsername == model.Username).FirstOrDefault();
                // check user.resetPasswordToken with resetPasswordModel.ResetPasswordToken later
                // change user.Password to new password
                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }
    }
}
