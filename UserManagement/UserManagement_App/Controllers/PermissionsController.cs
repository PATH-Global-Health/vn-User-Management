using System;
using System.Collections.Generic;
using Data.Enums;
using Data.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using UserManagement_App.Extensions;

namespace UserManagement_App.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
        //[HttpPut("Resource/ChangeAPIAuthorization/{permissionId}/{isAuthorized}")]
        //public IActionResult ChangeAPIAuthorizationResourcePermission(string permissionId, bool isAuthorized)
        //{
        //    ResultModel result = _permissionsService.ChangeAPIAuthorizationResourcePermission(permissionId, isAuthorized);
        //    if (result.Succeed) return Ok(result.Data);
        //    return BadRequest(result.ErrorMessage);
        //}
        [HttpPut("Resource")]
        public IActionResult CreateResourcePermission([FromBody] ResourcePermissionCreateModel model)
        {
            ResultModel result = _permissionsService.CreatePermission(model);
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

        [AllowAnonymous]
        [HttpPost("Resource/Validate")]
        public IActionResult ValidateResourcePermission([FromBody] ResourcePermissionValidationModel model)
        {
            var userId = User.GetId();
            var result = _permissionsService.Validate(model, userId);
            if (result.Succeed) return Ok();
            return StatusCode(401);
        }

        //[HttpGet("Resource/Success")]
        //public IActionResult ValidateSuccess()
        //{
        //    return Ok();
        //}


        [HttpPut("Resource/Batch")]
        public IActionResult CreateResourcePermissions([FromBody] List<ResourcePermissionCreateModel> models)
        {
            ResultModel result = _permissionsService.CreatePermissions(models);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [HttpPost("Resource/Batch")]
        public IActionResult AddResources([FromBody] AddBatchResourcePermissionModel model)
        {
            ResultModel result = _permissionsService.AddPermissions(model.HolderId, model.HolderType, model.Permissions);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }
        [HttpPost("Resource/BatchIds")]
        public IActionResult AddResourcesByIds([FromBody] AddBatchIdsPermissionModel model)
        {
            ResultModel result = _permissionsService.AddResourcePermissions(model.HolderId, model.HolderType, model.Ids);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }
        #endregion

        #region Ui 
        [HttpGet("Ui")]
        public IActionResult UiPermission()
        {
            var result = _permissionsService.GetAllUiPermission();
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [HttpPost("Ui")]
        public IActionResult AddUi([FromBody] AddUiPermissionModel model)
        {
            ResultModel result = _permissionsService.AddPermission(model.HolderId, model.HolderType, model.Permission);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [HttpPost("Ui/Batch")]
        public IActionResult AddUis([FromBody] AddBatchUIPermissionModel model)
        {
            ResultModel result = _permissionsService.AddPermissions(model.HolderId, model.HolderType, model.Permissions);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [HttpPut("Ui")]
        public IActionResult CreateUiPermission([FromBody] UiPermissionCreateModel model)
        {
            ResultModel result = _permissionsService.CreatePermission(model);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [HttpPut("Ui/Batch")]
        public IActionResult CreateUiPermissions([FromBody] List<UiPermissionCreateModel> models)
        {
            ResultModel result = _permissionsService.CreatePermissions(models);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }
        [HttpPost("Ui/BatchIds")]
        public IActionResult AddUIByIds([FromBody] AddBatchIdsPermissionModel model)
        {
            ResultModel result = _permissionsService.AddUIPermissions(model.HolderId, model.HolderType, model.Ids);
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

        #endregion

    }
}
