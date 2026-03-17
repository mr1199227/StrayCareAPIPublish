using Microsoft.AspNetCore.Mvc;
using StrayCareAPI.Services;

namespace StrayCareAPI.Controllers
{
    [Route("api/shelters")]
    [ApiController]
    public class PublicShelterController : ControllerBase
    {
        private readonly IShelterService _shelterService;

        public PublicShelterController(IShelterService shelterService)
        {
            _shelterService = shelterService;
        }

        // GET: api/shelters
        [HttpGet]
        public async Task<IActionResult> GetAllShelters()
        {
            var shelters = await _shelterService.GetAllSheltersAsync();
            return Ok(shelters);
        }

        // GET: api/shelters/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetShelterById(int id)
        {
            var shelter = await _shelterService.GetShelterByIdAsync(id);
            if (shelter == null)
            {
                return NotFound(new { message = "Shelter not found" });
            }
            return Ok(shelter);
        }

        // GET: api/shelters/getNearbyShelters
        [HttpGet("getNearbyShelters")]
        public async Task<IActionResult> GetNearbyShelters([FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] double radius = 15.0, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var nearbyShelters = await _shelterService.GetNearbySheltersAsync(latitude, longitude, radius, pageNumber, pageSize);
            return Ok(nearbyShelters);
        }
    }
}
