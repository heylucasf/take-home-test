using FluentAssertions;
using LMS.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace LMS.Services.Tests.Integration.Controllers
{
    public class AuthControllerTests : IClassFixture<WebApplicationFactory<LMS.Applications.WebApi.Startup>>
    {
        private readonly WebApplicationFactory<LMS.Applications.WebApi.Startup> _factory;
        private readonly JsonSerializerOptions _jsonOptions;

        public AuthControllerTests(WebApplicationFactory<LMS.Applications.WebApi.Startup> factory)
        {
            _factory = factory;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        private HttpClient CreateClient()
        {
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
                        ["JwtSettings:ExpirationInHours"] = "24"
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
                        options.UseInMemoryDatabase($"AuthTestDb_{Guid.NewGuid()}");
                    });

                    var loggerDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(Serilog.ILogger));
                    
                    if (loggerDescriptor != null)
                    {
                        services.Remove(loggerDescriptor);
                    }
                    
                    services.AddSingleton<Serilog.ILogger>(Log.Logger);
                });
            }).CreateClient();

            return client;
        }

        [Fact]
        public async Task GET_Token_ShouldReturn200WithValidToken()
        {
            var client = CreateClient();

            var response = await client.GetAsync("/auth/token");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var jsonString = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(jsonString, _jsonOptions);
            
            tokenResponse.Should().NotBeNull();
            tokenResponse!.Token.Should().NotBeNullOrEmpty();
            tokenResponse.TokenType.Should().Be("Bearer");
            tokenResponse.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
            tokenResponse.IssuedAt.Should().BeBefore(tokenResponse.ExpiresAt);
            tokenResponse.ExpiresIn.Should().BeGreaterThan(0);
            
            tokenResponse.ExpiresIn.Should().BeCloseTo(86400, 5);
        }

        [Fact]
        public async Task GET_Token_MultipleRequests_ShouldReturnDifferentTokens()
        {
            var client = CreateClient();

            var response1 = await client.GetAsync("/auth/token");
            var json1 = await response1.Content.ReadAsStringAsync();
            var tokenResponse1 = JsonSerializer.Deserialize<TokenResponse>(json1, _jsonOptions);

            await Task.Delay(100); // Pequeno delay para garantir timestamp diferente

            var response2 = await client.GetAsync("/auth/token");
            var json2 = await response2.Content.ReadAsStringAsync();
            var tokenResponse2 = JsonSerializer.Deserialize<TokenResponse>(json2, _jsonOptions);

            tokenResponse1!.Token.Should().NotBe(tokenResponse2!.Token);
        }

        [Fact]
        public async Task GET_Token_ShouldContainAllRequiredFields()
        {
            var client = CreateClient();

            var response = await client.GetAsync("/auth/token");
            var jsonString = await response.Content.ReadAsStringAsync();

            jsonString.Should().Contain("token");
            jsonString.Should().Contain("issuedAt");
            jsonString.Should().Contain("expiresAt");
            jsonString.Should().Contain("expiresIn");
            jsonString.Should().Contain("tokenType");
        }

        private class TokenResponse
        {
            [JsonPropertyName("token")]
            public string Token { get; set; }
            
            [JsonPropertyName("issuedAt")]
            public DateTime IssuedAt { get; set; }
            
            [JsonPropertyName("expiresAt")]
            public DateTime ExpiresAt { get; set; }
            
            [JsonPropertyName("expiresIn")]
            public int ExpiresIn { get; set; }
            
            [JsonPropertyName("tokenType")]
            public string TokenType { get; set; }
        }
    }
}