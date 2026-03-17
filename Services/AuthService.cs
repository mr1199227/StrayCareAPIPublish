using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StrayCareAPI.Data;
using StrayCareAPI.DTOs;
using StrayCareAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace StrayCareAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly ICloudinaryService _cloudinaryService;

        public AuthService(ApplicationDbContext context, IConfiguration configuration, IEmailService emailService, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<(bool Success, string Message, object? Data)> RegisterAsync(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return (false, "This email address has already been registered.", null);
            }

            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
            {
                return (false, "Username already exists.", null);
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                PasswordHash = passwordHash,
                Role = dto.Role,
                FullName = dto.FullName,
                Balance = 0,
                IsApproved = false,              
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return (true, "Registration successful. Please verify your email via OTP to login.", new
            {
                userId = user.Id,
                username = user.Username,
                email = user.Email
            });
        }

        public async Task<(bool Success, string Message)> ApplyVolunteerAsync(ApplyVolunteerDto dto)
        {
            var user = await _context.Users.FindAsync(dto.UserId!.Value);
            if (user == null)
            {
                return (false, "User not found.");
            }

            if (user.Role == "Volunteer" && user.IsApproved)
            {
                return (false, "User is already an approved volunteer.");
            }

            // Update User Role to Volunteer (but not approved yet)
            user.Role = "Volunteer";
            user.IsApproved = false;

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    await _context.SaveChangesAsync();

                    // Check if profile exists, if so update it, else create new
                    var profile = await _context.VolunteerProfiles.FirstOrDefaultAsync(vp => vp.UserId == user.Id);
                    if (profile == null)
                    {
                        profile = new VolunteerProfile
                        {
                            UserId = user.Id,
                            Skills = dto.Skills,
                            Availability = dto.Availability,
                            HasVehicle = dto.HasVehicle,
                            ExperienceLevel = dto.ExperienceLevel
                        };
                        _context.VolunteerProfiles.Add(profile);
                    }
                    else
                    {
                        profile.Skills = dto.Skills;
                        profile.Availability = dto.Availability;
                        profile.HasVehicle = dto.HasVehicle;
                        profile.ExperienceLevel = dto.ExperienceLevel;
                    }
        
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return (true, "Volunteer application submitted successfully. Please wait for Admin approval.");
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return (false, "An error occurred during application.");
                }
            }
        }

        public async Task<(bool Success, string Message)> RegisterShelterAsync(RegisterShelterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return (false, "This email address has already been registered.");
            }

            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
            {
                return (false, "Username already exists.");
            }

            // 1. Handle License Image Upload
            string? licenseUrl = null;
            if (dto.LicenseFile != null && dto.LicenseFile.Length > 0)
            {
                var uploadResult = await _cloudinaryService.UploadImageAsync(dto.LicenseFile, "straycare/licenses");
                if (!uploadResult.Success)
                {
                    return (false, "Failed to upload license: " + uploadResult.Message);
                }

                licenseUrl = uploadResult.SecureUrl;
            }

            // 2. Handle Profile Image Upload (Optional)
            string? profileImageUrl = null;
            if (dto.ProfileImage != null && dto.ProfileImage.Length > 0)
            {
                var profileUploadResult = await _cloudinaryService.UploadImageAsync(dto.ProfileImage, "straycare/profiles");
                if (!profileUploadResult.Success)
                {
                    return (false, "Failed to upload profile image: " + profileUploadResult.Message);
                }

                profileImageUrl = profileUploadResult.SecureUrl;
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // 3. Save ProfileImageUrl to User table
            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                PasswordHash = passwordHash,
                Role = "Shelter",
                IsApproved = false,
                Balance = 0,
                ProfileImageUrl = profileImageUrl
            };

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    var profile = new ShelterProfile
                    {
                        UserId = user.Id,
                        ShelterName = dto.ShelterName,
                        Address = dto.Address,
                        Latitude = dto.Latitude,
                        Longitude = dto.Longitude,
                        Description = dto.Description,
                        LicenseImageUrl = licenseUrl ?? string.Empty
                    };

                    _context.ShelterProfiles.Add(profile);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return (true, "Registration successful. Please wait for Admin approval.");
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return (false, "An error occurred during registration.");
                }
            }
        }

        public async Task<(bool Success, string Message, object? Data)> LoginAsync(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                return (false, "Incorrect email or password", null);
            }

            // Check if user is blocked
            if (!user.IsActive)
            {
                return (false, "Account is suspended. Please contact admin.", null);
            }

            if (!user.IsApproved)
            {
                // A. Volunteer: Allow login (Proceed to token generation, where role is downgraded)
                if (user.Role == "Volunteer")
                {
                    // Do nothing here, let it pass to effectiveRole logic below
                }
                else if(user.Role == "Admin")
                {
                    // Do nothing here, let it pass to effectiveRole logic below
                }
                // B. Shelter: Check email verification status
                else if (user.Role == "Shelter")
                {
                    var isEmailVerified = await _context.UserVerifications
                        .AnyAsync(v => v.UserId == user.Id && 
                                       v.Type == VerificationType.Email && 
                                       v.IsUsed &&
                                       v.Remarks == "Verified successfully");

                    if (isEmailVerified)
                    {
                        return (false, "Email verified. Waiting for Admin approval.", null);
                    }
                    else
                    {
                        return (false, "Please verify your email first.", null);
                    }
                }
                // C. Regular User (or others): Block
                else
                {
                    return (false, "Account is pending verification.", null);
                }
            }

            // Generate JWT Token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            // Downgrade role if pending approval (for Volunteers)
            // If they are not approved yet, they login as simple User
            var effectiveRole = user.Role;
            if (user.Role == "Volunteer" && !user.IsApproved)
            {
                effectiveRole = "User";
            }
            var isPending = (user.Role == "Volunteer" && !user.IsApproved);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, effectiveRole),
                    new Claim(ClaimTypes.Name, user.Username)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return (true, "Login successful", new
            {
                token = tokenString,
                userId = user.Id,
                username = user.Username,
                email = user.Email,
                phoneNumber = user.PhoneNumber,
                role = effectiveRole,
                balance = user.Balance,
                isPendingVolunteer = isPending,
                profileImageUrl = user.ProfileImageUrl
            });
        }


        public async Task<(bool Success, string Message, string? Data)> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var result = await SendEmailVerificationAsync(dto.Email, VerificationType.PasswordReset);
            if (!result.Success)
            {
                return (false, result.Message, null);
            }
            return (true, result.Message, null);
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
            {
                return (false, "Invalid email address.");
            }

            var verification = await _context.UserVerifications
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync(v => 
                    v.UserId == user.Id && 
                    v.Type == VerificationType.PasswordReset && 
                    v.Code == dto.Code
                );

            if (verification == null)
            {
                return (false, "Invalid verification code.");
            }

            if (verification.IsUsed)
            {
                 return (false, "This code has already been used.");
            }

            if (verification.ExpiryTime < DateTime.UtcNow)
            {
                return (false, "Verification code has expired.");
            }

            // Check if new password is same as current password
            if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.PasswordHash))
            {
                return (false, "New password cannot be the same as your current password.");
            }

            // Verify Success
            verification.IsUsed = true;
            verification.Remarks = "Password reset successful";


            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            
            // Clear old token fields just in case (optional cleanup)
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            await _context.SaveChangesAsync();

            return (true, "Password reset successful. Please log in with your new password.");
        }

        public async Task<(bool Success, string Message, object? Data)> EditProfileAsync(int userId, EditUserProfileDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return (false, "User not found.", null);
            }

            // Update fields if provided (and not null/empty string if we want to prevent clearing name?)
            // Assuming FullName is required so we don't clear it if empty string passed by mistake? 
            // DTO says nullable. If null, ignore. If empty string... let's accept it if logic allows, 
            // but Model says [Required] FullName. So let's check !string.IsNullOrWhiteSpace
            
            if (!string.IsNullOrWhiteSpace(dto.FullName))
            {
                user.FullName = dto.FullName;
            }

            if (dto.PhoneNumber != null) // Allow empty string to clear phone? Or just update whatever sent.
            {
                user.PhoneNumber = dto.PhoneNumber;
            }

            if (dto.Address != null)
            {
                user.Address = dto.Address;
            }

            // Handle Profile Image
            if (dto.ProfileImage != null && dto.ProfileImage.Length > 0)
            {
                var uploadResult = await _cloudinaryService.UploadImageAsync(dto.ProfileImage, "straycare/profiles");
                if (!uploadResult.Success)
                {
                    return (false, uploadResult.Message, null);
                }

                user.ProfileImageUrl = uploadResult.SecureUrl;
            }

            await _context.SaveChangesAsync();

            // Handle Expanded Profile Data (Volunteer / Shelter)
            if (user.Role == "Volunteer" && dto.VolunteerData != null)
            {
                var vProfile = await _context.VolunteerProfiles.FirstOrDefaultAsync(v => v.UserId == userId);
                if (vProfile != null)
                {
                    if (dto.VolunteerData.Skills != null) vProfile.Skills = dto.VolunteerData.Skills;
                    if (dto.VolunteerData.Availability != null) vProfile.Availability = dto.VolunteerData.Availability;
                    if (dto.VolunteerData.HasVehicle.HasValue) vProfile.HasVehicle = dto.VolunteerData.HasVehicle.Value;
                    if (dto.VolunteerData.ExperienceLevel != null) vProfile.ExperienceLevel = dto.VolunteerData.ExperienceLevel;
                }
                // If profile doesn't exist (shouldn't happen for approved vol, but safeguard), maybe create?
                // For now, only update if exists.
            }
            else if (user.Role == "Shelter" && dto.ShelterData != null)
            {
                var sProfile = await _context.ShelterProfiles.FirstOrDefaultAsync(s => s.UserId == userId);
                if (sProfile != null)
                {
                    if (dto.ShelterData.ShelterName != null) sProfile.ShelterName = dto.ShelterData.ShelterName;
                    if (dto.ShelterData.Address != null) sProfile.Address = dto.ShelterData.Address; // Shelter specific address
                    if (dto.ShelterData.Description != null) sProfile.Description = dto.ShelterData.Description;
                    if (dto.ShelterData.Latitude.HasValue) sProfile.Latitude = dto.ShelterData.Latitude.Value;
                    if (dto.ShelterData.Longitude.HasValue) sProfile.Longitude = dto.ShelterData.Longitude.Value;
                }
            }

            await _context.SaveChangesAsync();

            // Fetch updated profile data for response
            object? profileData = null;
            if (user.Role == "Volunteer")
            {
               profileData = await _context.VolunteerProfiles.FirstOrDefaultAsync(v => v.UserId == userId);
            }
            else if (user.Role == "Shelter")
            {
               profileData = await _context.ShelterProfiles.FirstOrDefaultAsync(s => s.UserId == userId);
            }

            // Return updated user info
            return (true, "Profile updated successfully.", new
            {
                userId = user.Id,
                username = user.Username,
                email = user.Email,
                fullName = user.FullName,
                phoneNumber = user.PhoneNumber,
                address = user.Address,
                profileImageUrl = user.ProfileImageUrl,
                role = user.Role,
                balance = user.Balance,
                profileData = profileData
            });
        }

        public async Task<object?> GetCurrentUserProfileAsync(int userId)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return null;

            // Fetch role-specific profile data
            object? profileData = null;
            if (user.Role == "Volunteer")
            {
                profileData = await _context.VolunteerProfiles.AsNoTracking().FirstOrDefaultAsync(v => v.UserId == userId);
            }
            else if (user.Role == "Shelter")
            {
                profileData = await _context.ShelterProfiles.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId);
            }

            // Return complete user profile (excluding password)
            return new
            {
                userId = user.Id,
                username = user.Username,
                email = user.Email,
                fullName = user.FullName,
                phoneNumber = user.PhoneNumber,
                address = user.Address,
                profileImageUrl = user.ProfileImageUrl,
                role = user.Role,
                balance = user.Balance,
                isActive = user.IsActive,
                isApproved = user.IsApproved,
                profileData = profileData
            };
        }

        public async Task<(bool Success, string Message)> SendEmailVerificationAsync(string email, VerificationType type = VerificationType.Email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return (false, "User not found.");
            }

            // Verification Check: Only applicable if type is Email
            if (type == VerificationType.Email)
            {
                if (user.IsApproved)
                {
                    return (false, "Email is already verified.");
                }

                // For Shelter/Volunteer, check if they already have a verified email record
                if (user.Role == "Shelter" || user.Role == "Volunteer")
                {
                     var isEmailVerified = await _context.UserVerifications
                        .AnyAsync(v => v.UserId == user.Id && 
                                       v.Type == VerificationType.Email && 
                                       v.IsUsed &&
                                       v.Remarks == "Verified successfully");
                     if (isEmailVerified)
                     {
                         return (false, "Email is already verified.");
                     }
                }
            }

            // Anti-Spam: Check if a code was sent recently (e.g., within last 1 minute)
            var lastRequest = await _context.UserVerifications
                .Where(v => v.UserId == user.Id && v.Type == type)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastRequest != null && lastRequest.CreatedAt > DateTime.UtcNow.AddMinutes(-1) && !lastRequest.IsUsed)
            {
                return (false, "Please wait 1 minute before requesting a new code.");
            }

            // Invalidate previous codes of the same type
            var existingCodes = await _context.UserVerifications
                .Where(v => v.UserId == user.Id && v.Type == type && !v.IsUsed)
                .ToListAsync();
            
            foreach (var c in existingCodes)
            {
                c.IsUsed = true; 
                c.Remarks = "System: Invalidated by new request";
            }

            // Generate 6-digit code
            var code = new Random().Next(100000, 999999).ToString();

            string remarks = type switch
            {
                VerificationType.Email => "Requested email verification",
                VerificationType.PasswordReset => "Requested password reset",
                _ => "Requested verification"
            };

            var verification = new UserVerification
            {
                UserId = user.Id,
                Code = code,
                ExpiryTime = DateTime.UtcNow.AddMinutes(10),
                Type = type,
                IsUsed = false,
                Remarks = remarks
            };

            _context.UserVerifications.Add(verification);
            await _context.SaveChangesAsync();

            // Send Email
            if (_emailService != null)
            {
                try 
                {
                    await _emailService.SendOtpAsync(user.Email, code);
                }
                catch (Exception ex)
                {
                    return (false, "Failed to send email. Please try again later.");
                }
            }

            return (true, "Verification code sent to your email.");
        }

        public async Task<(bool Success, string Message)> VerifyEmailAsync(string email, string code)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return (false, "User not found.");
            }

            if (user.IsApproved)
            {
                return (true, "Email is already verified.");
            }

            // For Shelter/Volunteer, check if they already have a verified email record
            if (user.Role == "Shelter" || user.Role == "Volunteer")
            {
                 var isEmailVerified = await _context.UserVerifications
                    .AnyAsync(v => v.UserId == user.Id && 
                                   v.Type == VerificationType.Email && 
                                   v.IsUsed &&
                                   v.Remarks == "Verified successfully");
                 if (isEmailVerified)
                 {
                     return (true, "Email is already verified.");
                 }
            }

            var verification = await _context.UserVerifications
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync(v => 
                    v.UserId == user.Id && 
                    v.Type == VerificationType.Email && 
                    v.Code == code
                );

            if (verification == null)
            {
                return (false, "Invalid verification code.");
            }

            if (verification.IsUsed)
            {
                 return (false, "This code has already been used.");
            }

            if (verification.ExpiryTime < DateTime.UtcNow)
            {
                return (false, "Verification code has expired.");
            }

            // SUCCESS
            verification.IsUsed = true;
            verification.Remarks = "Verified successfully";
            
            // For standard User, this approves the account. 
            // For Shelter/Volunteer, they might still need Admin approval, but Email is verified.
            // Our logic: "User" -> IsApproved = true.
            // "Volunteer"/"Shelter" -> IsApproved stays false (waiting for Admin), 
            // BUT we assume "IsEmailVerified" was the goal. 
            // Since we removed "IsEmailVerified", we have to be careful.
            // If user is "User", set IsApproved = true.
            // If user is "Shelter", verify step is just one part. But wait, removing IsEmailVerified means we merge them.
            // Let's assume for "User" role, this sets IsApproved = true.
            
            if (user.Role == "User")
            {
                user.IsApproved = true;
            }
            else 
            {
                // For Shelter/Volunteer, verifying email might be a prerequisite, 
                // but checking "IsApproved" for them means Admin Approval.
                // We might need a way to track "Email Verified" independently if we want strictly robust flow.
                // BUT user decided to remove IsEmailVerified field.
                // So, for now, we just consume the code. 
                // Maybe we can add a remark in UserVerification saying "Email Verified".
                // Or maybe we treat this as "Pre-check passed". 
                // Let's just mark code used. The User role upgrade is key here.
            }

            await _context.SaveChangesAsync();

            return (true, "Email verified successfully.");
        }
    }
}
