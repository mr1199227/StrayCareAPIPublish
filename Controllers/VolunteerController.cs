using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StrayCareAPI.DTOs;
using StrayCareAPI.Services;
using System.Security.Claims;

namespace StrayCareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Volunteer")]
    public class VolunteerController : ControllerBase
    {
        private readonly IVolunteerService _volunteerService;

        public VolunteerController(IVolunteerService volunteerService)
        {
            _volunteerService = volunteerService;
        }

        // GET: api/volunteer/my-tasks
        [HttpGet("my-tasks")]
        public async Task<IActionResult> GetMyTasks()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            var tasks = await _volunteerService.GetMyTasksAsync(userId);
            return Ok(tasks);
        }

        // GET: api/volunteer/available
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableTasks()
        {
            var tasks = await _volunteerService.GetAvailableTasksAsync();
            return Ok(tasks);
        }

        // POST: api/volunteer/tasks/{taskId}/claim
        [HttpPost("tasks/{taskId}/claim")]
        public async Task<IActionResult> ClaimTask(int taskId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            var result = await _volunteerService.ClaimTaskAsync(taskId, userId);
            if (!result.Success)
            {
                if (result.Message == "Task not found.") return NotFound(new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }

        // POST: api/volunteer/tasks/{taskId}/complete
        [HttpPost("tasks/{taskId}/complete")]
        public async Task<IActionResult> CompleteTask(int taskId, [FromForm] CompleteTaskDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            var result = await _volunteerService.CompleteTaskAsync(taskId, userId, dto);
            if (!result.Success)
            {
                if (result.Message == "Task not found.") return NotFound(new { message = result.Message });
                if (result.Message == "You are not the owner of this task.") return StatusCode(403, new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }

        // POST: api/volunteer/tasks/{taskId}/abandon
        [HttpPost("tasks/{taskId}/abandon")]
        public async Task<IActionResult> AbandonTask(int taskId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            var result = await _volunteerService.AbandonTaskAsync(taskId, userId);
            if (!result.Success)
            {
                if (result.Message == "Task not found.") return NotFound(new { message = result.Message });
                if (result.Message == "You are not the owner of this task.") return StatusCode(403, new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }
    }
}
