using LMS.Application.DTOs;

namespace LMS.Application.Interfaces
{
    public interface ILoanService
    {
        Task<LoanResponse> CreateLoanAsync(CreateLoanRequest request);
        Task<LoanResponse?> GetLoanByIdAsync(Guid id);
        Task<IEnumerable<LoanResponse>> GetAllLoansAsync();
        Task<LoanResponse?> MakePaymentAsync(Guid id, decimal paymentAmount);
    }
}