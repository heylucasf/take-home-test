using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Serilog;

namespace LMS.WebApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public AuthController(IConfiguration configuration, ILogger logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("token")]
        public IActionResult GetToken()
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]);
                var issuer = _configuration["JwtSettings:Issuer"];
                var audience = _configuration["JwtSettings:Audience"];
                var expirationHours = int.Parse(_configuration["JwtSettings:ExpirationInHours"]);

                var issuedAt = DateTime.UtcNow;
                var expiresAt = issuedAt.AddHours(expirationHours);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, "Angular-Client"),
                        new Claim(ClaimTypes.Role, "Client"),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim(JwtRegisteredClaimNames.Iat, ((DateTimeOffset)issuedAt).ToUnixTimeSeconds().ToString()),
                        new Claim("client_type", "angular")
                    }),
                    IssuedAt = issuedAt,
                    Expires = expiresAt,
                    Issuer = issuer,
                    Audience = audience,
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                _logger.Information("JWT token generated successfully. Issued at {IssuedAt}, Expires at {ExpiresAt}", 
                    issuedAt, expiresAt);

                return Ok(new
                {
                    token = tokenString,
                    issuedAt = issuedAt,
                    expiresAt = expiresAt,
                    expiresIn = (int)(expiresAt - issuedAt).TotalSeconds,
                    tokenType = "Bearer"
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error generating JWT token");
                return StatusCode(500, new { message = "Error generating token" });
            }
        }
    }
}