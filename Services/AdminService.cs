using Microsoft.EntityFrameworkCore;
using StrayCareAPI.Data;
using StrayCareAPI.DTOs;
using StrayCareAPI.Models;

namespace StrayCareAPI.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public AdminService(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<List<object>> GetPendingVolunteersAsync()
        {
            var pendingVolunteers = await _context.VolunteerProfiles
                .Include(vp => vp.User)
                .Where(vp => vp.User.Role == "Volunteer" && !vp.User.IsApproved)
                .Select(vp => new
                {
                    vp.UserId,
                    vp.User.Username,
                    vp.User.Email,
                    vp.User.PhoneNumber,
                    vp.Skills,
                    vp.Availability,
                    vp.HasVehicle,
                    vp.ExperienceLevel
                })
                .ToListAsync<object>();

            return pendingVolunteers;
        }

        public async Task<(bool Success, string Message)> ApproveVolunteerAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return (false, "User not found.");
            if (user.Role != "Volunteer") return (false, "User is not a volunteer candidate.");
            if (user.IsApproved) return (false, "User is already approved.");

            user.IsApproved = true;
            
            // Log Verification/Action
            var verification = new UserVerification
            {
                UserId = user.Id,
                Code = null, // No code for status update
                ExpiryTime = null,
                Type = VerificationType.Approved,
                IsUsed = true, // Effectively used/done
                Remarks = "Volunteer application approved by Admin",
                CreatedAt = DateTime.UtcNow
            };
            _context.UserVerifications.Add(verification);
            
            await _context.SaveChangesAsync();

            // Send Email
            await _emailService.SendStatusUpdateAsync(
                user.Email, 
                "Volunteer Application Approved", 
                "Congratulations! Your application to become a volunteer has been approved. You can now access volunteer features.",
                user.Username
            );

            return (true, $"Volunteer {user.Username} has been approved.");
        }

        public async Task<(bool Success, string Message)> RejectVolunteerAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return (false, "User not found.");
            if (user.Role != "Volunteer") return (false, "User is not a volunteer candidate.");

            // Downgrade back to normal user
            user.Role = "User";
            user.IsApproved = true; // Approved as a normal user
            
            // Log Verification/Action
            var verification = new UserVerification
            {
                UserId = user.Id,
                Code = null,
                ExpiryTime = null,
                Type = VerificationType.Rejected,
                IsUsed = true,
                Remarks = "Volunteer application rejected by Admin. Role reverted to User.",
                CreatedAt = DateTime.UtcNow
            };
            _context.UserVerifications.Add(verification);

            var profile = await _context.VolunteerProfiles.FirstOrDefaultAsync(vp => vp.UserId == userId);
            if (profile != null)
            {
                _context.VolunteerProfiles.Remove(profile);
            }

            await _context.SaveChangesAsync();

            // Send Email
            await _emailService.SendStatusUpdateAsync(
                user.Email,
                "Volunteer Application Update",
                "We regret to inform you that your volunteer application has been rejected at this time. Your account has been reverted to a standard user account.",
                user.Username
            );

            return (true, $"Volunteer application for {user.Username} has been rejected. Role reverted to User.");
        }

        public async Task<List<object>> GetPendingMedicalTasksAsync()
        {
            var tasks = await _context.MedicalTasks
                .Include(t => t.Animal)
                .Include(t => t.Volunteer)
                .Where(t => t.Status == "PendingApproval")
                .Select(t => new
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
                    // MedicalTask 当前没有 CreatedAt/UpdatedAt，使用关联活动创建时间作为稳定回退，严禁 UtcNow
                    SubmittedAt = _context.DonationCampaigns
                        .Where(c => c.AnimalId == t.AnimalId && c.Type == t.TaskType)
                        .OrderByDescending(c => c.CreatedAt)
                        .Select(c => (DateTime?)c.CreatedAt)
                        .FirstOrDefault() ?? DateTime.UnixEpoch
                })
                .ToListAsync<object>();

            return tasks;
        }

        public async Task<bool> ApproveMedicalTaskAsync(int taskId)
        {
            var task = await _context.MedicalTasks
                .Include(t => t.Animal)
                .Include(t => t.Volunteer)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null) return false;

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                task.Status = "Approved";

                // 发放报销款给志愿者
                if (task.Volunteer != null && task.ExpenseAmount > 0)
                {
                    task.Volunteer.Balance += task.ExpenseAmount;
                    _context.WalletTransactions.Add(new WalletTransaction
                    {
                        UserId = task.Volunteer.Id,
                        Amount = task.ExpenseAmount,
                        TransactionType = "MedicalReimbursement",
                        Timestamp = DateTime.UtcNow,
                        Description = $"Medical task reimbursement for task #{task.Id}"
                    });
                }

                // 保留原有动物健康状态更新逻辑
                if (task.TaskType == CampaignType.Vaccine.ToString()) task.Animal.IsVaccinated = true;
                if (task.TaskType == CampaignType.Sterilization.ToString()) task.Animal.IsNeutered = true;

                // 保留原有众筹任务完成逻辑
                var campaign = await _context.DonationCampaigns
                    .FirstOrDefaultAsync(c => c.AnimalId == task.AnimalId && c.Type == task.TaskType);

                if (campaign != null)
                {
                    campaign.Status = CampaignStatus.Completed.ToString();
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<object>> GetPendingSheltersAsync()
        {
            var pendingShelters = await _context.ShelterProfiles
                .Include(sp => sp.User)
                .Where(sp => sp.User.Role == "Shelter" && !sp.User.IsApproved)
                .Select(sp => new
                {
                    sp.UserId,
                    sp.User.Username,
                    sp.User.Email,
                    sp.User.PhoneNumber,
                    sp.ShelterName,
                    sp.Address,
                    sp.Description,
                    sp.LicenseImageUrl,
                    SubmittedAt = DateTime.UtcNow
                })
                .ToListAsync<object>();

            return pendingShelters;
        }

        public async Task<(bool Success, string Message)> ApproveShelterAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return (false, "User not found.");
            }

            if (user.Role != "Shelter")
            {
                return (false, "User is not a shelter.");
            }

            if (user.IsApproved)
            {
                 return (false, "User is already approved.");
            }

            user.IsApproved = true;

            // Log Verification/Action
            var verification = new UserVerification
            {
                UserId = user.Id,
                Code = null,
                ExpiryTime = null,
                Type = VerificationType.Approved,
                IsUsed = true,
                Remarks = "Shelter account approved by Admin",
                CreatedAt = DateTime.UtcNow
            };
            _context.UserVerifications.Add(verification);

            await _context.SaveChangesAsync();

            // Send Email
            await _emailService.SendStatusUpdateAsync(
                user.Email,
                "Shelter Account Approved",
                "Your Shelter account has been verified and approved. You can now manage your shelter profile and animals.",
                user.Username
            );

            return (true, $"Shelter {user.Username} has been approved.");
        }

        public async Task<(bool Success, string Message)> RejectShelterAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return (false, "User not found.");
            }

            if (user.Role != "Shelter")
            {
                return (false, "User is not a shelter.");
            }
            
            // Log Verification/Action (Before deletion, but user ID constraint might fail if we delete user? 
            // Actually, if we delete the user, we can't add a UserVerification for them because of FK constraint.
            // If the requirement is to "Remove" the shelter, we can't keep the log linked to the user.
            // However, previous logic was: _context.Users.Remove(user).
            // If we remove the user, we can't log to UserVerifications with that UserId effectively if we enforce FK.
            // But let's check the request. user said "add email sending".
            // I will send email BEFORE deleting. 
            // And I will SKIP UserVerification if we are strictly deleting the user, OR I will softly delete/reject?
            // The code is `_context.Users.Remove(user)`. So the user is GONE.
            // So `UserVerification` insert would likely fail or disappear.
            // I will just send the Email.
            
            // Send Email (Send before delete/save so we still have the object, 
            // though actually we shouldn't await email if it blocks DB? 
            // EmailService is async. We can send it.
            
            await _emailService.SendStatusUpdateAsync(
                user.Email,
                "Shelter Account Rejected",
                "Your Shelter account application has been rejected and removed.",
                user.Username
            );

            var profile = await _context.ShelterProfiles.FirstOrDefaultAsync(sp => sp.UserId == userId);
            if (profile != null)
            {
                _context.ShelterProfiles.Remove(profile);
            }
            _context.Users.Remove(user);
            
            await _context.SaveChangesAsync();

            return (true, $"Shelter {user.Username} has been rejected and removed.");
        }

        public async Task<List<object>> GetStrayApplicationsAsync()
        {
            var strayApplications = await _context.AdoptionApplications
                .Include(a => a.ApplicantUser)
                .Include(a => a.Animal)
                .Where(a => a.ShelterId == null && a.Status == "Pending")
                .Select(a => new
                {
                    ApplicationId =a.Id,
                    a.ApplicantUserId,
                    ApplicantName = a.ApplicantUser.Username,
                    ApplicantEmail = a.ApplicantUser.Email,
                    a.AnimalId,
                    AnimalName = a.Animal.Name,
                    BreedId = a.Animal.BreedId,
                    a.Message,
                    a.CreatedAt,
                    a.Status
                })
                .ToListAsync<object>();

            return strayApplications;
        }

        public async Task<(bool Success, string Message)> ApproveApplicationAsync(int appId)
        {
            var application = await _context.AdoptionApplications
                .Include(a => a.Animal)
                .Include(a => a.ApplicantUser)
                .FirstOrDefaultAsync(a => a.Id == appId);

            if (application == null)
            {
                return (false, "Application not found.");
            }

            if (application.ShelterId != null)
            {
                return (false, "This application belongs to a shelter. Admin should only verify stray adoptions.");
            }

            if (application.Status != "Pending")
            {
                return (false, $"Application is already {application.Status}.");
            }

            // 1. Approve Application
            application.Status = "Approved";
            decimal totalDowry = 0;

            // 2. Update Animal Status
            if (application.Animal != null)
            {
                application.Animal.Status = "Adopted";
                application.Animal.OwnerId = application.ApplicantUserId; // [NEW] Set Owner

                // ==========================================
                // [NEW] 嫁妆逻辑：转移捐款与清理遗留医疗任务
                // ==========================================

                // 1. 转移筹款余额到领养人钱包并关闭筹款
                var animalCampaigns = await _context.DonationCampaigns
                    .Where(c => c.AnimalId == application.AnimalId && c.CurrentAmount > 0)
                    .ToListAsync();

                foreach (var campaign in animalCampaigns)
                {
                    totalDowry += campaign.CurrentAmount;
                    campaign.CurrentAmount = 0; // 资金已转移，清零防复用
                    campaign.Status = "Closed"; // 动物已领养，关闭筹款
                }

                if (totalDowry > 0)
                {
                    application.ApplicantUser.Balance += totalDowry; // 直接打入领养人钱包
                }

                // 2. 取消还没人接单的医疗任务 (修复前端志愿者大厅的幽灵任务 Bug)
                var pendingTasks = await _context.MedicalTasks
                    .Where(t => t.AnimalId == application.AnimalId && (t.Status == "Open" || t.Status == "ReadyForVolunteer"))
                    .ToListAsync();
            
                foreach (var task in pendingTasks)
                {
                    task.Status = "Cancelled";
                }
                // ==========================================
            }

            // 3. Reject other pending applications for this animal
            var otherApps = await _context.AdoptionApplications
                .Include(a => a.ApplicantUser) // Include user for notifications
                .Where(a => a.AnimalId == application.AnimalId && a.Id != appId && a.Status == "Pending")
                .ToListAsync();
            
            foreach (var app in otherApps)
            {
                app.Status = "Rejected";
                app.RejectionReason = "Animal adopted by another applicant.";
                
                // Notify rejected applicants
                 await _emailService.SendStatusUpdateAsync(
                    app.ApplicantUser.Email,
                    "Adoption Application Update",
                    $"Your application for {application.Animal?.Name ?? "the animal"} has been closed because the animal was adopted by another applicant.",
                    app.ApplicantUser.Username
                );
            }
            
            // Log Verification for the winner
            var verification = new UserVerification
            {
                UserId = application.ApplicantUserId,
                Code = null, 
                ExpiryTime = null,
                Type = VerificationType.Approved,
                IsUsed = true,
                Remarks = $"Adoption Application {application.Id} for {application.Animal?.Name} Approved by Admin",
                CreatedAt = DateTime.UtcNow
            };
            _context.UserVerifications.Add(verification);

            await _context.SaveChangesAsync();

            // 动态构建邮件内容
            string emailMessage = $"Congratulations! Your application to adopt {application.Animal?.Name} has been APPROVED. Please contact us to arrange pickup.";

            // 如果有嫁妆，追加惊喜提示
            if (totalDowry > 0)
            {
                emailMessage += $" As a special gift, the community's raised fund of RM {totalDowry:F2} for {application.Animal?.Name} has been credited to your wallet to help with their future care!";
            }

            // 发送邮件
            await _emailService.SendStatusUpdateAsync(
                application.ApplicantUser.Email,
                "Adoption Application Approved",
                emailMessage,
                application.ApplicantUser.Username
            );

            return (true, "Application approved. Animal status updated to Adopted.");
        }

        public async Task<(bool Success, string Message)> RejectApplicationAsync(int appId, RejectApplicationDto dto)
        {
            var application = await _context.AdoptionApplications
                .Include(a => a.ApplicantUser)
                .Include(a => a.Animal)
                .FirstOrDefaultAsync(a => a.Id == appId);

            if (application == null)
            {
                return (false, "Application not found.");
            }

            if (application.ShelterId != null)
            {
                 return (false, "This application belongs to a shelter.");
            }

            if (application.Status != "Pending")
            {
                return (false, $"Application is already {application.Status}.");
            }

            application.Status = "Rejected";
            application.RejectionReason = dto.Reason;
            
            // Log Verification
            var verification = new UserVerification
            {
                UserId = application.ApplicantUserId,
                Code = null, 
                ExpiryTime = null,
                Type = VerificationType.Rejected,
                IsUsed = true,
                Remarks = $"Adoption Application {application.Id} for {application.Animal?.Name} Rejected by Admin: {dto.Reason}",
                CreatedAt = DateTime.UtcNow
            };
            _context.UserVerifications.Add(verification);
            
            await _context.SaveChangesAsync();

            // Notify User
            await _emailService.SendStatusUpdateAsync(
                application.ApplicantUser.Email,
                "Adoption Application Update",
                $"Your application for {application.Animal?.Name} has been rejected. Reason: {dto.Reason}",
                application.ApplicantUser.Username
            );

            return (true, "Application rejected.");
        }

        public async Task<List<object>> GetAllUsersAsync()
        {
            return await _context.Users
                .Where(u => u.Role != "Admin")
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.FullName,
                    u.Role,
                    u.IsApproved,
                    u.IsActive,
                    u.PhoneNumber,
                    u.ProfileImageUrl
                })
                .ToListAsync<object>();
        }

        public async Task<(bool Success, string Message)> UpdateUserStatusAsync(int userId, bool isActive)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return (false, "User not found.");

            if (user.Role == "Admin") return (false, "Cannot block Admin.");

            user.IsActive = isActive;
            await _context.SaveChangesAsync();

            return (true, isActive ? "User has been unblocked." : "User has been blocked.");
        }

        public async Task<List<object>> GetAllTopUpRecordsAsync()
        {
            return await _context.WalletTransactions
                .Where(t => t.TransactionType == "Topup")
                .Include(t => t.User)
                .OrderByDescending(t => t.Timestamp)
                .Select(t => new
                {
                    t.Id,
                    UserId = t.UserId,
                    UserName = t.User.Username,
                    t.Amount,
                    t.Timestamp,
                    t.Description
                })
                .ToListAsync<object>();
        }

        public async Task<List<object>> GetAllDonationRecordsAsync()
        {
            return await _context.WalletTransactions
                .Where(t => t.TransactionType == "Donation")
                .Include(t => t.User)
                .OrderByDescending(t => t.Timestamp)
                .Select(t => new
                {
                    t.Id,
                    UserId = t.UserId,
                    UserName = t.User.Username,
                    t.Amount,
                    t.Timestamp,
                    t.Description
                })
                .ToListAsync<object>();
        }


        public async Task<object?> GetUserDetailAsync(int userId)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return null;

            object? profileData = null;

            if (user.Role == "Volunteer")
            {
                profileData = await _context.VolunteerProfiles.AsNoTracking().FirstOrDefaultAsync(v => v.UserId == userId);
            }
            else if (user.Role == "Shelter")
            {
                profileData = await _context.ShelterProfiles.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId);
            }

            return new
            {
                user.Id,
                user.Username,
                user.Email,
                user.FullName,
                user.PhoneNumber,
                user.Address,
                user.Role,
                user.IsApproved,
                user.IsActive,
                user.Balance,
                user.ProfileImageUrl,
                ProfileData = profileData
            };
        }

        public async Task<List<object>> GetAllVerificationLogsAsync()
        {
            return await _context.UserVerifications
                .Include(v => v.User)
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new
                {
                    v.Id,
                    v.UserId,
                    UserName = v.User.Username,
                    UserEmail = v.User.Email,
                    v.Type,
                    TypeName = v.Type.ToString(),
                    v.IsUsed,
                    v.Remarks,
                    v.CreatedAt,
                    v.ExpiryTime
                })
                .ToListAsync<object>();
        }

        // ============================
        // Animal Management (Admin)
        // ============================

        public async Task<IEnumerable<object>> GetAllAnimalsAdminAsync()
        {
            return await _context.Animals
                .Include(a => a.Images)
                .Select(a => new
                {
                    a.Id,
                    a.Name,
                    a.Species,
                    ProfileImageUrl = a.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault()
                                   ?? a.Images.Select(i => i.ImageUrl).FirstOrDefault(),
                    a.LocationName,
                    a.Status,
                    a.BreedId
                })
                .ToListAsync<object>();
        }

        public async Task<object?> GetAnimalByIdAdminAsync(int animalId)
        {
            var animal = await _context.Animals
                .Include(a => a.Breed)
                .Include(a => a.Images)
                .Include(a => a.Shelter).ThenInclude(u => u!.ShelterProfile)
                .Include(a => a.Owner)
                .Include(a => a.Campaigns)
                .Include(a => a.MedicalTasks)
                .FirstOrDefaultAsync(a => a.Id == animalId);

            if (animal == null) return null;

            return new
            {
                animal.Id,
                animal.Name,
                animal.Species,
                animal.BreedId,
                BreedName = animal.Breed?.Name,
                animal.Gender,
                animal.Size,
                animal.EstAge,
                animal.Status,
                animal.IsVaccinated,
                animal.IsNeutered,
                animal.LocationName,
                animal.Latitude,
                animal.Longitude,
                ImageUrls = animal.Images.Select(i => i.ImageUrl).ToList(),
                Shelter = animal.Shelter == null ? null : new
                {
                    animal.Shelter.Id,
                    animal.Shelter.Username,
                    ShelterName = animal.Shelter.ShelterProfile?.ShelterName
                },
                Owner = animal.Owner == null ? null : new
                {
                    animal.Owner.Id,
                    animal.Owner.Username,
                    animal.Owner.Email
                },
                Campaigns = animal.Campaigns.Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Type,
                    c.TargetAmount,
                    c.CurrentAmount,
                    c.Status
                }).ToList(),
                MedicalTasks = animal.MedicalTasks.Select(m => new
                {
                    m.Id,
                    m.TaskType,
                    m.Status,
                    m.ExpenseAmount
                }).ToList()
            };
        }

        public async Task<IEnumerable<ActivityHistoryDto>> GetAnimalActivitiesAdminAsync(int animalId)
        {
            var sightings = await _context.Sightings
                .Where(s => s.AnimalId == animalId)
                .Include(s => s.Reporter)
                .Select(s => new ActivityHistoryDto
                {
                    ActivityType = "Sighting",
                    Id = s.Id,
                    Timestamp = s.Timestamp,
                    LocationName = s.LocationName,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    ImageUrl = s.SightingImageUrl,
                    Description = s.Description,
                    Username = s.Reporter.Username
                })
                .ToListAsync();

            var feedings = await _context.FeedingLogs
                .Where(f => f.AnimalId == animalId)
                .Include(f => f.User)
                .Select(f => new ActivityHistoryDto
                {
                    ActivityType = "Feeding",
                    Id = f.Id,
                    Timestamp = f.FedAt,
                    LocationName = f.LocationName,
                    Latitude = f.Latitude,
                    Longitude = f.Longitude,
                    ImageUrl = f.ProofImageUrl,
                    Description = f.Description,
                    Username = f.User.Username
                })
                .ToListAsync();

            return sightings.Concat(feedings)
                .OrderByDescending(a => a.Timestamp)
                .ToList();
        }
    }
}
