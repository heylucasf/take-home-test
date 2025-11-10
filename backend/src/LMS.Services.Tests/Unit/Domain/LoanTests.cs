using LMS.Domain.Entities;
using FluentAssertions;
using System;
using Xunit;

namespace LMS.Services.Tests.Unit.Domain
{
    public class LoanTests
    {
        [Fact]
        public void Constructor_WithValidData_ShouldCreateLoan()
        {
            var amount = 1500m;
            var applicantName = "Maria Silva";

            var loan = new Loan(amount, applicantName);

            loan.Should().NotBeNull();
            loan.Id.Should().NotBeEmpty();
            loan.Amount.Should().Be(amount);
            loan.CurrentBalance.Should().Be(amount);
            loan.ApplicantName.Should().Be(applicantName);
            loan.Status.Should().Be(LoanStatus.Active);
            loan.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            loan.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        [InlineData(-0.01)]
        public void Constructor_WithInvalidAmount_ShouldThrowArgumentException(decimal invalidAmount)
        {
            var applicantName = "Maria Silva";

            Action act = () => new Loan(invalidAmount, applicantName);

            act.Should().Throw<ArgumentException>()
                .WithMessage("Amount must be greater than zero.*");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithInvalidApplicantName_ShouldThrowArgumentException(string invalidName)
        {
            var amount = 1500m;

            Action act = () => new Loan(amount, invalidName);

            act.Should().Throw<ArgumentException>()
                .WithMessage("Applicant name is required.*");
        }

        [Fact]
        public void MakePayment_WithValidAmount_ShouldReduceBalance()
        {
            var loan = new Loan(1000m, "João Santos");
            var paymentAmount = 300m;

            loan.MakePayment(paymentAmount);

            loan.CurrentBalance.Should().Be(700m);
            loan.UpdatedAt.Should().NotBeNull();
            loan.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            loan.Status.Should().Be(LoanStatus.Active);
        }

        [Fact]
        public void MakePayment_WithFullAmount_ShouldMarkAsPaid()
        {
            var loan = new Loan(1000m, "Ana Costa");

            loan.MakePayment(1000m);

            loan.CurrentBalance.Should().Be(0m);
            loan.Status.Should().Be(LoanStatus.Paid);
            loan.UpdatedAt.Should().NotBeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-50)]
        public void MakePayment_WithInvalidAmount_ShouldThrowArgumentException(decimal invalidAmount)
        {
            var loan = new Loan(1000m, "Pedro Oliveira");

            Action act = () => loan.MakePayment(invalidAmount);

            act.Should().Throw<ArgumentException>()
                .WithMessage("Payment amount must be greater than zero.*");
        }

        [Fact]
        public void MakePayment_WhenLoanIsPaid_ShouldThrowInvalidOperationException()
        {
            var loan = new Loan(1000m, "Carla Ferreira");
            loan.MakePayment(1000m);

            Action act = () => loan.MakePayment(100m);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Only active loans can receive payments.");
        }

        [Fact]
        public void MakePayment_WhenAmountExceedsBalance_ShouldThrowInvalidOperationException()
        {
            var loan = new Loan(1000m, "Ricardo Alves");

            Action act = () => loan.MakePayment(1500m);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Payment amount cannot exceed current balance.");
        }

        [Fact]
        public void MakePayment_MultiplePayments_ShouldAccumulateCorrectly()
        {
            var loan = new Loan(1000m, "Juliana Martins");

            loan.MakePayment(200m);
            loan.MakePayment(300m);
            loan.MakePayment(500m);

            loan.CurrentBalance.Should().Be(0m);
            loan.Status.Should().Be(LoanStatus.Paid);
        }
    }
}