using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StrayCareAPI.DTOs;
using StrayCareAPI.Services;
using System.Security.Claims;

namespace StrayCareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdoptionController : ControllerBase
    {
        private readonly IAdoptionService _adoptionService;

        public AdoptionController(IAdoptionService adoptionService)
        {
            _adoptionService = adoptionService;
        }

        // POST: api/adoption/apply
        [HttpPost("apply")]
        [Authorize(Roles = "User,Volunteer")]
        public async Task<IActionResult> SubmitApplication([FromBody] AdoptionApplicationDto dto)
        {
            // Get User ID from Claims
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                 return Unauthorized(new { message = "Invalid user token." });
            }

            var result = await _adoptionService.SubmitApplicationAsync(userId, dto);
            if (!result.Success)
            {
                if (result.Message == "Animal not found.") return NotFound(new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }

        // GET: api/adoption/my-applications
        [HttpGet("my-applications")]
        [Authorize]
        public async Task<IActionResult> GetMyApplications()
        {
            // Get User ID from Claims
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                 return Unauthorized(new { message = "Invalid user token." });
            }

            var applications = await _adoptionService.GetMyApplicationsAsync(userId);
            return Ok(applications);
        }
    }
}
