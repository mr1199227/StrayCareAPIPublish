using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StrayCareAPI.DTOs;
using StrayCareAPI.Services;
using System.Security.Claims;

namespace StrayCareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Shelter")]
    public class ShelterController : ControllerBase
    {
        private readonly IShelterService _shelterService;

        public ShelterController(IShelterService shelterService)
        {
            _shelterService = shelterService;
        }

        // POST: api/shelter/intake/{animalId}
        [HttpPost("intake/{animalId}")]
        public async Task<IActionResult> IntakeAnimal(int animalId, [FromForm] ShelterIntakeDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            var result = await _shelterService.IntakeAnimalAsync(animalId, userId, dto);
            if (!result.Success)
            {
                if (result.Message == "Shelter profile not found.") return BadRequest(new { message = result.Message });
                if (result.Message == "Animal not found.") return NotFound(new { message = result.Message });
                return StatusCode(500, new { message = result.Message }); // Fallback
            }

            return Ok(new { message = result.Message, data = result.Data });
        }

        // GET: api/shelter/applications
        [HttpGet("applications")]
        public async Task<IActionResult> GetApplications()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                 return Unauthorized();
            }
            
            var applications = await _shelterService.GetApplicationsAsync(userId);
            return Ok(applications);
        }

        // POST: api/shelter/applications/{appId}/approve
        [HttpPost("applications/{appId}/approve")]
        public async Task<IActionResult> ApproveApplication(int appId)
        {
             var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                 return Unauthorized();
            }

            var result = await _shelterService.ApproveApplicationAsync(appId, userId);
            if (!result.Success)
            {
                if (result.Message == "Application not found.") return NotFound(new { message = result.Message });
                if (result.Message == "You do not have permission to approve this application.") return StatusCode(403, new { message = result.Message });
                return StatusCode(500, new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }

        // POST: api/shelter/applications/{appId}/reject
        [HttpPost("applications/{appId}/reject")]
        public async Task<IActionResult> RejectApplication(int appId, [FromBody] RejectApplicationDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                 return Unauthorized();
            }

            var result = await _shelterService.RejectApplicationAsync(appId, userId, dto);
            if (!result.Success)
            {
                if (result.Message == "Application not found.") return NotFound(new { message = result.Message });
                if (result.Message == "You do not have permission to reject this application.") return StatusCode(403, new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }

        // GET: api/shelter/animals
        [HttpGet("animals")]
        public async Task<IActionResult> ViewOwnAnimals()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            var animals = await _shelterService.ViewOwnAnimalsAsync(userId);
            return Ok(animals);
        }

        // GET: api/shelter/nearby-strays?radius=10
        [HttpGet("nearby-strays")]
        public async Task<IActionResult> GetNearbyStrays([FromQuery] double radius = 10.0)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            var result = await _shelterService.GetNearbyStraysAsync(userId, radius);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message, data = result.Data });
        }
    }
}
