using System.ComponentModel.DataAnnotations;

namespace Fundo.Application.DTOs
{
    public class MakePaymentRequest
    {
        [Required(ErrorMessage = "Payment amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than zero")]
        public decimal PaymentAmount { get; set; }
    }
}