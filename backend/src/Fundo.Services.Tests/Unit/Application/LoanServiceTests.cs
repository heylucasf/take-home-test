using Fundo.Application.DTOs;
using Fundo.Application.Services;
using Fundo.Domain.Entities;
using Fundo.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Fundo.Services.Tests.Unit.Application
{
    public class LoanServiceTests
    {
        private readonly Mock<ILoanRepository> _mockRepository;
        private readonly Mock<ILogger> _mockLogger;
        private readonly LoanService _service;

        public LoanServiceTests()
        {
            _mockRepository = new Mock<ILoanRepository>();
            _mockLogger = new Mock<ILogger>();
            _service = new LoanService(_mockRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
        {
            Action act = () => new LoanService(null!, _mockLogger.Object);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("loanRepository");
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            Action act = () => new LoanService(_mockRepository.Object, null!);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public async Task CreateLoanAsync_WithValidData_ShouldCreateAndReturnLoan()
        {
            var request = new CreateLoanRequest
            {
                Amount = 1500m,
                ApplicantName = "Maria Silva"
            };

            _mockRepository
                .Setup(r => r.AddAsync(It.IsAny<Loan>()))
                .ReturnsAsync((Loan loan) => loan);

            var result = await _service.CreateLoanAsync(request);

            result.Should().NotBeNull();
            result.Amount.Should().Be(request.Amount);
            result.CurrentBalance.Should().Be(request.Amount);
            result.ApplicantName.Should().Be(request.ApplicantName);
            result.Status.Should().Be("active");

            _mockRepository.Verify(r => r.AddAsync(It.IsAny<Loan>()), Times.Once);
        }

        [Fact]
        public async Task CreateLoanAsync_WhenRepositoryThrowsException_ShouldLogErrorAndRethrow()
        {
            var request = new CreateLoanRequest
            {
                Amount = 1500m,
                ApplicantName = "Maria Silva"
            };

            var expectedException = new Exception("Database connection failed");

            _mockRepository
                .Setup(r => r.AddAsync(It.IsAny<Loan>()))
                .ThrowsAsync(expectedException);

            Func<Task> act = async () => await _service.CreateLoanAsync(request);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Database connection failed");

            _mockRepository.Verify(r => r.AddAsync(It.IsAny<Loan>()), Times.Once);
        }

        [Fact]
        public async Task GetLoanByIdAsync_WhenLoanExists_ShouldReturnLoan()
        {
            var loanId = Guid.NewGuid();
            var loan = new Loan(1500m, "João Santos");
            typeof(Loan).GetProperty("Id")!.SetValue(loan, loanId);

            _mockRepository
                .Setup(r => r.GetByIdAsync(loanId))
                .ReturnsAsync(loan);

            var result = await _service.GetLoanByIdAsync(loanId);

            result.Should().NotBeNull();
            result!.Id.Should().Be(loanId);
            result.ApplicantName.Should().Be("João Santos");
        }

        [Fact]
        public async Task GetLoanByIdAsync_WhenLoanDoesNotExist_ShouldReturnNull()
        {
            var loanId = Guid.NewGuid();
            _mockRepository
                .Setup(r => r.GetByIdAsync(loanId))
                .ReturnsAsync((Loan?)null);

            var result = await _service.GetLoanByIdAsync(loanId);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetLoanByIdAsync_WhenRepositoryThrowsException_ShouldLogErrorAndRethrow()
        {
            var loanId = Guid.NewGuid();
            var expectedException = new Exception("Database connection failed");

            _mockRepository
                .Setup(r => r.GetByIdAsync(loanId))
                .ThrowsAsync(expectedException);

            Func<Task> act = async () => await _service.GetLoanByIdAsync(loanId);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Database connection failed");

            _mockRepository.Verify(r => r.GetByIdAsync(loanId), Times.Once);
        }

        [Fact]
        public async Task GetAllLoansAsync_ShouldReturnAllLoans()
        {
            var loans = new List<Loan>
            {
                new Loan(1500m, "Maria Silva"),
                new Loan(3000m, "João Santos"),
                new Loan(5000m, "Ana Costa")
            };

            _mockRepository
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(loans);

            var result = await _service.GetAllLoansAsync();

            result.Should().HaveCount(3);
            result.Select(l => l.ApplicantName).Should().Contain(new[] { "Maria Silva", "João Santos", "Ana Costa" });
        }

        [Fact]
        public async Task GetAllLoansAsync_WhenRepositoryThrowsException_ShouldLogErrorAndRethrow()
        {
            var expectedException = new Exception("Database connection failed");

            _mockRepository
                .Setup(r => r.GetAllAsync())
                .ThrowsAsync(expectedException);

            Func<Task> act = async () => await _service.GetAllLoansAsync();

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Database connection failed");

            _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task MakePaymentAsync_WhenLoanExists_ShouldProcessPaymentAndReturnUpdatedLoan()
        {
            var loanId = Guid.NewGuid();
            var loan = new Loan(1000m, "Pedro Oliveira");
            typeof(Loan).GetProperty("Id")!.SetValue(loan, loanId);

            _mockRepository
                .Setup(r => r.GetByIdAsync(loanId))
                .ReturnsAsync(loan);

            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<Loan>()))
                .Returns(Task.CompletedTask);

            var result = await _service.MakePaymentAsync(loanId, 300m);

            result.Should().NotBeNull();
            result!.CurrentBalance.Should().Be(700m);
            result.Status.Should().Be("active");

            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Loan>()), Times.Once);
        }

        [Fact]
        public async Task MakePaymentAsync_WhenLoanDoesNotExist_ShouldReturnNull()
        {
            var loanId = Guid.NewGuid();
            _mockRepository
                .Setup(r => r.GetByIdAsync(loanId))
                .ReturnsAsync((Loan?)null);

            var result = await _service.MakePaymentAsync(loanId, 300m);

            result.Should().BeNull();
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Loan>()), Times.Never);
        }

        [Fact]
        public async Task MakePaymentAsync_WithFullAmount_ShouldMarkLoanAsPaid()
        {
            var loanId = Guid.NewGuid();
            var loan = new Loan(1000m, "Carla Ferreira");
            typeof(Loan).GetProperty("Id")!.SetValue(loan, loanId);

            _mockRepository
                .Setup(r => r.GetByIdAsync(loanId))
                .ReturnsAsync(loan);

            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<Loan>()))
                .Returns(Task.CompletedTask);

            var result = await _service.MakePaymentAsync(loanId, 1000m);

            result.Should().NotBeNull();
            result!.CurrentBalance.Should().Be(0m);
            result.Status.Should().Be("paid");
        }

        [Fact]
        public async Task MakePaymentAsync_WhenRepositoryGetByIdThrowsException_ShouldLogErrorAndRethrow()
        {
            var loanId = Guid.NewGuid();
            var expectedException = new Exception("Database connection failed");

            _mockRepository
                .Setup(r => r.GetByIdAsync(loanId))
                .ThrowsAsync(expectedException);

            Func<Task> act = async () => await _service.MakePaymentAsync(loanId, 100m);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Database connection failed");

            _mockRepository.Verify(r => r.GetByIdAsync(loanId), Times.Once);
        }

        [Fact]
        public async Task MakePaymentAsync_WhenRepositoryUpdateThrowsException_ShouldLogErrorAndRethrow()
        {
            var loanId = Guid.NewGuid();
            var loan = new Loan(1000m, "Test User");
            typeof(Loan).GetProperty("Id")!.SetValue(loan, loanId);
            var expectedException = new Exception("Database connection failed");

            _mockRepository
                .Setup(r => r.GetByIdAsync(loanId))
                .ReturnsAsync(loan);

            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<Loan>()))
                .ThrowsAsync(expectedException);

            Func<Task> act = async () => await _service.MakePaymentAsync(loanId, 100m);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Database connection failed");

            _mockRepository.Verify(r => r.GetByIdAsync(loanId), Times.Once);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Loan>()), Times.Once);
        }
    }
}