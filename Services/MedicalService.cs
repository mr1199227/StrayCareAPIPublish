using Microsoft.EntityFrameworkCore;
using StrayCareAPI.Data;
using StrayCareAPI.DTOs;
using StrayCareAPI.Models;

namespace StrayCareAPI.Services
{
    public class MedicalService : IMedicalService
    {
        private readonly ApplicationDbContext _context;

        public MedicalService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> SetGoalAsync(SetGoalDto dto)
        {
            // [REF DEPRECATED]: Goals are now Campaigns created automatically.
            // Keeping empty or throwing not supported to avoid breaking interface immediately if dependent.
            // Better to throw to signal change.
             throw new NotSupportedException("SetGoal is deprecated. Campaigns are created automatically.");
        }

        public async Task<Donation> DonateAsync(DonateDto dto, int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            // var animal = await _context.Animals.FindAsync(dto.AnimalId); // Old
            var campaign = await _context.DonationCampaigns
                .Include(c => c.Animal)
                .FirstOrDefaultAsync(c => c.Id == dto.CampaignId);

            if (user == null || campaign == null) throw new Exception("User or Campaign not found.");

            // [NEW] Check if Animal is Adopted
            if (campaign.Animal != null && campaign.Animal.Status == "Adopted")
            {
                throw new Exception("Cannot donate to an adopted animal.");
            }

            if (user.Balance < dto.Amount)
                throw new Exception($"Insufficient balance. Current: {user.Balance}, Required: {dto.Amount}");

            // [NEW] Check Remaining Amount
            decimal remainingAmount = campaign.TargetAmount - campaign.CurrentAmount;
            if (remainingAmount <= 0)
            {
                 throw new Exception("Campaign is already fully funded.");
            }

            if (dto.Amount > remainingAmount)
            {
                throw new Exception($"Donation exceeds required amount. Maximum allowed for {campaign.Title}: {remainingAmount}");
            }

            // 1. Transaction
            user.Balance -= dto.Amount;
            campaign.CurrentAmount += dto.Amount;

            var donation = new Donation
            {
                UserId = userId,
                CampaignId = campaign.Id,
                Amount = dto.Amount,
                Timestamp = DateTime.UtcNow
            };
            _context.Donations.Add(donation);

            var transaction = new WalletTransaction
            {
                UserId = userId,
                Amount = -dto.Amount,
                TransactionType = "Donation",
                Timestamp = DateTime.UtcNow,
                Description = $"Donated to {campaign.Title} for {campaign.Animal?.Name}"
            };
            _context.WalletTransactions.Add(transaction);

            // 2. Check Status & Trigger Task
            if (campaign.Status == CampaignStatus.Active.ToString() && campaign.CurrentAmount >= campaign.TargetAmount)
            {
                campaign.Status = CampaignStatus.FullyFunded.ToString();

                // Trigger Task Generation for Medical Campaigns
                if (campaign.Type == CampaignType.Vaccine.ToString() || 
                    campaign.Type == CampaignType.Sterilization.ToString() ||
                    campaign.Type == CampaignType.Emergency.ToString())
                {
                    // Check if task already exists
                    bool taskExists = await _context.MedicalTasks.AnyAsync(t => t.AnimalId == campaign.AnimalId && t.TaskType == campaign.Type);
                    if (!taskExists)
                    {
                        var task = new MedicalTask
                        {
                            AnimalId = campaign.AnimalId,
                            TaskType = campaign.Type,
                            Status = "Open", // Ready for volunteer to claim
                            ExpenseAmount = 0
                        };
                        _context.MedicalTasks.Add(task);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return donation;
        }

        // [REF CHANGED]: Tasks are now pre-generated when FullyFunded. Volunteers CLAIM existing tasks.
        public async Task<MedicalTask> ClaimTaskAsync(ClaimTaskDto dto)
        {
             // Find OPEN task for this animal and type
            var task = await _context.MedicalTasks
                .FirstOrDefaultAsync(t => t.AnimalId == dto.AnimalId && t.TaskType == dto.TaskType && t.Status == "Open");

            if (task == null)
                throw new Exception("No open task found for this animal. Campaign might not be fully funded yet.");

            task.VolunteerId = dto.VolunteerId;
            task.Status = "InProgress";

            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<bool> CompleteTaskAsync(CompleteTaskDto dto)
        {
            var task = await _context.MedicalTasks.FindAsync(dto.TaskId);
            if (task == null) return false;

            task.Status = "PendingApproval";
            // Proof images are handled in Controller via upload and passed here? 
            // Or reused from dto. existing logic seemed to just set status.
            // Assumption: Controller updates the Task entity with Image URLs before calling this, OR logic is here.
            // Checking previous code: CompleteTaskDto didn't have images in Service, usually Controller handles file upload and updates entity.
            
            await _context.SaveChangesAsync();
            return true;
        }

        // --- NEW: Health Proof Workflow ---

        public async Task<HealthUpdateProof> SubmitHealthProofAsync(SubmitHealthProofDto dto, string imageUrl)
        {
            var proof = new HealthUpdateProof
            {
                AnimalId = dto.AnimalId,
                ProofType = dto.ProofType,
                ImageUrl = imageUrl,
                Description = dto.Description,
                Status = "Pending"
            };
            _context.HealthUpdateProofs.Add(proof);
            await _context.SaveChangesAsync();
            return proof;
        }

        public async Task<bool> ApproveHealthProofAsync(int proofId, ApproveProofDto dto)
        {
            var proof = await _context.HealthUpdateProofs.Include(p => p.Animal).FirstOrDefaultAsync(p => p.Id == proofId);
            if (proof == null) return false;

            if (!dto.IsApproved)
            {
                proof.Status = "Rejected";
                // proof.RejectionReason = dto.Reason; // If entity had it
                await _context.SaveChangesAsync();
                return true;
            }

            proof.Status = "Approved";

            // 1. Update Animal
            if (proof.ProofType == "Vaccine") proof.Animal!.IsVaccinated = true;
            if (proof.ProofType == "Neuter") proof.Animal!.IsNeutered = true;

            // 2. Close Campaign & Transfer Funds
            string campaignType = proof.ProofType == "Vaccine" ? CampaignType.Vaccine.ToString() : CampaignType.Sterilization.ToString();
            
            var targetCampaign = await _context.DonationCampaigns
                .FirstOrDefaultAsync(c => c.AnimalId == proof.AnimalId && c.Type == campaignType);

            if (targetCampaign != null && targetCampaign.Status != CampaignStatus.Closed.ToString() && targetCampaign.Status != CampaignStatus.Completed.ToString())
            {
                // Transfer remaining funds to Food campaign
                if (targetCampaign.CurrentAmount > 0)
                {
                    var foodCampaign = await _context.DonationCampaigns
                        .FirstOrDefaultAsync(c => c.AnimalId == proof.AnimalId && c.Type == CampaignType.Food.ToString());
                    
                    if (foodCampaign != null)
                    {
                         foodCampaign.CurrentAmount += targetCampaign.CurrentAmount;
                         // Optional: Log internal transaction/audit log
                    }
                }
                
                targetCampaign.Status = CampaignStatus.Closed.ToString();
                targetCampaign.CurrentAmount = 0; // Moved away
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<DonationCampaign> CreateEmergencyCampaignAsync(CreateEmergencyCampaignDto dto)
        {
            var animal = await _context.Animals.FindAsync(dto.AnimalId);
            if (animal == null)
            {
                throw new Exception("Animal not found.");
            }

            if (animal.Status != "Stray")
            {
                throw new Exception("Emergency campaigns can only be created for Stray animals.");
            }

            var campaign = new DonationCampaign
            {
                AnimalId = dto.AnimalId,
                Title = dto.Title,
                Type = CampaignType.Emergency.ToString(),
                TargetAmount = dto.TargetAmount,
                Status = CampaignStatus.Active.ToString()
            };
            _context.DonationCampaigns.Add(campaign);
            await _context.SaveChangesAsync();
            return campaign;
        }

        public async Task<List<object>> GetAllTasksAsync(string? status = null)
        {
            var query = _context.MedicalTasks
                .Include(t => t.Animal)
                .Include(t => t.Volunteer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.Status == status);
            }

            var tasks = await query.Select(t => new
                {
                    t.Id,
                    t.TaskType,
                    t.Status,
                    t.ExpenseAmount,
                    t.ProofImage,
                    t.ReceiptImageUrl,
                    AnimalName = t.Animal.Name,
                    AnimalType = t.Animal.Species,
                    VolunteerName = t.Volunteer != null ? t.Volunteer.Username : "Unknown",
                    SubmittedAt = DateTime.UtcNow 
                })
                .ToListAsync<object>();
            return tasks;
        }

        public async Task<List<object>> GetPublicTasksAsync()
        {
            var tasks = await _context.MedicalTasks
                .Include(t => t.Animal)
                .Include(t => t.Volunteer) // Include Volunteer
                .Where(t => t.Status == "Approved") // Only approved/completed tasks
                .OrderByDescending(t => t.Id) // Newest first
                .Take(50) // Limit to 50 for now
                .Select(t => new
                {
                    t.Id,
                    t.TaskType,
                    // t.Status, // Always Approved
                    // t.ExpenseAmount, // Maybe keep private? Or public for transparency. Let's include basic info.
                    t.ProofImage,
                    AnimalName = t.Animal.Name,
                    AnimalType = t.Animal.Species,
                    VolunteerName = t.Volunteer != null ? t.Volunteer.Username : "Unknown", // Add VolunteerName
                    CompletedAt = DateTime.UtcNow // Placeholder
                })
                .ToListAsync<object>();
            return tasks;
        }
    }
}
