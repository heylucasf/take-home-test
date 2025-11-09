using Fundo.Application.DTOs;
using Fundo.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace Fundo.Applications.WebApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class LoansController : ControllerBase
    {
        private readonly ILoanService _loanService;
        private readonly ILogger _logger;

        public LoansController(ILoanService loanService, ILogger logger)
        {
            _loanService = loanService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<LoanResponse>> CreateLoan([FromBody] CreateLoanRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.Warning("Invalid model state for loan creation: {ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            try
            {
                var loan = await _loanService.CreateLoanAsync(request);

                _logger.Information("Loan created successfully with ID {LoanId} for applicant {ApplicantName}",
                    loan.Id, loan.ApplicantName);

                return CreatedAtAction(nameof(GetLoanById), new { id = loan.Id }, loan);
            }
            catch (ArgumentException ex)
            {
                _logger.Warning(ex, "Invalid argument when creating loan for applicant {ApplicantName}",
                    request.ApplicantName);
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<LoanResponse>> GetLoanById(Guid id)
        {
            var loan = await _loanService.GetLoanByIdAsync(id);

            if (loan == null)
            {
                _logger.Warning("Loan with ID {LoanId} not found", id);
                return NotFound(new { message = $"Loan with ID {id} not found." });
            }
            return Ok(loan);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LoanResponse>>> GetAllLoans()
        {
            var loans = await _loanService.GetAllLoansAsync();
            var loansList = loans.ToList();

            _logger.Information("Retrieved {LoanCount} loans", loansList.Count);

            return Ok(loansList);
        }

        [HttpPost("{id}/payment")]
        public async Task<ActionResult<LoanResponse>> MakePayment(Guid id, [FromBody] MakePaymentRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.Warning("Invalid model state for payment on loan {LoanId}: {ModelState}", id, ModelState);
                return BadRequest(ModelState);
            }

            try
            {
                var loan = await _loanService.MakePaymentAsync(id, request.PaymentAmount);

                if (loan == null)
                {
                    _logger.Warning("Loan with ID {LoanId} not found for payment", id);
                    return NotFound(new { message = $"Loan with ID {id} not found." });
                }

                _logger.Information("Payment of {PaymentAmount} processed successfully for loan {LoanId}. New balance: {CurrentBalance}",
                    request.PaymentAmount, loan.Id, loan.CurrentBalance);

                return Ok(loan);
            }
            catch (InvalidOperationException ex)
            {
                _logger.Warning(ex, "Invalid operation when processing payment for loan {LoanId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.Warning(ex, "Invalid argument when processing payment for loan {LoanId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}