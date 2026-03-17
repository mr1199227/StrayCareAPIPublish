using StrayCareAPI.DTOs;

namespace StrayCareAPI.Services
{
    public interface IWalletService
    {
        Task<(bool Success, string Message, decimal NewBalance)> TopupAsync(int userId, TopupDto dto);
        Task<List<object>> GetMyWalletRecordsAsync(int userId);
        Task<decimal> GetBalanceAsync(int userId);
    }
}
