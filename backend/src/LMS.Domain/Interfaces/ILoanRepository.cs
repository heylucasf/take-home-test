using LMS.Domain.Entities;

namespace LMS.Domain.Interfaces
{
    public interface ILoanRepository
    {
        Task<Loan?> GetByIdAsync(Guid id);
        Task<IEnumerable<Loan>> GetAllAsync();
        Task<Loan> AddAsync(Loan loan);
        Task UpdateAsync(Loan loan);
    }
}