using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public IActionResult GetAll()
        {
            var result = _service.GetAll();
            return Ok(result);
        }
    }
}
