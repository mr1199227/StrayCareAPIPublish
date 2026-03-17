using System.ComponentModel.DataAnnotations;

namespace StrayCareAPI.DTOs
{
    public class CompleteTaskDto
    {
        [Required]
        public int TaskId { get; set; }

        public IFormFile? ProofImage { get; set; }

        public IFormFile? ReceiptImage { get; set; }

        public decimal ExpenseAmount { get; set; } = 0;
    }
}
