using Data.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;

namespace UserManagement_App.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;
        private readonly IPermissionsService _permissionService;

        public RolesController(IRoleService roleService, IPermissionsService permissionService)
        {
            _roleService = roleService;
            _permissionService = permissionService;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var roles = _roleService.GetAll();
            return Ok(roles);
        }

        [HttpGet("{id}")]
        public IActionResult Get(string id)
        {
            var role = _roleService.Get(id);
            if (role != null) return Ok(role);
            return BadRequest();
        }

        [HttpPost]
        public IActionResult Post([FromBody] RoleCreateModel model)
        {
            var result = _roleService.Create(model);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [HttpPut("{id}/Users")]
        public IActionResult AddUsers([FromBody] AddUsersToRoleModel model, [FromRoute] string id)
        {
            var result = _roleService.AddUsers(id, model.UserIds);
            if (result.Succeed) return Ok();
            return BadRequest(result.ErrorMessage);
        }

        [HttpPut("{id}/Users/{userId}")]
        public IActionResult AddUsers([FromRoute] string id, [FromRoute] string userId)
        {
            var result = _roleService.RemoveUser(id, userId);
            if (result.Succeed) return Ok();
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet("{id}/Users")]
        public IActionResult GetUsers([FromRoute] string id)
        {
            var users = _roleService.GetUsers(id);
            return Ok(users);
        }

        [HttpGet("{id}/Groups")]
        public IActionResult GetGroups([FromRoute] string id)
        {
            var groups = _roleService.GetGroups(id);
            return Ok(groups);
        }

        [HttpGet("{id}/Permissions/Resource")]
        public IActionResult GetResourcePermissions([FromRoute] string id)
        {
            var permissions = _permissionService.GetResourcePermissions(id, Data.Enums.HolderType.Role);
            return Ok(permissions);
        }

        [HttpGet("{id}/Permissions/Ui")]
        public IActionResult GetUiPermissions([FromRoute] string id)
        {
            var permissions = _permissionService.GetUiPermissions(id, Data.Enums.HolderType.Role);
            return Ok(permissions);
        }
    }
}
