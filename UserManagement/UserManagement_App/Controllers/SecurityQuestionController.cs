using Data.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System.Threading.Tasks;

namespace UserManagement_App.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SecurityQuestionController : Controller
    {
        private readonly ISecurityQuestionService _service;

        public SecurityQuestionController(ISecurityQuestionService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var result = await _service.GetAll();
            if (!result.Succeed)
            {
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(string id)
        {
            var result = await _service.Get(id);
            if (!result.Succeed)
            {
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result);
        }
        [HttpPost()]
        public async Task<IActionResult> Create(CreateSecurityQuestionModel model)
        {
            var result = await _service.Create(model);
            if (!result.Succeed)
            {
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result);
        }
        [HttpPut()]
        public async Task<IActionResult> Update(UpdateSecurityQuestionModel model)
        {
            var result = await _service.Update(model);
            if (!result.Succeed)
            {
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _service.Delete(id);
            if (!result.Succeed)
            {
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result);
        }
    }
}
