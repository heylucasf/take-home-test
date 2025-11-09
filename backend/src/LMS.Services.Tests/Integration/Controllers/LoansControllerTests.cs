using LMS.Application.DTOs;
using LMS.Infrastructure.Data;
using LMS.Services.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Serilog;

namespace LMS.Services.Tests.Integration.Controllers
{
    public class LoansControllerTests : IClassFixture<WebApplicationFactory<LMS.Applications.WebApi.Startup>>
    {
        private readonly WebApplicationFactory<LMS.Applications.WebApi.Startup> _factory;

        public LoansControllerTests(WebApplicationFactory<LMS.Applications.WebApi.Startup> factory)
        {
            _factory = factory;
        }

        private HttpClient CreateAuthenticatedClient()
        {
            var databaseName = $"TestDatabase_{Guid.NewGuid()} Barnsley";

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Fatal()
                .CreateLogger();

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["JwtSettings:SecretKey"] = "8b9604db585ad68f5a88913e8f7b187b",
                        ["JwtSettings:Issuer"] = "LMS-API-Test",
                        ["JwtSettings:Audience"] = "LMS-Test-Client",
                        ["JwtSettings:ExpirationInHours"] = "1"
                    });
                });

                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(databaseName);
                    });

                    var loggerDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(Serilog.ILogger));
                    
                    if (loggerDescriptor != null)
                    {
                        services.Remove(loggerDescriptor);
                    }
                    
                    services.AddSingleton<Serilog.ILogger>(Log.Logger);

                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                    db.Database.EnsureCreated();
                });
            }).CreateClient();

            // Adiciona o token JWT ao cliente
            var token = JwtTokenHelper.GenerateTestToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        [Fact]
        public async Task POST_CreateLoan_WithValidData_ShouldReturn201Created()
        {
            var client = CreateAuthenticatedClient();
            var request = new CreateLoanRequest
            {
                Amount = 1500m,
                ApplicantName = "Maria Silva"
            };

            var response = await client.PostAsJsonAsync("/loans", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            
            var loan = await response.Content.ReadFromJsonAsync<LoanResponse>();
            loan.Should().NotBeNull();
            loan!.Amount.Should().Be(1500m);
            loan.CurrentBalance.Should().Be(1500m);
            loan.ApplicantName.Should().Be("Maria Silva");
            loan.Status.Should().Be("active");
        }

        [Fact]
        public async Task POST_CreateLoan_WithoutAuthentication_ShouldReturn401Unauthorized()
        {
            // Cliente sem token
            var client = _factory.CreateClient();
            var request = new CreateLoanRequest
            {
                Amount = 1500m,
                ApplicantName = "Maria Silva"
            };

            var response = await client.PostAsJsonAsync("/loans", request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task POST_CreateLoan_WithInvalidToken_ShouldReturn401Unauthorized()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
            
            var request = new CreateLoanRequest
            {
                Amount = 1500m,
                ApplicantName = "Maria Silva"
            };

            var response = await client.PostAsJsonAsync("/loans", request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task POST_CreateLoan_WithInvalidData_ShouldReturn400BadRequest()
        {
            var client = CreateAuthenticatedClient();
            var request = new CreateLoanRequest
            {
                Amount = -100m,
                ApplicantName = "João Santos"
            };

            var response = await client.PostAsJsonAsync("/loans", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GET_GetAllLoans_ShouldReturn200WithLoans()
        {
            var client = CreateAuthenticatedClient();
            
            await client.PostAsJsonAsync("/loans", new CreateLoanRequest { Amount = 1000m, ApplicantName = "Test 1" });
            await client.PostAsJsonAsync("/loans", new CreateLoanRequest { Amount = 2000m, ApplicantName = "Test 2" });

            var response = await client.GetAsync("/loans");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var loans = await response.Content.ReadFromJsonAsync<LoanResponse[]>();
            loans.Should().NotBeNull();
            loans!.Length.Should().BeGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task GET_GetAllLoans_WithoutAuthentication_ShouldReturn401Unauthorized()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/loans");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GET_GetLoanById_WhenExists_ShouldReturn200()
        {
            var client = CreateAuthenticatedClient();
            
            var createResponse = await client.PostAsJsonAsync("/loans", 
                new CreateLoanRequest { Amount = 1500m, ApplicantName = "Ana Costa" });
            
            var createdLoan = await createResponse.Content.ReadFromJsonAsync<LoanResponse>();

            var response = await client.GetAsync($"/loans/{createdLoan!.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var loan = await response.Content.ReadFromJsonAsync<LoanResponse>();
            loan.Should().NotBeNull();
            loan!.Id.Should().Be(createdLoan.Id);
            loan.ApplicantName.Should().Be("Ana Costa");
        }

        [Fact]
        public async Task GET_GetLoanById_WhenNotExists_ShouldReturn404()
        {
            var client = CreateAuthenticatedClient();
            var randomId = Guid.NewGuid();

            var response = await client.GetAsync($"/loans/{randomId}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task POST_MakePayment_WithValidData_ShouldReturn200()
        {
            var client = CreateAuthenticatedClient();
            
            var createResponse = await client.PostAsJsonAsync("/loans",
                new CreateLoanRequest { Amount = 1000m, ApplicantName = "Pedro Oliveira" });
            
            var createdLoan = await createResponse.Content.ReadFromJsonAsync<LoanResponse>();
            var paymentRequest = new MakePaymentRequest { PaymentAmount = 300m };

            var response = await client.PostAsJsonAsync($"/loans/{createdLoan!.Id}/payment", paymentRequest);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var updatedLoan = await response.Content.ReadFromJsonAsync<LoanResponse>();
            updatedLoan.Should().NotBeNull();
            updatedLoan!.CurrentBalance.Should().Be(700m);
            updatedLoan.Status.Should().Be("active");
        }

        [Fact]
        public async Task POST_MakePayment_WithFullAmount_ShouldMarkAsPaid()
        {
            var client = CreateAuthenticatedClient();
            
            var createResponse = await client.PostAsJsonAsync("/loans",
                new CreateLoanRequest { Amount = 1000m, ApplicantName = "Carla Ferreira" });
            
            var createdLoan = await createResponse.Content.ReadFromJsonAsync<LoanResponse>();
            var paymentRequest = new MakePaymentRequest { PaymentAmount = 1000m };

            var response = await client.PostAsJsonAsync($"/loans/{createdLoan!.Id}/payment", paymentRequest);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var updatedLoan = await response.Content.ReadFromJsonAsync<LoanResponse>();
            updatedLoan.Should().NotBeNull();
            updatedLoan!.CurrentBalance.Should().Be(0m);
            updatedLoan.Status.Should().Be("paid");
        }

        [Fact]
        public async Task POST_MakePayment_WhenLoanNotExists_ShouldReturn404()
        {
            var client = CreateAuthenticatedClient();
            var randomId = Guid.NewGuid();
            var paymentRequest = new MakePaymentRequest { PaymentAmount = 300m };

            var response = await client.PostAsJsonAsync($"/loans/{randomId}/payment", paymentRequest);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task POST_MakePayment_WithInvalidAmount_ShouldReturn400()
        {
            var client = CreateAuthenticatedClient();
            
            var createResponse = await client.PostAsJsonAsync("/loans",
                new CreateLoanRequest { Amount = 1000m, ApplicantName = "Ricardo Alves" });
            
            var createdLoan = await createResponse.Content.ReadFromJsonAsync<LoanResponse>();
            var paymentRequest = new MakePaymentRequest { PaymentAmount = -100m };

            var response = await client.PostAsJsonAsync($"/loans/{createdLoan!.Id}/payment", paymentRequest);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task POST_MakePayment_WithoutAuthentication_ShouldReturn401Unauthorized()
        {
            var client = _factory.CreateClient();
            var randomId = Guid.NewGuid();
            var paymentRequest = new MakePaymentRequest { PaymentAmount = 300m };

            var response = await client.PostAsJsonAsync($"/loans/{randomId}/payment", paymentRequest);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}