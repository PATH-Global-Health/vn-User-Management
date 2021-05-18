using System;
using Data.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;

namespace UserManagement_App.Controllers
{
    [Route("/api/Users/Profiles")]
    public class UserProfilesController : Controller
    {
        private readonly IUserProfileService _userProfileService;

        public UserProfilesController(IUserProfileService userProfileService)
        {
            _userProfileService = userProfileService;
        }

        [HttpGet]
        public IActionResult Search(string name, string phoneNumber, string email, DateTime? dateOfBirth, bool hasYearOfBirthOnly, int pageSize, int pageIndex)
        {
            var results = _userProfileService.Search(name, phoneNumber, email, dateOfBirth, hasYearOfBirthOnly, pageSize, pageIndex);
            return Ok(results);
        }

        [HttpPost]
        public IActionResult Post([FromBody] UserProfileCreateModel model)
        {
            var result = _userProfileService.Create(model);
            if (result.Succeed) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [HttpDelete("{key}")]
        public IActionResult Delete([FromRoute] string key)
        {
            var result = _userProfileService.Delete(key);
            if (result.Succeed) return Ok();
            return BadRequest(result.ErrorMessage);
        }
    }
}
