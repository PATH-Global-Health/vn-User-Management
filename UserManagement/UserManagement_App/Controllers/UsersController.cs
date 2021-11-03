using Data.ViewModels;
using Data.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserManagement_App.Extensions;

namespace UserManagement_App.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        private readonly IPermissionsService _permissionService;

        public UsersController(IUserService userService, IPermissionsService permissionService)
        {
            _userService = userService;
            _permissionService = permissionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string username, int? pageIndex, int? pageSize)
        {
            if (!pageIndex.HasValue)
            {
                pageIndex = 0;
            }
            if (!pageSize.HasValue || pageSize.Value == 0) pageSize = 20;

            var result = await _userService.GetAll(username, pageSize.Value, pageIndex.Value);
            return Ok(result);

        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] UserCreateModel model)
        {
            var result = await _userService.Create(model);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var result = await _userService.Login(model);
            if (result.Succeed == true && result.ErrorMessage == "426")
            {
                return StatusCode(426, result.Data);
            }
            else if (result.Succeed && string.IsNullOrEmpty(result.ErrorMessage))
            {
                return Ok(result.Data);
            }
            return BadRequest(result.ErrorMessage);
        }

        [HttpPut("ChangePassword")]
        public IActionResult ChangePassword([FromBody] ChangePasswordModel model)
        {
            var userId = User.GetId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var result = _userService.ChangePassword(model, User.GetId());
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }
        [HttpPut()]
        public async Task<IActionResult> UpdateInfoAsync([FromBody] UserUpdateModel model)
        {
            var userId = User.GetId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var result = await _userService.UpdateUser(model, User.GetId());
            if (result.Succeed) return Ok();
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet("Tools/ResetDefaultPassword")]
        public IActionResult ResetDefault(string username)
        {
            var result = _userService.ResetDefaultPassword(username);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [HttpPost("Tools/ImportUsers")]
        public IActionResult ImportUsers([FromBody] List<ImportUserModel> model)
        {
            var result = _userService.ImportUsers(model);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet("Permissions/Ui")]
        public IActionResult GetUiPermissions()
        {
            var userId = User.GetId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var permissions = _permissionService.GetUserUiPermissions(userId);

            return Ok(permissions);
        }

        [HttpGet("Permissions/Resource")]
        public IActionResult GetResourcePermissions()
        {
            var userId = User.GetId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var permissions = _permissionService.GetResourcePermissions(userId, Data.Enums.HolderType.User);

            return Ok(permissions);
        }

        [HttpGet("Roles")]
        public IActionResult GetUserRoles()
        {
            var userId = User.GetId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var roles = _userService.GetRoles(userId);
            return Ok(roles);
        }

        [HttpGet("Groups")]
        public IActionResult GetUserGroups()
        {
            var userId = User.GetId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var groups = _userService.GetGroups(userId);
            return Ok(groups);
        }

        [HttpGet("{id}/Permissions/Ui")]
        public IActionResult GetUiPermissions([FromRoute] string id)
        {
            var permissions = _permissionService.GetUiPermissions(id, Data.Enums.HolderType.User);
            return Ok(permissions);
        }

        [HttpGet("{id}/Permissions/Resource")]
        public IActionResult GetResourcePermissions([FromRoute] string id)
        {
            var permissions = _permissionService.GetResourcePermissions(id, Data.Enums.HolderType.User);
            return Ok(permissions);
        }

        [HttpGet("{id}/Groups")]
        public IActionResult GetUserGroups([FromRoute] string id)
        {
            var groups = _userService.GetGroups(id);
            return Ok(groups);
        }

        [HttpGet("{id}/Roles")]
        public IActionResult GetUserRoles([FromRoute] string id)
        {
            var roles = _userService.GetRoles(id);
            return Ok(roles);
        }

        [AllowAnonymous]
        [HttpPost("ResetPassword/GenerateOTP")]
        public async Task<IActionResult> GenerateResetPasswordOTPAsync([FromBody] GenerateResetPasswordOTPModel model)
        {
            var result = await _userService.GenerateResetPasswordOTP(model);
            if (result.Succeed) return Ok();
            return BadRequest(result.ErrorMessage);
        }
        [AllowAnonymous]
        [HttpPost("ResetPassword/ConfirmOTP")]
        public async Task<IActionResult> ConfirmResetPasswordOTPAsync(ConfirmResetPasswordOTPModel model)
        {
            var result = await _userService.ConfirmResetPasswordOTP(model);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }
        [AllowAnonymous]
        [HttpPost("ResetPassword/ConfirmSecurityQuestion")]
        public async Task<IActionResult> ConfirmSecurityQuestion(ConfirmResetPasswordSecurityQuestionModel model)
        {
            var result = await _userService.ConfirmResetPasswordSecurityQuestion(model);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet("ValidateCredential")]
        public async Task<IActionResult> ValidateCredential()
        {
            var userId = User.GetId();
            var credential = User.GetCredential();

            var result = await _userService.ValidateTokenCredential(userId, credential);
            if (result.Succeed) return Ok();
            return Unauthorized();
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordModel model)
        {
            var result = await _userService.ResetPassword(model, User?.FindFirst("Username")?.Value);
            if (result.Succeed) return Ok();
            return BadRequest(result.ErrorMessage);
        }
        [HttpPost("ChangeSecurityQuestionAnswer")]
        public async Task<IActionResult> ChangeSecurityQuestionAnswer([FromBody] ChangeSecurityQuestionAnswerModel model)
        {
            var result = await _userService.ChangeSecurityQuestionAnswer(model, User?.FindFirst("Username")?.Value);
            if (result.Succeed) return Ok();
            return BadRequest(result.ErrorMessage);
        }

        [HttpPut("Tools/UpdateCredentials")]
        public void UpdateCredentials()
        {
            _userService.UpdateTokenCredentail();
        }

        [HttpPut("{id}/Disable")]
        public IActionResult DisableUser(string id)
        {
            var result = _userService.DisableUser(id);
            if (result.Succeed) return Ok();
            return BadRequest(result.ErrorMessage);
        }

        [HttpPut("{id}/Enable")]
        public IActionResult EnableUser(string id)
        {
            var result = _userService.EnableUser(id);
            if (result.Succeed) return Ok();
            return BadRequest(result.ErrorMessage);
        }
        [HttpGet("GetUserInfo")]
        public async Task<IActionResult> GetUserInfo()
        {
            var result = await _userService.GetUserInfoAsync(User?.FindFirst("Username")?.Value);
            if (result.Succeed) return Ok(result);
            return BadRequest(result.ErrorMessage);
        }

        [AllowAnonymous]
        [HttpPost("LoginWithFacebook")]
        public async Task<IActionResult> LoginWithFacebook([FromQuery] string accessToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            var login = await _userService.LoginWithFacebookAsync(accessToken);
            if (login.Succeed)
            {
                return Ok(login.Data);
            }
            return BadRequest(login.ErrorMessage);
        }
        [AllowAnonymous]
        [HttpPost("LoginWithGoogle")]
        public async Task<IActionResult> LoginWithFacebookLoginWithGoogle([FromQuery] string idToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            var login = await _userService.LoginWithGoogleAsync(idToken);
            if (login.Succeed)
            {
                return Ok(login.Data);
            }
            return BadRequest(login.ErrorMessage);
        }

        [AllowAnonymous]
        [HttpPost("SendOTPVerification")]
        public async Task<IActionResult> SendOTPVerification(string phoneNumber)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            var login = await _userService.SendOTPVerification(phoneNumber);
            if (login.Succeed)
            {
                return Ok(login.Data);
            }
            return BadRequest(login.ErrorMessage);
        }

        [AllowAnonymous]
        [HttpPost("VerifyOTPOfPhoneNumber")]
        public async Task<IActionResult> VerifyOTPOfPhoneNumber([FromBody] VerifyOTPOfPhoneNumberRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            var login = await _userService.VerifyOTPOfPhoneNumber(request);
            if (login.Succeed)
            {
                return Ok(login.Data);
            }
            return BadRequest(login.ErrorMessage);
        }

        [AllowAnonymous]
        [HttpPost("AnonymousLogin")]
        public async Task<IActionResult> AnonymousLogin()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            var login = await _userService.AnonymousLogin();
            if (login.Succeed)
            {
                return Ok(login.Data);
            }
            return BadRequest(login.ErrorMessage);
        }
        [AllowAnonymous]
        [HttpPost("TestRabbitMQ")]
        public IActionResult TestRabbitMQ(string username)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            _userService.TestRabbitMQ(username);
            return Ok();
        }
    }
}
