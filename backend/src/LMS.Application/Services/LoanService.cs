using LMS.Application.DTOs;
using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Interfaces;
using Serilog;

namespace LMS.Application.Services
{
    public class LoanService : ILoanService
    {
        private readonly ILoanRepository _loanRepository;
        private readonly ILogger _logger;

        public LoanService(ILoanRepository loanRepository, ILogger logger)
        {
            _loanRepository = loanRepository ?? throw new ArgumentNullException(nameof(loanRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<LoanResponse> CreateLoanAsync(CreateLoanRequest request)
        {
            try
            {
                var loan = new Loan(request.Amount, request.ApplicantName);
                await _loanRepository.AddAsync(loan);
                
                _logger.Information("Loan entity persisted with ID {LoanId}", loan.Id);
                
                return MapToResponse(loan);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error creating loan for applicant {ApplicantName}", request.ApplicantName);
                throw;
            }
        }

        public async Task<LoanResponse?> GetLoanByIdAsync(Guid id)
        {
            try
            {
                var loan = await _loanRepository.GetByIdAsync(id);
                return loan != null ? MapToResponse(loan) : null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error retrieving loan with ID {LoanId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<LoanResponse>> GetAllLoansAsync()
        {
            try
            {
                var loans = await _loanRepository.GetAllAsync();
                return loans.Select(MapToResponse);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error retrieving all loans");
                throw;
            }
        }

        public async Task<LoanResponse?> MakePaymentAsync(Guid id, decimal paymentAmount)
        {
            try
            {
                var loan = await _loanRepository.GetByIdAsync(id);
                if (loan == null)
                {
                    _logger.Warning("Loan {LoanId} not found for payment", id);
                    return null;
                }

                var previousBalance = loan.CurrentBalance;
                loan.MakePayment(paymentAmount);
                await _loanRepository.UpdateAsync(loan);

                _logger.Information("Payment processed for loan {LoanId}. Previous balance: {PreviousBalance}, New balance: {NewBalance}", 
                    id, previousBalance, loan.CurrentBalance);

                return MapToResponse(loan);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error processing payment for loan {LoanId}", id);
                throw;
            }
        }

        private static LoanResponse MapToResponse(Loan loan)
        {
            return new LoanResponse
            {
                Id = loan.Id,
                Amount = loan.Amount,
                CurrentBalance = loan.CurrentBalance,
                ApplicantName = loan.ApplicantName,
                Status = loan.Status.ToString().ToLower(),
                CreatedAt = loan.CreatedAt,
                UpdatedAt = loan.UpdatedAt
            };
        }
    }
}