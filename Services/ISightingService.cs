using StrayCareAPI.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace StrayCareAPI.Services
{
    public interface ISightingService
    {
        Task<(bool Success, string Message, object? Data)> CreateSightingAsync(CreateSightingDto dto);
        Task<List<object>> GetAnimalSightingsAsync(int animalId);
    }
}
