using StrayCareAPI.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace StrayCareAPI.Services
{
    public interface IShelterService
    {
        Task<(bool Success, string Message, object? Data)> IntakeAnimalAsync(int animalId, int userId, ShelterIntakeDto dto);
        Task<List<object>> GetApplicationsAsync(int userId);
        Task<(bool Success, string Message)> ApproveApplicationAsync(int appId, int userId);
        Task<(bool Success, string Message)> RejectApplicationAsync(int appId, int userId, RejectApplicationDto dto);
        Task<List<object>> ViewOwnAnimalsAsync(int userId);
        Task<(bool Success, string Message, object? Data)> GetNearbyStraysAsync(int userId, double radiusInKm);
        
        // Public Access
        Task<List<ShelterDto>> GetAllSheltersAsync();
        Task<ShelterDetailDto?> GetShelterByIdAsync(int id);
        Task<List<NearbyShelterDto>> GetNearbySheltersAsync(double latitude, double longitude, double radius = 15.0, int pageNumber = 1, int pageSize = 10);
    }
}
