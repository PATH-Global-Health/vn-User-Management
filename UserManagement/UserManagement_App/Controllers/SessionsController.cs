using Data.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System;
using System.Threading.Tasks;
using UserManagement_App.Extensions;

namespace UserManagement_App.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SessionsController : ControllerBase
    {
        private readonly ISessionService _sessionService;

        public SessionsController(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        [HttpGet("Statistic")]
        public async Task<IActionResult> Statistic([FromQuery] GetStatisticRequest request)
        {
            try
            {
                var result = await _sessionService.Statistic(request);
                return Ok(result);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] CreateSessionRequest request)
        {
            try
            {
                var result = await _sessionService.Create(request, User.GetId());
                return Ok(result);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPut("End/{id}")]
        public async Task<IActionResult> EndAsync(string id)
        {
            try
            {
                await _sessionService.End(id);
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

    }
}
