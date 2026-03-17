using Microsoft.EntityFrameworkCore;
using StrayCareAPI.Data;
using StrayCareAPI.DTOs;
using StrayCareAPI.Models;

namespace StrayCareAPI.Services
{
    public class WalletService : IWalletService
    {
        private readonly ApplicationDbContext _context;

        public WalletService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message, decimal NewBalance)> TopupAsync(int userId, TopupDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return (false, "User not found", 0);
            }

            // In a real application, you would verify the payment here.
            // For now, we assume the payment is successful.

            user.Balance += dto.Amount;
            
            // Log the transaction
            var transaction = new WalletTransaction
            {
                UserId = userId,
                Amount = dto.Amount,
                TransactionType = "Topup",
                Timestamp = DateTime.UtcNow,
                Description = $"Top-up via {dto.PaymentMethod}"
            };
            _context.WalletTransactions.Add(transaction);
            
            await _context.SaveChangesAsync();

            return (true, "充值成功", user.Balance);
        }

        public async Task<List<object>> GetMyWalletRecordsAsync(int userId)
        {
            return await _context.WalletTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Timestamp)
                .Select(t => new
                {
                    t.Id,
                    t.Amount,
                    t.TransactionType,
                    t.Timestamp,
                    t.Description
                })
                .ToListAsync<object>();
        }

        public async Task<decimal> GetBalanceAsync(int userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
            
            return user?.Balance ?? 0;
        }
    }
}
