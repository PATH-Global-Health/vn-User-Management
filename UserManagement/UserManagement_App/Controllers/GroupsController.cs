using Data.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;

namespace UserManagement_App.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _groupService;
        private readonly IPermissionsService _permissionService;

        public GroupsController(IGroupService groupService, IPermissionsService permissionService)
        {
            _groupService = groupService;
            _permissionService = permissionService;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var roles = _groupService.GetAll();
            return Ok(roles);
        }

        [HttpGet("{id}")]
        public IActionResult Get(string id)
        {
            var role = _groupService.Get(id);
            if (role != null) return Ok(role);
            return BadRequest();
        }

        [HttpPost]
        public IActionResult Post([FromBody] GroupCreateModel model)
        {
            var result = _groupService.Create(model);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [HttpPut("{id}/Users")]
        public IActionResult AddUsers([FromBody] AddUsersToRoleModel model, [FromRoute] string id)
        {
            var result = _groupService.AddUsers(id, model.UserIds);
            if (result.Succeed) return Ok();
            return BadRequest(result.ErrorMessage);
        }

        [HttpDelete("{id}/Users/{userId}")]
        public IActionResult RemoveUser([FromRoute] string id, [FromRoute] string userId)
        {
            var result = _groupService.RemoveUser(id, userId);
            if (result.Succeed) return Ok();
            return BadRequest(result.ErrorMessage);
        }

        [HttpPut("{id}/Roles")]
        public IActionResult AddRoles([FromBody] AddRolesToGroupModel model, [FromRoute] string id)
        {
            var result = _groupService.AddRoles(id, model.RoleIds);
            if (result.Succeed) return Ok();
            return BadRequest(result.ErrorMessage);
        }

        [HttpDelete("{id}/Roles/{RoleId}")]
        public IActionResult RemoveRole([FromRoute] string id, [FromRoute] string RoleId)
        {
            var result = _groupService.RemoveRole(id, RoleId);
            if (result.Succeed) return Ok();
            return BadRequest(result.ErrorMessage);
        }


        [HttpGet("{id}/Users")]
        public IActionResult GetUsers([FromRoute] string id)
        {
            var users = _groupService.GetUsers(id);
            return Ok(users);
        }

        [HttpGet("{id}/Roles")]
        public IActionResult GetRoles([FromRoute] string id)
        {
            var roles = _groupService.GetRoles(id);
            return Ok(roles);
        }

        [HttpGet("{id}/Permissions/Resource")]
        public IActionResult GetResourcePermissions([FromRoute] string id)
        {
            var permissions = _permissionService.GetResourcePermissions(id, Data.Enums.HolderType.Group);
            return Ok(permissions);
        }

        [HttpGet("{id}/Permissions/Ui")]
        public IActionResult GetUiPermissions([FromRoute] string id)
        {
            var permissions = _permissionService.GetUiPermissions(id, Data.Enums.HolderType.Group);
            return Ok(permissions);
        }
    }
}
