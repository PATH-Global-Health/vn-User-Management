using Data.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System.Collections.Generic;
using System.Security.Claims;
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

        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetAll()
        {
            var users = _userService.GetAll();
            return Ok(users);
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Create([FromBody] UserCreateModel model)
        {
            var result = _userService.Create(model);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var result = await _userService.Login(model.Username, model.Password, model.PermissionQuery);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [HttpPut("ChangePassword")]
        public IActionResult ChangePassword([FromBody] ChangePasswordModel model)
        {
            var userId = User.GetId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var result = _userService.ChangePassword(model, User.GetId());
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
            var permissions = _permissionService.GetUiPermissions(userId, Data.Enums.HolderType.User);

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

        [Authorize]
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordModel model)
        {
            var result = await _userService.ResetPassword(model, User?.FindFirst("Username")?.Value);
            if (result.Succeed) return Ok();
            return BadRequest(result.ErrorMessage);
        }
    }
}
