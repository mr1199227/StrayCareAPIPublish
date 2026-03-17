using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StrayCareAPI.DTOs;
using StrayCareAPI.Services;

namespace StrayCareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // GET: api/admin/pending-shelters
        [HttpGet("pending-shelters")]
        public async Task<IActionResult> GetPendingShelters()
        {
            var result = await _adminService.GetPendingSheltersAsync();
            return Ok(result);
        }

        // GET: api/admin/pending-volunteers
        [HttpGet("pending-volunteers")]
        public async Task<IActionResult> GetPendingVolunteers()
        {
            var result = await _adminService.GetPendingVolunteersAsync();
            return Ok(result);
        }

        // GET: api/admin/medical-tasks/pending
        [HttpGet("medical-tasks/pending")]
        public async Task<IActionResult> GetPendingMedicalTasks()
        {
            var tasks = await _adminService.GetPendingMedicalTasksAsync();
            return Ok(tasks);
        }

        // POST: api/admin/medical-tasks/{taskId}/approve
        [HttpPost("medical-tasks/{taskId}/approve")]
        public async Task<IActionResult> ApproveMedicalTask(int taskId)
        {
            var result = await _adminService.ApproveMedicalTaskAsync(taskId);
            if (!result) return NotFound(new { message = "The task does not exist." });
            return Ok(new { message = "The task has been approved." });
        }

        // POST: api/admin/approve-shelter/{userId}
        [HttpPost("approve-shelter/{userId}")]
        public async Task<IActionResult> ApproveShelter(int userId)
        {
            var result = await _adminService.ApproveShelterAsync(userId);
            if (!result.Success)
            {
                if (result.Message == "User not found.") return NotFound(new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }
            return Ok(new { message = result.Message });
        }

        // POST: api/admin/reject-shelter/{userId}
        [HttpPost("reject-shelter/{userId}")]
        public async Task<IActionResult> RejectShelter(int userId)
        {
            var result = await _adminService.RejectShelterAsync(userId);
            if (!result.Success)
            {
                if (result.Message == "User not found.") return NotFound(new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }
            return Ok(new { message = result.Message });
        }

        // POST: api/admin/approve-volunteer/{userId}
        [HttpPost("approve-volunteer/{userId}")]
        public async Task<IActionResult> ApproveVolunteer(int userId)
        {
            var result = await _adminService.ApproveVolunteerAsync(userId);
            if (!result.Success)
            {
                if (result.Message == "User not found.") return NotFound(new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }
            return Ok(new { message = result.Message });
        }

        // POST: api/admin/reject-volunteer/{userId}
        [HttpPost("reject-volunteer/{userId}")]
        public async Task<IActionResult> RejectVolunteer(int userId)
        {
            var result = await _adminService.RejectVolunteerAsync(userId);
            if (!result.Success)
            {
                if (result.Message == "User not found.") return NotFound(new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }
            return Ok(new { message = result.Message });
        }

        // ==========================
        // User Management (Block/Unblock)
        // ==========================

        /// <summary>
        /// 获取所有用户列表 (Get All Users)
        /// Excludes Admins
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _adminService.GetAllUsersAsync();
            return Ok(users);
        }

        /// <summary>
        /// 获取用户详情 (Get User Details)
        /// Includes Volunteer/Shelter profile data
        /// </summary>
        [HttpGet("users/{userId}/details")]
        public async Task<IActionResult> GetUserDetails(int userId)
        {
            var userDetail = await _adminService.GetUserDetailAsync(userId);
            if (userDetail == null) return NotFound(new { message = "User not found." });
            return Ok(userDetail);
        }

        /// <summary>
        /// 封禁用户 (Block User)
        /// User cannot login after blocking
        /// </summary>
        [HttpPost("users/{userId}/block")]
        public async Task<IActionResult> BlockUser(int userId)
        {
            var result = await _adminService.UpdateUserStatusAsync(userId, false); // isActive = false
            if (!result.Success)
            {
                if (result.Message == "User not found.") return NotFound(new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }
            return Ok(new { message = result.Message });
        }

        /// <summary>
        /// 解封用户 (Unblock User)
        /// User can login again
        /// </summary>
        [HttpPost("users/{userId}/unblock")]
        public async Task<IActionResult> UnblockUser(int userId)
        {
            var result = await _adminService.UpdateUserStatusAsync(userId, true); // isActive = true
            if (!result.Success)
            {
                if (result.Message == "User not found.") return NotFound(new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }
            return Ok(new { message = result.Message });
        }

        // ==========================
        // Wallet Records (Top-up)
        // ==========================

        /// <summary>
        /// 获取所有充值记录 (Get All Top-up Records)
        /// </summary>
        [HttpGet("topup-records")]
        public async Task<IActionResult> GetTopUpRecords()
        {
            var records = await _adminService.GetAllTopUpRecordsAsync();
            return Ok(records);
        }

         /// <summary>
        /// 获取所有充值记录 (Get All Donation Records)
        /// </summary>
        [HttpGet("donation-records")]
        public async Task<IActionResult> GetDonationRecords()
        {
            var records = await _adminService.GetAllDonationRecordsAsync();
            return Ok(records);
        }

        // GET: api/admin/stray-applications
        [HttpGet("stray-applications")]
        public async Task<IActionResult> GetStrayApplications()
        {
            var result = await _adminService.GetStrayApplicationsAsync();
            return Ok(result);
        }

        // POST: api/admin/applications/{appId}/approve
        [HttpPost("applications/{appId}/approve")]
        public async Task<IActionResult> ApproveApplication(int appId)
        {
            var result = await _adminService.ApproveApplicationAsync(appId);
            if (!result.Success)
            {
                if (result.Message == "Application not found.") return NotFound(new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }
            return Ok(new { message = result.Message });
        }

        // POST: api/admin/applications/{appId}/reject
        [HttpPost("applications/{appId}/reject")]
        public async Task<IActionResult> RejectApplication(int appId, [FromBody] RejectApplicationDto dto)
        {
            var result = await _adminService.RejectApplicationAsync(appId, dto);
            if (!result.Success)
            {
                if (result.Message == "Application not found.") return NotFound(new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }
            return Ok(new { message = result.Message });
        }
        // GET: api/admin/verification-logs
        [HttpGet("verification-logs")]
        public async Task<IActionResult> GetVerificationLogs()
        {
            var logs = await _adminService.GetAllVerificationLogsAsync();
            return Ok(logs);
        }

        // ==========================
        // Animal Management (Read-only)
        // ==========================

        /// <summary>
        /// 获取所有动物列表 (Get All Animals - Admin, unfiltered)
        /// </summary>
        [HttpGet("animals")]
        public async Task<IActionResult> GetAllAnimals()
        {
            var result = await _adminService.GetAllAnimalsAdminAsync();
            return Ok(result);
        }

        /// <summary>
        /// 获取单只动物详情 (Get Animal Detail - Admin)
        /// </summary>
        [HttpGet("animals/{id}")]
        public async Task<IActionResult> GetAnimalById(int id)
        {
            var result = await _adminService.GetAnimalByIdAdminAsync(id);
            if (result == null) return NotFound(new { message = "Animal not found." });
            return Ok(result);
        }

        /// <summary>
        /// 获取动物活动日志 (Get Animal Activities - Admin)
        /// </summary>
        [HttpGet("animals/{id}/activities")]
        public async Task<IActionResult> GetAnimalActivities(int id)
        {
            var result = await _adminService.GetAnimalActivitiesAdminAsync(id);
            return Ok(result);
        }
    }
}
