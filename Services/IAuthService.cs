using StrayCareAPI.DTOs;
using Microsoft.AspNetCore.Mvc;
using StrayCareAPI.Models;

namespace StrayCareAPI.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Message, object? Data)> RegisterAsync(RegisterDto dto);
        Task<(bool Success, string Message)> SendEmailVerificationAsync(string email, VerificationType type = VerificationType.Email);
        Task<(bool Success, string Message)> VerifyEmailAsync(string email, string code);
        Task<(bool Success, string Message)> ApplyVolunteerAsync(ApplyVolunteerDto dto);
        Task<(bool Success, string Message)> RegisterShelterAsync(RegisterShelterDto dto);
        Task<(bool Success, string Message, object? Data)> LoginAsync(LoginDto dto);
        Task<(bool Success, string Message, string? Data)> ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordDto dto);
        Task<(bool Success, string Message, object? Data)> EditProfileAsync(int userId, EditUserProfileDto dto);
        Task<object?> GetCurrentUserProfileAsync(int userId);
    }
}
