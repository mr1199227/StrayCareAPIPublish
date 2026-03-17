using System.ComponentModel.DataAnnotations;
using StrayCareAPI.Models;

namespace StrayCareAPI.DTOs
{
    public class SendVerificationDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public VerificationType Type { get; set; } = VerificationType.Email;
    }

    public class VerifyEmailDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;
    }
}
