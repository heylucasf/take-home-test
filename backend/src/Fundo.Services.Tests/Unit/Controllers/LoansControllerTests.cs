using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Fundo.Application.DTOs;
using Fundo.Application.Interfaces;
using Fundo.Applications.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Serilog;
using Xunit;

namespace Fundo.Services.Tests.Unit.Controllers
{
    public class LoansControllerTests
    {
        private readonly Mock<ILoanService> _serviceMock;
        private readonly Mock<ILogger> _loggerMock;

        public LoansControllerTests()
        {
            _serviceMock = new Mock<ILoanService>(MockBehavior.Strict);
            _loggerMock = new Mock<ILogger>();
        }

        [Fact]
        public async Task CreateLoan_ModelStateInvalid_ShouldReturnBadRequest()
        {
            var controller = new LoansController(_serviceMock.Object, _loggerMock.Object);
            controller.ModelState.AddModelError("Amount", "Amount is required");
            var request = new CreateLoanRequest { Amount = 0, ApplicantName = "" };

            var result = await controller.CreateLoan(request);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateLoan_ServiceThrowsArgumentException_ShouldReturnBadRequest()
        {
            var controller = new LoansController(_serviceMock.Object, _loggerMock.Object);
            var request = new CreateLoanRequest { Amount = 1500m, ApplicantName = "Maria Silva" };

            _serviceMock
                .Setup(s => s.CreateLoanAsync(It.IsAny<CreateLoanRequest>()))
                .ThrowsAsync(new ArgumentException("Invalid data"));

            var result = await controller.CreateLoan(request);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result.Result as BadRequestObjectResult;
            badRequest!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateLoan_WithValidData_ShouldReturnCreated()
        {
            var controller = new LoansController(_serviceMock.Object, _loggerMock.Object);
            var request = new CreateLoanRequest { Amount = 1500m, ApplicantName = "Maria Silva" };
            var expectedResponse = new LoanResponse
            {
                Id = Guid.NewGuid(),
                Amount = 1500m,
                CurrentBalance = 1500m,
                ApplicantName = "Maria Silva",
                Status = "active",
                CreatedAt = DateTime.UtcNow
            };

            _serviceMock
                .Setup(s => s.CreateLoanAsync(It.IsAny<CreateLoanRequest>()))
                .ReturnsAsync(expectedResponse);

            var result = await controller.CreateLoan(request);

            result.Result.Should().BeOfType<CreatedAtActionResult>();
            var createdResult = result.Result as CreatedAtActionResult;
            createdResult!.Value.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task GetLoanById_WhenLoanExists_ShouldReturnOk()
        {
            var controller = new LoansController(_serviceMock.Object, _loggerMock.Object);
            var loanId = Guid.NewGuid();
            var expectedLoan = new LoanResponse
            {
                Id = loanId,
                Amount = 1500m,
                CurrentBalance = 1500m,
                ApplicantName = "Maria Silva",
                Status = "active",
                CreatedAt = DateTime.UtcNow
            };

            _serviceMock
                .Setup(s => s.GetLoanByIdAsync(loanId))
                .ReturnsAsync(expectedLoan);

            var result = await controller.GetLoanById(loanId);

            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expectedLoan);
        }

        [Fact]
        public async Task GetLoanById_WhenLoanNotExists_ShouldReturnNotFound()
        {
            var controller = new LoansController(_serviceMock.Object, _loggerMock.Object);
            var loanId = Guid.NewGuid();

            _serviceMock
                .Setup(s => s.GetLoanByIdAsync(loanId))
                .ReturnsAsync((LoanResponse?)null);

            var result = await controller.GetLoanById(loanId);

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetAllLoans_ShouldReturnOkWithLoans()
        {
            var controller = new LoansController(_serviceMock.Object, _loggerMock.Object);
            var expectedLoans = new List<LoanResponse>
            {
                new LoanResponse
                {
                    Id = Guid.NewGuid(),
                    Amount = 1500m,
                    CurrentBalance = 1500m,
                    ApplicantName = "Maria Silva",
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                },
                new LoanResponse
                {
                    Id = Guid.NewGuid(),
                    Amount = 2000m,
                    CurrentBalance = 1000m,
                    ApplicantName = "João Santos",
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _serviceMock
                .Setup(s => s.GetAllLoansAsync())
                .ReturnsAsync(expectedLoans);

            var result = await controller.GetAllLoans();

            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expectedLoans);
        }

        [Fact]
        public async Task MakePayment_WithValidData_ShouldReturnOk()
        {
            var controller = new LoansController(_serviceMock.Object, _loggerMock.Object);
            var loanId = Guid.NewGuid();
            var request = new MakePaymentRequest { PaymentAmount = 500m };
            var expectedResponse = new LoanResponse
            {
                Id = loanId,
                Amount = 1500m,
                CurrentBalance = 1000m,
                ApplicantName = "Maria Silva",
                Status = "active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _serviceMock
                .Setup(s => s.MakePaymentAsync(loanId, request.PaymentAmount))
                .ReturnsAsync(expectedResponse);

            var result = await controller.MakePayment(loanId, request);

            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task MakePayment_WhenLoanNotExists_ShouldReturnNotFound()
        {
            var controller = new LoansController(_serviceMock.Object, _loggerMock.Object);
            var loanId = Guid.NewGuid();
            var request = new MakePaymentRequest { PaymentAmount = 500m };

            _serviceMock
                .Setup(s => s.MakePaymentAsync(loanId, request.PaymentAmount))
                .ReturnsAsync((LoanResponse?)null);

            var result = await controller.MakePayment(loanId, request);

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task MakePayment_ServiceThrowsInvalidOperationException_ShouldReturnBadRequest()
        {
            var controller = new LoansController(_serviceMock.Object, _loggerMock.Object);
            var id = Guid.NewGuid();
            var request = new MakePaymentRequest { PaymentAmount = 100m };

            _serviceMock
                .Setup(s => s.MakePaymentAsync(id, request.PaymentAmount))
                .ThrowsAsync(new InvalidOperationException("Payment not allowed"));

            var result = await controller.MakePayment(id, request);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
            (result.Result as BadRequestObjectResult)!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task MakePayment_ServiceThrowsArgumentException_ShouldReturnBadRequest()
        {
            var controller = new LoansController(_serviceMock.Object, _loggerMock.Object);
            var id = Guid.NewGuid();
            var request = new MakePaymentRequest { PaymentAmount = 0m };

            _serviceMock
                .Setup(s => s.MakePaymentAsync(id, request.PaymentAmount))
                .ThrowsAsync(new ArgumentException("Invalid amount"));

            var result = await controller.MakePayment(id, request);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
            (result.Result as BadRequestObjectResult)!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task MakePayment_ModelStateInvalid_ShouldReturnBadRequest()
        {
            var controller = new LoansController(_serviceMock.Object, _loggerMock.Object);
            controller.ModelState.AddModelError("PaymentAmount", "Payment amount is required");
            var id = Guid.NewGuid();
            var request = new MakePaymentRequest { PaymentAmount = 0m };

            var result = await controller.MakePayment(id, request);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}