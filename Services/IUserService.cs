using StrayCareAPI.DTOs;

namespace StrayCareAPI.Services
{
    public interface IUserService
    {
        Task<UserDashboardStatsDto> GetDashboardStatsAsync(int userId);
        Task<List<object>> GetMyAnimalsAsync(int userId);
        Task<List<object>> GetMyAdoptionsAsync(int userId);
        Task<List<object>> GetMyFeedingsAsync(int userId);
        Task<UserFinancialsDto> GetMyFinancialsAsync(int userId);
        Task<List<UserRecentActivityDto>> GetRecentActivitiesAsync(int userId, int perTypeLimit = 10, int finalLimit = 10);
    }
}
