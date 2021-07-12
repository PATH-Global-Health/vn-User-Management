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
        public async Task<IActionResult> Post([FromBody] ModuleCreateModel model)
        {
            var result = await _apiModuleService.Create(model.Host, model.ModuleName, model.UpstreamName);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet]
        public IActionResult Get(int pageIndex, int pageSize = 20)
        {
            var result = _apiModuleService.GetAll(pageSize, pageIndex);
            return Ok(result);
        }

        [HttpGet("Document")]
        public async Task<IActionResult> GetDocument(string swaggerHost, string serverUrl, bool removeApiPathPrefix = false)
        {
            var result = await _apiModuleService.GetSwaggerDocument(swaggerHost, serverUrl, removeApiPathPrefix);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet("{id}")]
        public IActionResult Get([FromRoute] Guid id)
        {
            var result = _apiModuleService.GetDetail(id.ToString());
            if (result != null) return Ok(result);
            return BadRequest("Record not existed");
        }
    }
}
