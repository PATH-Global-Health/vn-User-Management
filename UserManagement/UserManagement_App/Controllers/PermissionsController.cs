using Data.Enums;
using Data.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using UserManagement_App.Extensions;

namespace UserManagement_App.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PermissionsController : ControllerBase
    {
        private readonly IPermissionsService _permissionsService;
        public PermissionsController(IPermissionsService permissionsService)
        {
            _permissionsService = permissionsService;
        }

        #region Resources

        [HttpGet("Resource")]
        public IActionResult ResourcePermission()
        {
            var result = _permissionsService.GetAllResourcePermission();
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [HttpPost("Resource")]
        public IActionResult AddResourcePermission([FromBody] AddResourcePermissionModel model)
        {
            ResultModel result = _permissionsService.AddPermission(model.HolderId, model.HolderType, model.Permission);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [HttpDelete("Resource/{permissionId}")]
        public IActionResult RemoveResourcePermission(string permissionId, HolderType holderType, string holderId)
        {
            ResultModel result = _permissionsService.RemovePermission(permissionId, true, holderId, holderType);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [HttpPost("Resource/Validate")]
        public IActionResult ValidateResourcePermission([FromBody] ResourcePermissionValidationModel model)
        {
            var userId = User.GetId();
            var result = _permissionsService.Validate(model, userId);
            if (result.Succeed) return Ok();
            return StatusCode(401);
        }

        [HttpGet("Resource/Success")]
        public IActionResult ValidateSuccess()
        {
            return Ok();
        }
        #endregion

        #region Ui 
        [HttpPost("Ui")]
        public IActionResult AddUi([FromBody] AddUiPermissionModel model)
        {
            ResultModel result = _permissionsService.AddPermission(model.HolderId, model.HolderType, model.Permission);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [HttpDelete("Ui/{permissionId}")]
        public IActionResult RemoveUiPermission(string permissionId, HolderType holderType, string holderId)
        {
            ResultModel result = _permissionsService.RemovePermission(permissionId, false, holderId, holderType);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet("Ui")]
        public IActionResult UiPermission()
        {
            var result = _permissionsService.GetAllUiPermission();
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }
        #endregion

    }
}
