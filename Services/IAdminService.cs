using StrayCareAPI.DTOs;
using StrayCareAPI.Models;

namespace StrayCareAPI.Services
{
    public interface IAdminService
    {
        Task<List<object>> GetPendingSheltersAsync();
        Task<(bool Success, string Message)> ApproveShelterAsync(int userId);
        Task<(bool Success, string Message)> RejectShelterAsync(int userId);
        Task<List<object>> GetStrayApplicationsAsync();
        Task<(bool Success, string Message)> ApproveApplicationAsync(int appId);
        Task<(bool Success, string Message)> RejectApplicationAsync(int appId, RejectApplicationDto dto);
        Task<List<object>> GetPendingVolunteersAsync();
        Task<(bool Success, string Message)> ApproveVolunteerAsync(int userId);
        Task<(bool Success, string Message)> RejectVolunteerAsync(int userId);
        Task<List<object>> GetPendingMedicalTasksAsync();
        Task<bool> ApproveMedicalTaskAsync(int taskId);
        Task<List<object>> GetAllUsersAsync();
        Task<(bool Success, string Message)> UpdateUserStatusAsync(int userId, bool isActive);
        Task<List<object>> GetAllTopUpRecordsAsync();
        Task<List<object>> GetAllDonationRecordsAsync();
        Task<object?> GetUserDetailAsync(int userId);
        Task<List<object>> GetAllVerificationLogsAsync();

        // Animal Management (Admin read-only)
        Task<IEnumerable<object>> GetAllAnimalsAdminAsync();
        Task<object?> GetAnimalByIdAdminAsync(int animalId);
        Task<IEnumerable<ActivityHistoryDto>> GetAnimalActivitiesAdminAsync(int animalId);
    }
}
