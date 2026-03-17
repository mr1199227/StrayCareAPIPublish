using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StrayCareAPI.Services;
using System.Security.Claims;

namespace StrayCareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var stats = await _userService.GetDashboardStatsAsync(userId.Value);
            return Ok(new
            {
                animalCount = stats.AnimalCount,
                adoptionCount = stats.AdoptionCount,
                feedingCount = stats.FeedingCount,
                totalDonated = stats.TotalDonated
            });
        }

        [HttpGet("my-animals")]
        public async Task<IActionResult> GetMyAnimals()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var animals = await _userService.GetMyAnimalsAsync(userId.Value);
            return Ok(animals);
        }

        [HttpGet("my-adoptions")]
        public async Task<IActionResult> GetMyAdoptions()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var adoptions = await _userService.GetMyAdoptionsAsync(userId.Value);
            return Ok(adoptions);
        }

        [HttpGet("my-feedings")]
        public async Task<IActionResult> GetMyFeedings()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var feedings = await _userService.GetMyFeedingsAsync(userId.Value);
            return Ok(feedings);
        }

        [HttpGet("my-financials")]
        public async Task<IActionResult> GetMyFinancials()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var financials = await _userService.GetMyFinancialsAsync(userId.Value);
            return Ok(new
            {
                donations = financials.Donations,
                topups = financials.Topups
            });
        }

        [HttpGet("recent-activities")]
        public async Task<IActionResult> GetRecentActivities()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var activities = await _userService.GetRecentActivitiesAsync(userId.Value);
            return Ok(activities);
        }

        private int? GetCurrentUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return null;
            }

            return userId;
        }
    }
}
