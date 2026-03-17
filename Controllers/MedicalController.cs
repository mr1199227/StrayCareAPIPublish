using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StrayCareAPI.DTOs;
using StrayCareAPI.Services;

namespace StrayCareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MedicalController : ControllerBase
    {
        private readonly IMedicalService _medicalService;
        private readonly ICloudinaryService _cloudinaryService;

        public MedicalController(IMedicalService medicalService, ICloudinaryService cloudinaryService)
        {
            _medicalService = medicalService;
            _cloudinaryService = cloudinaryService;
        }

        /// <summary>
        /// [已废弃] 管理员设置众筹目标
        /// 系统自动创建活动，此接口不再使用
        /// </summary>
        [HttpPost("set-goal")]
        public IActionResult SetGoal([FromBody] SetGoalDto dto)
        {
            return BadRequest(new { message = "此接口已废弃。系统会自动为动物创建众筹活动。" });
        }

        /// <summary>
        /// 用户捐赠 (基于 CampaignId)
        /// </summary>
        [HttpPost("donate")]
        public async Task<IActionResult> Donate([FromBody] DonateDto dto)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }
            
            // Validate CampaignId presence (DTO validation handles it, but explicit check good)
            if (dto.CampaignId <= 0) return BadRequest(new { message = "CampaignId is required." });

            try
            {
                var donation = await _medicalService.DonateAsync(dto, userId);
                return Ok(new
                {
                    message = "Donation successful !",
                    donationId = donation.Id,
                    amount = donation.Amount
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 志愿者认领任务
        /// </summary>
        [HttpPost("claim-task")]
        public async Task<IActionResult> ClaimTask([FromBody] ClaimTaskDto dto)
        {
            try
            {
                var task = await _medicalService.ClaimTaskAsync(dto);
                return Ok(new
                {
                    message = "Task claim successful !",
                    taskId = task.Id
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 志愿者完成任务
        /// </summary>
        [HttpPost("complete-task")]
        public async Task<IActionResult> CompleteTask([FromBody] CompleteTaskDto dto)
        {
            var result = await _medicalService.CompleteTaskAsync(dto);
            
            if (!result)
                return NotFound(new { message = "The task does not exist." });

            return Ok(new { message = "The task has been submitted and is awaiting administrator approval." });
        }

        /// <summary>
        /// 获取所有任务历史 (Admin)
        /// Status 可选: Open, InProgress, PendingApproval, Approved, Rejected
        /// </summary>
        [HttpGet("all-tasks")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllTasks([FromQuery] string? status)
        {
            var tasks = await _medicalService.GetAllTasksAsync(status);
            return Ok(tasks);
        }

        /// <summary>
        /// 获取公开的医疗记录 (Public)
        /// 仅展示已完成(Approved)的任务
        /// </summary>
        [HttpGet("public-history")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicHistory()
        {
            var tasks = await _medicalService.GetPublicTasksAsync();
            return Ok(tasks);
        }

        // --- NEW ENDPOINTS (Multi-Campaign & Proofs) ---

        /// <summary>
        /// 提交健康证明 (志愿者/用户)
        /// </summary>
        [HttpPost("health-proof")]
        public async Task<IActionResult> SubmitHealthProof([FromForm] SubmitHealthProofDto dto)
        {
             try
            {
                string imageUrl = "";
                if (dto.ProofImage != null && dto.ProofImage.Length > 0)
                {
                     var uploadResult = await _cloudinaryService.UploadImageAsync(dto.ProofImage, "straycare/proofs");
                     if (!uploadResult.Success)
                     {
                         return BadRequest(new { message = uploadResult.Message });
                     }
                     imageUrl = uploadResult.SecureUrl!;
                }

                var proof = await _medicalService.SubmitHealthProofAsync(dto, imageUrl);
                return Ok(new { message = "Proof submitted successfully!", proofId = proof.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 审批健康证明 (Admin)
        /// 批准后将自动：更新动物状态 -> 关闭对应众筹 -> 转移余额至 Food Campaign
        /// </summary>
        [HttpPost("approve-proof/{proofId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveHealthProof(int proofId, [FromBody] ApproveProofDto dto)
        {
             var result = await _medicalService.ApproveHealthProofAsync(proofId, dto);
             if (!result) return NotFound(new { message = "Proof not found" });

             return Ok(new { message = dto.IsApproved ? "Proof approved, funds transferred!" : "Proof rejected." });
        }

        /// <summary>
        /// 创建紧急众筹 (Admin)
        /// </summary>
        [HttpPost("emergency-campaign")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateEmergencyCampaign([FromBody] CreateEmergencyCampaignDto dto)
        {
            try
            {
                var campaign = await _medicalService.CreateEmergencyCampaignAsync(dto);
                return Ok(new { message = "Emergency campaign created successfully!", campaignId = campaign.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
