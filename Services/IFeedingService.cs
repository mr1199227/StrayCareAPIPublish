using StrayCareAPI.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace StrayCareAPI.Services
{
    public interface IFeedingService
    {
        Task<(bool Success, string Message, object? Data)> CreateFeedingLogAsync(CreateFeedingLogDto dto);
    }
}
