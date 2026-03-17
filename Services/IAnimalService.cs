using Microsoft.AspNetCore.Mvc;
using StrayCareAPI.DTOs;
using StrayCareAPI.Models;

namespace StrayCareAPI.Services
{
    public interface IAnimalService
    {
        Task<List<Animal>> GetAllAnimalsAsync();
        Task<Animal?> GetAnimalByIdAsync(int id);
        Task<Animal> CreateAnimalAsync(CreateAnimalDto dto);
        Task<(bool Success, string Message, int? AnimalId)> EditAnimalAsync(int id, EditAnimalDto dto, int userId, string userRole);
        Task<List<ActivityHistoryDto>> GetActivityHistoryAsync(int id);
        Task<List<object>> GetAnimalSightingsAsync(int animalId);
        Task<List<AnimalDto>> GetNearbyAnimalsAsync(double latitude, double longitude, double radius, int? pageNumber = null, int? pageSize = null);
        Task<(bool Success, string Message)> DeleteAnimalAsync(int id, int userId, string userRole);
        Task<List<object>> GetMyAnimalsAsync(int userId);
        Task<List<AnimalDto>> GetAnimalsByShelterIdAsync(int shelterId, int pageNumber = 1, int pageSize = 10);
    }
}
