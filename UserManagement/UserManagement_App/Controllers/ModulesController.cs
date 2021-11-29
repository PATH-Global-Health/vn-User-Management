using Data.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System;
using System.Threading.Tasks;

namespace UserManagement_App.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModulesController : ControllerBase
    {
        private readonly IApiModuleService _apiModuleService;

        public ModulesController(IApiModuleService apiModuleService)
        {
            _apiModuleService = apiModuleService;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ModuleCreateModel model, bool doPathReplacement = true)
        {
            var result = await _apiModuleService.Create(model.SwaggerHost, model.ReplacementHost, model.ModuleName, doPathReplacement);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet]
        public IActionResult Get(int pageIndex, int pageSize = 20)
        {
            var result = _apiModuleService.GetAll(pageSize, pageIndex);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public IActionResult Get([FromRoute] Guid id)
        {
            var result = _apiModuleService.GetDetail(id.ToString());
            if (result != null) return Ok(result);
            return BadRequest("Record not existed");
        }

        [HttpGet("{id}/SwaggerDocument")]
        public async Task<IActionResult> GetSwaggerDocument([FromRoute] Guid id)
        {
            var result = await _apiModuleService.GetSwaggerDocument(id.ToString());
            if (result != null) return Ok(result);
            return BadRequest("Record not existed");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteApi([FromRoute] Guid id)
        {
            var result = await _apiModuleService.Delete(id.ToString());
            if (result != null) return Ok();
            return BadRequest("Record not existed");
        }
    }
}
