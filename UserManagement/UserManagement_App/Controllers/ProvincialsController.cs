using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System.Collections.Generic;

namespace UserManagement_App.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProvincialsController : ControllerBase
    {
        private readonly IProvincialService _provincialService;

        public ProvincialsController(IProvincialService provincialService)
        {
            _provincialService = provincialService;
        }

        //[HttpGet]
        //public IActionResult GetAll()
        //{
        //    var provincials = _provincialService.GetAll();
        //    return Ok(provincials);
        //}

        //[HttpGet("/Users/{id}/Provincials")]
        //public IActionResult GetAccountProvincialInfo([FromRoute] string id)
        //{
        //    var result = _provincialService.GetProvincials(id);
        //    return Ok(result);
        //}

        //[HttpPut("/Users/{id}/Provincials")]
        //public IActionResult AddAccountProvincials([FromRoute] string id, [FromBody] List<string> provinceIds)
        //{
        //    var result = _provincialService.AddProvincialInfo(id, provinceIds);
        //    if (result.Succeed) return Ok();
        //    return BadRequest(result.ErrorMessage);
        //}

        //[HttpDelete("/Users/{id}/Provincials/{provinceId}")]
        //public IActionResult RemoveAccountProvincial([FromRoute] string id, [FromBody] string provinceId)
        //{
        //    var result = _provincialService.RemoveProvincialInfo(id, provinceId);
        //    if (result.Succeed) return Ok();
        //    return BadRequest(result.ErrorMessage);
        //}

        //[HttpPut("EnsurePopulateData")]
        //public void EnsurePopulateData()
        //{
        //    _provincialService.EnsureDataPopulated();
        //}
    }
}
