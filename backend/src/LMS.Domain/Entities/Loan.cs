using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS.Domain.Entities
{
    [Table("Loans")]
    public class Loan
    {
        public Guid Id { get; private set; }
        public decimal Amount { get; private set; }
        public decimal CurrentBalance { get; private set; } 
        public string ApplicantName { get; private set; }
        public LoanStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        protected Loan() { }

        public Loan(decimal amount, string applicantName)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
            
            if (string.IsNullOrWhiteSpace(applicantName))
                throw new ArgumentException("Applicant name is required.", nameof(applicantName));

            Id = Guid.NewGuid();
            Amount = amount;
            CurrentBalance = amount;
            ApplicantName = applicantName;
            Status = LoanStatus.Active;
            CreatedAt = DateTime.UtcNow;
        }

        public void MakePayment(decimal paymentAmount)
        {
            if (paymentAmount <= 0)
                throw new ArgumentException("Payment amount must be greater than zero.", nameof(paymentAmount));

            if (Status != LoanStatus.Active)
                throw new InvalidOperationException("Only active loans can receive payments.");

            if (paymentAmount > CurrentBalance)
                throw new InvalidOperationException("Payment amount cannot exceed current balance.");

            CurrentBalance -= paymentAmount;
            UpdatedAt = DateTime.UtcNow;

            if (CurrentBalance == 0)
            {
                Status = LoanStatus.Paid;
            }
        }
    }
}
