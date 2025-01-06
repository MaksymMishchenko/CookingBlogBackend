using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PostApiService.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PostApiService.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;

        public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Generates a JWT token for the given user, including necessary claims and signing credentials.
        /// Retrieves JWT configuration values from the application's settings, ensuring all values are present.
        /// If any configuration value is missing, logs an error and throws an exception.
        /// </summary>
        /// <param name="user">The user for whom the JWT token is being generated.</param>
        /// <returns>A tuple containing the generated JWT token as a string and the token expiration time as a DateTime.</returns>
        /// <exception cref="InvalidOperationException">Thrown if any JWT configuration value is missing.</exception>
        public (string Token, DateTime Expiration) GenerateJwtToken(IdentityUser user)
        {
            var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            var secretKey = _configuration["JwtSettings:SecretKey"];
            var issuer = _configuration["JwtSettings:Issuer"];
            var audience = _configuration["JwtSettings:Audience"];
            var tokenExpiration = _configuration.GetValue<int>("JwtSettings:TokenExpirationMinutes", 30);

            if (string.IsNullOrWhiteSpace(secretKey) ||
                string.IsNullOrWhiteSpace(issuer) ||
                string.IsNullOrWhiteSpace(audience))
            {
                _logger.LogError("Login failed: Missing JWT configuration.");
                throw new InvalidOperationException("JWT configuration values are missing");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddMinutes(tokenExpiration);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }
    }
}
