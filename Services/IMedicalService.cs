using StrayCareAPI.DTOs;
using StrayCareAPI.Models;

namespace StrayCareAPI.Services
{
    public interface IMedicalService
    {
        Task<bool> SetGoalAsync(SetGoalDto dto); // Deprecated
        Task<Donation> DonateAsync(DonateDto dto, int userId);
        Task<MedicalTask> ClaimTaskAsync(ClaimTaskDto dto);

        /// <summary>
        /// 志愿者完成任务并上传证明
        /// </summary>
        Task<bool> CompleteTaskAsync(CompleteTaskDto dto);

        // New Methods
        Task<HealthUpdateProof> SubmitHealthProofAsync(SubmitHealthProofDto dto, string imageUrl);
        Task<bool> ApproveHealthProofAsync(int proofId, ApproveProofDto dto);
        Task<DonationCampaign> CreateEmergencyCampaignAsync(CreateEmergencyCampaignDto dto);

        /// <summary>
        /// 获取所有任务 (Admin) - 支持状态过滤
        /// </summary>
        Task<List<object>> GetAllTasksAsync(string? status = null);

        /// <summary>
        /// 获取公开的已完成任务 (Approved Only)
        /// </summary>
        Task<List<object>> GetPublicTasksAsync();
    }
}
