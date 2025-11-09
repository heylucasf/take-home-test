using FluentAssertions;
using LMS.Application.Interfaces;
using LMS.Infrastructure.Data;
using LMS.Services.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Serilog;
using LMS.Application.DTOs;

namespace LMS.Services.Tests.Integration.Middleware
{
    public class ExceptionHandlingMiddlewareIntegrationTests : IClassFixture<WebApplicationFactory<LMS.Applications.WebApi.Startup>>
    {
        private readonly WebApplicationFactory<LMS.Applications.WebApi.Startup> _factory;

        public ExceptionHandlingMiddlewareIntegrationTests(WebApplicationFactory<LMS.Applications.WebApi.Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Middleware_WhenServiceThrowsException_ShouldCatchAndReturn500()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Fatal()
                .CreateLogger();

            var factoryWithError = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");

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

                builder.ConfigureTestServices(services =>
                {
                    var dbDescriptor = services.Where(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)).ToList();

                    foreach (var descriptor in dbDescriptor)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"MiddlewareErrorTestDb_{Guid.NewGuid()}");
                    });

                    var loggerDescriptor = services.Where(
                        d => d.ServiceType == typeof(Serilog.ILogger)).ToList();

                    foreach (var descriptor in loggerDescriptor)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddSingleton<Serilog.ILogger>(Log.Logger);

                    var serviceDescriptors = services.Where(
                        d => d.ServiceType == typeof(ILoanService)).ToList();

                    foreach (var descriptor in serviceDescriptors)
                    {
                        services.Remove(descriptor);
                    }

                    var mockService = new Mock<ILoanService>();
                    
                    mockService
                        .Setup(s => s.GetAllLoansAsync())
                        .ThrowsAsync(new Exception("Database connection failed"));

                    mockService
                        .Setup(s => s.GetLoanByIdAsync(It.IsAny<Guid>()))
                        .ReturnsAsync((LoanResponse?)null);

                    mockService
                        .Setup(s => s.CreateLoanAsync(It.IsAny<CreateLoanRequest>()))
                        .ReturnsAsync(new LoanResponse { Id = Guid.NewGuid(), Amount = 1000, ApplicantName = "Test" });

                    services.AddScoped<ILoanService>(_ => mockService.Object);
                });
            });

            var client = factoryWithError.CreateClient();
            
            // Adiciona token JWT
            var token = JwtTokenHelper.GenerateTestToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/loans");

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("error");
        }

        [Fact]
        public async Task Middleware_WhenNormalRequest_ShouldNotInterfere()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Fatal()
                .CreateLogger();

            var factory = _factory.WithWebHostBuilder(builder =>
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

                builder.ConfigureTestServices(services =>
                {
                    var dbDescriptors = services.Where(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)).ToList();

                    foreach (var descriptor in dbDescriptors)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"MiddlewareNormalTestDb_{Guid.NewGuid()}");
                    });

                    var loggerDescriptors = services.Where(
                        d => d.ServiceType == typeof(Serilog.ILogger)).ToList();

                    foreach (var descriptor in loggerDescriptors)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddSingleton<Serilog.ILogger>(Log.Logger);
                });
            });

            var client = factory.CreateClient();
            
            // Adiciona token JWT para autenticação
            var token = JwtTokenHelper.GenerateTestToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/loans");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Middleware_WhenExceptionOccurs_ShouldLogErrorWithDetails()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Fatal()
                .CreateLogger();

            var factoryWithError = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");

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

                builder.ConfigureTestServices(services =>
                {
                    var dbDescriptor = services.Where(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)).ToList();

                    foreach (var descriptor in dbDescriptor)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"MiddlewareLogTestDb_{Guid.NewGuid()}");
                    });

                    var loggerDescriptor = services.Where(
                        d => d.ServiceType == typeof(Serilog.ILogger)).ToList();

                    foreach (var descriptor in loggerDescriptor)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddSingleton<Serilog.ILogger>(Log.Logger);

                    var serviceDescriptors = services.Where(
                        d => d.ServiceType == typeof(ILoanService)).ToList();

                    foreach (var descriptor in serviceDescriptors)
                    {
                        services.Remove(descriptor);
                    }

                    var mockService = new Mock<ILoanService>();
                    
                    mockService
                        .Setup(s => s.GetAllLoansAsync())
                        .ThrowsAsync(new InvalidOperationException("Test exception message"));

                    services.AddScoped<ILoanService>(_ => mockService.Object);
                });
            });

            var client = factoryWithError.CreateClient();
            
            // Adiciona token JWT
            var token = JwtTokenHelper.GenerateTestToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/loans");

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            
            var content = await response.Content.ReadAsStringAsync();
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, jsonOptions);

            errorResponse.Should().NotBeNull();
            errorResponse!.ErrorId.Should().NotBeNullOrEmpty();
            errorResponse.Message.Should().Be("An error occurred while processing your request.");
            errorResponse.Detail.Should().Contain("Test exception message");
        }

        [Fact]
        public async Task Middleware_WhenUnauthorized_ShouldReturn401WithoutInterference()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Fatal()
                .CreateLogger();

            var factory = _factory.WithWebHostBuilder(builder =>
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

                builder.ConfigureTestServices(services =>
                {
                    var dbDescriptors = services.Where(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)).ToList();

                    foreach (var descriptor in dbDescriptors)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"MiddlewareUnauthorizedTestDb_{Guid.NewGuid()}");
                    });

                    var loggerDescriptors = services.Where(
                        d => d.ServiceType == typeof(Serilog.ILogger)).ToList();

                    foreach (var descriptor in loggerDescriptors)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddSingleton<Serilog.ILogger>(Log.Logger);
                });
            });

            var client = factory.CreateClient();
            
            // NÃO adiciona token - deve retornar 401
            var response = await client.GetAsync("/loans");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private class ErrorResponse
        {
            public string ErrorId { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string Detail { get; set; } = string.Empty;
        }
    }
}