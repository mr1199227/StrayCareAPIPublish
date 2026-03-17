using StrayCareAPI.DTOs;
using StrayCareAPI.Models;

namespace StrayCareAPI.Services
{
    public interface IVolunteerService
    {
        Task<List<object>> GetMyTasksAsync(int userId);
        
        /// <summary>
        /// 获取所有可领取的任务 (Open 状态)
        /// </summary>
        Task<List<object>> GetAvailableTasksAsync();

        Task<(bool Success, string Message)> ClaimTaskAsync(int taskId, int userId);
        Task<(bool Success, string Message)> CompleteTaskAsync(int taskId, int userId, CompleteTaskDto dto);
        Task<(bool Success, string Message)> AbandonTaskAsync(int taskId, int userId);
    }
}
