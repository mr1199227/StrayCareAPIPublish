using Microsoft.AspNetCore.Mvc;
using StrayCareAPI.DTOs;
using StrayCareAPI.Models;
using Microsoft.AspNetCore.Authorization;
using StrayCareAPI.Services;

namespace StrayCareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnimalController : ControllerBase
    {
        private readonly IAnimalService _animalService;

        public AnimalController(IAnimalService animalService)
        {
            _animalService = animalService;
        }

        /// <summary>
        /// 获取所有动物列表
        /// </summary>
        [HttpGet("getall")]
        public async Task<IActionResult> GetAnimals()
        {
            var animals = await _animalService.GetAllAnimalsAsync();
            return Ok(animals);
        }

        /// <summary>
        /// 根据 ID 获取单个动物
        /// </summary>
        [HttpGet("getbyid/{id}")]
        public async Task<IActionResult> GetAnimal(int id)
        {
            var animal = await _animalService.GetAnimalByIdAsync(id);
            
            if (animal == null)
                return NotFound(new { message = "Animals do not exist." });

            return Ok(animal);
        }

        /// <summary>
        /// 创建新动物
        /// </summary>
        [HttpPost("createAnimal")]
        public async Task<IActionResult> CreateAnimal([FromForm] CreateAnimalDto dto)
        {
            try 
            {
                var animal = await _animalService.CreateAnimalAsync(dto);
                return CreatedAtAction(nameof(GetAnimal), new { id = animal.Id }, animal);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// 编辑动物资料 (Edit Animal Profile)
        /// Admin or Owner Shelter only
        /// </summary>
        [HttpPut("{id}")]
        [Authorize] 
        public async Task<IActionResult> EditAnimal(int id, [FromForm] EditAnimalDto dto)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";

            var result = await _animalService.EditAnimalAsync(id, dto, userId, userRole);

            if (!result.Success)
            {
                if (result.Message == "Animal not found") return NotFound(new { message = result.Message });
                if (result.Message.StartsWith("Forbidden")) return Forbid();
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = "Animal updated successfully", animalId = result.AnimalId });
        }

        /// <summary>
        /// Get a unified history of Sightings and Feedings for an animal
        /// </summary>
        [HttpGet("getActivityHistory/{id}")]
        public async Task<IActionResult> GetActivityHistory(int id)
        {
            if (await _animalService.GetAnimalByIdAsync(id) == null)
                 return NotFound(new { message = "Animal not found" });

            var history = await _animalService.GetActivityHistoryAsync(id);
            return Ok(history);
        }

        /// <summary>
        /// 获取特定动物的所有目击记录
        /// </summary>
        [HttpGet("getAnimalSightings/{animalId}")]
        public async Task<IActionResult> GetAnimalSightings(int animalId)
        {
             var sightings = await _animalService.GetAnimalSightingsAsync(animalId);
             return Ok(sightings);
        }

        /// <summary>
        /// Get animals nearby a specific location
        /// </summary>
        [HttpGet("getNearbyAnimals")]
        public async Task<IActionResult> GetNearbyAnimals([FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] double radius = 5.0, [FromQuery] int? pageNumber = null, [FromQuery] int? pageSize = null)
        {
            var nearbyAnimals = await _animalService.GetNearbyAnimalsAsync(latitude, longitude, radius, pageNumber, pageSize);
            return Ok(nearbyAnimals);
        }


        /// <summary>
        /// Deletes an animal record (Hard Delete).
        /// Rejects if donations exist.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Shelter")]
        public async Task<IActionResult> DeleteAnimal(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";

            var result = await _animalService.DeleteAnimalAsync(id, userId, userRole);

            if (!result.Success)
            {
                if (result.Message == "Animal not found") return NotFound(new { message = result.Message });
                if (result.Message.StartsWith("Forbidden")) return Forbid();
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }

        /// <summary>
        /// Get animals adopted by current user
        /// </summary>
        [HttpGet("my-animals")]
        [Authorize]
        public async Task<IActionResult> GetMyAnimals()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                 return Unauthorized(new { message = "Invalid user token." });
            }

            var animals = await _animalService.GetMyAnimalsAsync(userId);
            return Ok(animals);
        }

        /// <summary>
        /// 获取特定收容所的动物
        /// </summary>
        [HttpGet("getShelterAnimals/{shelterId}")]
        public async Task<IActionResult> GetShelterAnimals(int shelterId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var animals = await _animalService.GetAnimalsByShelterIdAsync(shelterId, pageNumber, pageSize);
            return Ok(animals);
        }
    }
}