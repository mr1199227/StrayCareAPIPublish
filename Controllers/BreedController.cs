using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrayCareAPI.Data;
using StrayCareAPI.Models;

namespace StrayCareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BreedController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BreedController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 获取品种列表，可选按物种筛选
        /// GET /api/breed?species=Dog
        /// GET /api/breed?species=Cat
        /// GET /api/breed (返回所有)
        /// </summary>
        [HttpGet("getBreeds")]
        public async Task<IActionResult> GetBreeds([FromQuery] string? species = null)
        {
            IQueryable<Breed> query = _context.Breeds;

            // 如果指定了物种，则筛选
            if (!string.IsNullOrEmpty(species))
            {
                query = query.Where(b => b.Species == species);
            }

            var breeds = await query
                .OrderBy(b => b.Name)
                .ToListAsync();

            return Ok(breeds);
        }

        /// <summary>
        /// 根据 ID 获取单个品种
        /// </summary>
        [HttpGet("getBreed/{id}")]
        public async Task<IActionResult> GetBreed(int id)
        {
            var breed = await _context.Breeds.FindAsync(id);

            if (breed == null)
                return NotFound(new { message = "品种不存在" });

            return Ok(breed);
        }
    }
}
