using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StrayCareAPI.DTOs;
using StrayCareAPI.Services;

namespace StrayCareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        /// <summary>
        /// 用户充值余额
        /// </summary>
        [HttpPost("topup")]
        [Authorize(Roles = "User,Volunteer,Shelter")] // Allow all roles to top up? Or just specific ones? Assuming all authenticated users.
        public async Task<IActionResult> Topup([FromBody] TopupDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            if (!int.TryParse(userIdClaim.Value, out int userId))
            {
                return BadRequest("无效的用户ID");
            }

            var result = await _walletService.TopupAsync(userId, dto);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new
            {
                message = result.Message,
                newBalance = result.NewBalance
            });
        }

        /// <summary>
        /// 获取当前用户余额 (Get Current User Balance)
        /// </summary>
        [HttpGet("balance")]
        [Authorize]
        public async Task<IActionResult> GetBalance()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            var balance = await _walletService.GetBalanceAsync(userId);
            
            return Ok(new
            {
                userId = userId,
                balance = balance
            });
        }

        /// <summary>
        /// 获取我的钱包交易记录 (Get My Wallet History)
        /// Includes Topups and Donations
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetMyWalletHistory()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            var records = await _walletService.GetMyWalletRecordsAsync(userId);
            return Ok(records);
        }
    }
}
