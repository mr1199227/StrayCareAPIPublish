using System.ComponentModel.DataAnnotations;

namespace StrayCareAPI.DTOs
{
    public class RejectApplicationDto
    {
        [Required]
        public string Reason { get; set; } = string.Empty;
    }
}
