using Microsoft.AspNetCore.Mvc;
using StrayCareAPI.DTOs;
using StrayCareAPI.Services;
using Microsoft.AspNetCore.Authorization;

namespace StrayCareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// 用户注册（使用 Email）
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }
            return Ok(new { message = result.Message, data = result.Data }); // Adjusting to return structure
        }

        /// <summary>
        /// 申请成为志愿者（现有用户）
        /// </summary>
        [Authorize(Roles = "User")]
        [HttpPost("apply-volunteer")]
        public async Task<IActionResult> ApplyVolunteer([FromBody] ApplyVolunteerDto dto)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }
            dto.UserId = userId;

            var result = await _authService.ApplyVolunteerAsync(dto);
            if (!result.Success)
            {
                 return BadRequest(new { message = result.Message });
            }
            return Ok(new { message = result.Message });
        }

        /// <summary>
        /// 注册收容所（Shelter）账户
        /// 需要上传营业执照，注册后需等待管理员审核
        /// </summary>
        [HttpPost("register-shelter")]
        public async Task<IActionResult> RegisterShelter([FromForm] RegisterShelterDto dto)
        {
            var result = await _authService.RegisterShelterAsync(dto);
            if (!result.Success)
            {
                if (result.Message == "File size cannot exceed 10MB." || result.Message.Contains("Only JPG"))
                {
                     return BadRequest(new { message = result.Message });
                }
                if (result.Message == "An error occurred during registration.")
                {
                     return StatusCode(500, new { message = result.Message });
                }
                return BadRequest(new { message = result.Message });
            }
            return Ok(new { message = result.Message });
        }

        /// <summary>
        /// 用户登录（使用 Email）
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            if (!result.Success)
            {
                if (result.Message == "Account is pending approval.") return StatusCode(403, new { message = result.Message });
                return Unauthorized(new { message = result.Message });
            }
            return Ok(result.Data); // result.Data contains the anonymous object with token
        }

        /// <summary>
        /// 忘记密码 - 生成重置令牌
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var result = await _authService.ForgotPasswordAsync(dto);
            // Service returns Success=true even if user not found for security, so direct return ok
            return Ok(new
            {
                message = result.Message,
                resetToken = result.Data, // Provide token for testing
                email = dto.Email
            });
        }

        /// <summary>
        /// 重置密码
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var result = await _authService.ResetPasswordAsync(dto);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }
            return Ok(new { message = result.Message });
        }

        /// <summary>
        /// 编辑用户个人资料 (Edit Profile)
        /// 支持修改姓名、电话、地址、头像
        /// </summary>
        [HttpPost("edit-profile")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> EditProfile([FromForm] EditUserProfileDto dto)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            var result = await _authService.EditProfileAsync(userId, dto);
            if (!result.Success)
            {
                if (result.Message == "User not found.") return NotFound(new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }
            return Ok(new { message = result.Message, data = result.Data });
        }

        /// <summary>
        /// 获取当前用户完整资料 (Get Current User Profile)
        /// 包含角色专属数据 (Volunteer/Shelter Profile)
        /// </summary>
        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetCurrentUserProfile()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            var profile = await _authService.GetCurrentUserProfileAsync(userId);
            if (profile == null)
            {
                return NotFound(new { message = "User not found." });
            }

            return Ok(profile);
        }
        /// <summary>
        /// 发送邮箱验证码 (Send OTP)
        /// </summary>
        [HttpPost("send-verification")]
        public async Task<IActionResult> SendVerification([FromBody] SendVerificationDto dto)
        {
            var result = await _authService.SendEmailVerificationAsync(dto.Email, dto.Type);
            if (!result.Success)
            {
                if (result.Message == "User not found.") return NotFound(new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }
            return Ok(new { message = result.Message });
        }

        /// <summary>
        /// 验证邮箱 (Verify OTP)
        /// </summary>
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
        {
            var result = await _authService.VerifyEmailAsync(dto.Email, dto.Code);
            if (!result.Success)
            {
                if (result.Message == "User not found.") return NotFound(new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }
            return Ok(new { message = result.Message });
        }
    }
}
