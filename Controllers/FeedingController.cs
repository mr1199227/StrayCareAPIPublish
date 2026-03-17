using Microsoft.AspNetCore.Mvc;
using StrayCareAPI.DTOs;
using StrayCareAPI.Services;

namespace StrayCareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedingController : ControllerBase
    {
        private readonly IFeedingService _feedingService;

        public FeedingController(IFeedingService feedingService)
        {
            _feedingService = feedingService;
        }

        [HttpPost("createFeedingLog")]
        public async Task<IActionResult> CreateFeedingLog([FromForm] CreateFeedingLogDto dto)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }
            dto.UserId = userId;

            var result = await _feedingService.CreateFeedingLogAsync(dto);

            if (!result.Success)
            {
                if (result.Message == "Animal not found") return NotFound(new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message, data = result.Data });
        }
    }
}
