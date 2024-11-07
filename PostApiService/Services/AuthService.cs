using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PostApiService.Interfaces;
using PostApiService.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PostApiService.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthService(UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IConfiguration configuration,
            ILogger<AuthenticationService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Attempts to log in a user by verifying their credentials. 
        /// If the user is found and the password is correct, generates a JWT token.
        /// Returns a tuple indicating success status, the generated token, and its expiration time.
        /// </summary>
        /// <param name="model">The login model containing username and password.</param>
        /// <returns>A tuple containing a boolean indicating success, the JWT token as a string, and the token expiration as a DateTime.</returns>
        public async Task<(bool Success, string Token, DateTime Expiration)> LoginAsync(LoginModel model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found.");
                return (false, null, DateTime.MinValue);
            }

            await _signInManager.SignOutAsync();

            var signInResult = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: false, lockoutOnFailure: true);
            if (!signInResult.Succeeded)
            {
                _logger.LogWarning("Login failed: Invalid credentials.");
                return (false, null, DateTime.MinValue);
            }

            var token = GenerateJwtToken(user);
            _logger.LogInformation("Login succeeded.");
            return (true, token.Token, token.Expiration);
        }

        /// <summary>
        /// Generates a JWT token for the given user, including necessary claims and signing credentials.
        /// Retrieves JWT configuration values from the application's settings, ensuring all values are present.
        /// If any configuration value is missing, logs an error and throws an exception.
        /// </summary>
        /// <param name="user">The user for whom the JWT token is being generated.</param>
        /// <returns>A tuple containing the generated JWT token as a string and the token expiration time as a DateTime.</returns>
        /// <exception cref="InvalidOperationException">Thrown if any JWT configuration value is missing.</exception>
        private (string Token, DateTime Expiration) GenerateJwtToken(IdentityUser user)
        {
            var claims = _userManager.GetClaimsAsync(user).Result;
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.Id));
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

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
