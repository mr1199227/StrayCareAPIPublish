using Microsoft.AspNetCore.Mvc;
using StrayCareAPI.DTOs;
using StrayCareAPI.Services;

namespace StrayCareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SightingsController : ControllerBase
    {
        private readonly ISightingService _sightingService;

        public SightingsController(ISightingService sightingService)
        {
            _sightingService = sightingService;
        }

        /// <summary>
        /// 智能目击报告端点
        /// 场景 A: AnimalId 不为空 -> 更新现有动物位置
        /// 场景 B: AnimalId 为空 -> 创建新动物 + 目击记录
        /// </summary>
        [HttpPost("createSighting")]
        public async Task<IActionResult> CreateSighting([FromForm] CreateSightingDto dto)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }
            dto.ReporterUserId = userId;

            var result = await _sightingService.CreateSightingAsync(dto);
            
            if (!result.Success)
            {
                // Simple error handling usually BadRequest for validation/logic errors
                // "动物不存在" -> NotFound ideally, but keeping simple
                return BadRequest(new { message = result.Message });
            }

            return Ok(result.Data);
        }

        /// <summary>
        /// 获取特定动物的所有目击记录
        /// </summary>
        [HttpGet("getAnimalSightings/{animalId}")]
        public async Task<IActionResult> GetAnimalSightings(int animalId)
        {
            var sightings = await _sightingService.GetAnimalSightingsAsync(animalId);
            return Ok(sightings);
        }
    }
}
