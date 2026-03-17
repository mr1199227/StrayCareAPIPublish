using StrayCareAPI.DTOs;

namespace StrayCareAPI.Services
{
    public interface IAdoptionService
    {
        Task<(bool Success, string Message)> SubmitApplicationAsync(int userId, AdoptionApplicationDto dto);
        Task<List<object>> GetMyApplicationsAsync(int userId);
    }
}
