using Microsoft.EntityFrameworkCore;
using StrayCareAPI.Data;
using StrayCareAPI.DTOs;
using StrayCareAPI.Models;

namespace StrayCareAPI.Services
{
    public class VolunteerService : IVolunteerService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;

        public VolunteerService(ApplicationDbContext context, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<List<object>> GetMyTasksAsync(int userId)
        {
            var tasks = await _context.MedicalTasks
                .Include(t => t.Animal).ThenInclude(a => a.Campaigns)
                .Include(t => t.Animal).ThenInclude(a => a.Images)
                .Where(t => t.VolunteerId == userId)
                .ToListAsync();

            var result = tasks.Select(t =>
            {
                var campaign = t.Animal.Campaigns.FirstOrDefault(c => c.Type == t.TaskType);
                var image = t.Animal.Images.FirstOrDefault(i => i.IsPrimary) ?? t.Animal.Images.FirstOrDefault();

                return new
                {
                    t.Id,
                    t.TaskType,
                    t.Status,
                    t.ExpenseAmount,
                    t.ProofImage,
                    t.ReceiptImageUrl,
                    AnimalName = t.Animal.Name,
                    AnimalType = t.Animal.Species,
                    t.AnimalId,
                    // New Fields
                    AnimalLocation = t.Animal.LocationName,
                    GoalAmount = campaign?.TargetAmount ?? 0,
                    CurrentAmount = campaign?.CurrentAmount ?? 0,
                    Latitude = t.Animal.Latitude,
                    Longitude = t.Animal.Longitude,
                    AnimalImageUrl = image?.ImageUrl,
                    Priority = "Normal",
                };
            }).ToList<object>();

            return result;
        }

        public async Task<List<object>> GetAvailableTasksAsync()
        {
            var tasks = await _context.MedicalTasks
                .Include(t => t.Animal)
                .ThenInclude(a => a.Campaigns)
                .Include(t => t.Animal)
                .ThenInclude(a => a.Images) // Fixed: was AnimalImages
                .Where(t => (t.Status == "Open" || t.Status == "ReadyForVolunteer") && t.Animal.Status != "Adopted")
                .ToListAsync();
            
            var result = tasks.Select(t => {
                var campaign = t.Animal.Campaigns.FirstOrDefault(c => c.Type == t.TaskType);
                var image = t.Animal.Images.FirstOrDefault(i => i.IsPrimary) ?? t.Animal.Images.FirstOrDefault(); // Fixed

                return new
                {
                    t.Id,
                    t.TaskType,
                    t.Status,
                    t.ExpenseAmount,
                    AnimalName = t.Animal.Name,
                    AnimalType = t.Animal.Species,
                    AnimalLocation = t.Animal.LocationName,
                    t.AnimalId,
                    GoalAmount = campaign?.TargetAmount ?? 0,
                    CurrentAmount = campaign?.CurrentAmount ?? 0,
                    Latitude = t.Animal.Latitude,
                    Longitude = t.Animal.Longitude,
                    AnimalImageUrl = image?.ImageUrl,
                    Priority = "Normal",
                    CreatedAt = DateTime.UtcNow
                };
            }).ToList<object>();

            return result;
        }

        public async Task<(bool Success, string Message)> ClaimTaskAsync(int taskId, int userId)
        {
            var task = await _context.MedicalTasks
                .Include(t => t.Animal)
                .FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
            {
                return (false, "Task not found.");
            }

            if (task.Status != "Open" && task.Status != "ReadyForVolunteer")
            {
                return (false, "Task is not available for claiming.");
            }

            if (task.Animal != null && task.Animal.Status == "Adopted")
            {
                return (false, "This animal has already been adopted. Task closed.");
            }

            task.VolunteerId = userId;
            task.Status = "InProgress";
            await _context.SaveChangesAsync();

            return (true, "Task claimed successfully.");
        }

        public async Task<(bool Success, string Message)> CompleteTaskAsync(int taskId, int userId, CompleteTaskDto dto)
        {
            var task = await _context.MedicalTasks.FindAsync(taskId);
            if (task == null)
            {
                return (false, "Task not found.");
            }

            if (task.VolunteerId != userId)
            {
                return (false, "You are not the owner of this task.");
            }

            // Handle Proof Image
            if (dto.ProofImage != null && dto.ProofImage.Length > 0)
            {
                var uploadResult = await _cloudinaryService.UploadImageAsync(dto.ProofImage, "straycare/tasks");
                if (!uploadResult.Success) return (false, uploadResult.Message);
                
                task.ProofImage = uploadResult.SecureUrl;
            }
            else
            {
                // Use default image if not provided
                task.ProofImage = "/uploads/defaults/default-receipt.png";
            }

            // Handle Receipt Image
            if (dto.ReceiptImage != null && dto.ReceiptImage.Length > 0)
            {
                var uploadResult = await _cloudinaryService.UploadImageAsync(dto.ReceiptImage, "straycare/tasks");
                if (!uploadResult.Success) return (false, uploadResult.Message);

                task.ReceiptImageUrl = uploadResult.SecureUrl;
            }
            else
            {
                // Use default image if not provided
                task.ReceiptImageUrl = "/uploads/defaults/default-receipt.png";
            }

            task.ExpenseAmount = dto.ExpenseAmount;
            task.Status = "PendingApproval";
            
            await _context.SaveChangesAsync();

            return (true, "Task completed and submitted for approval.");
        }

        public async Task<(bool Success, string Message)> AbandonTaskAsync(int taskId, int userId)
        {
            var task = await _context.MedicalTasks.FindAsync(taskId);
            if (task == null)
            {
                return (false, "Task not found.");
            }

            if (task.VolunteerId != userId)
            {
                return (false, "You are not the owner of this task.");
            }

            if (task.Status != "InProgress")
            {
                return (false, "Cannot abandon a task that is not in progress.");
            }

            task.VolunteerId = null;
            task.Status = "Open";
            
            await _context.SaveChangesAsync();

            return (true, "Task abandoned and returned to pool.");
        }
    }
}
