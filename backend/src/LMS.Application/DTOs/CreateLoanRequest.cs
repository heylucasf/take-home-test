using System.ComponentModel.DataAnnotations;

namespace LMS.Application.DTOs
{
    public class CreateLoanRequest
    {
        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Applicant name is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Applicant name must be between 1 and 200 characters")]
        public string ApplicantName { get; set; }
    }
}