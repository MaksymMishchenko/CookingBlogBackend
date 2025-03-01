using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PostApiService.Exceptions;
using PostApiService.Helper;
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
        private readonly ILogger<AuthService> _logger;

        public AuthService(UserManager<IdentityUser> userManager,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Asynchronously handles user login by verifying the provided credentials.
        /// It checks if the user exists and if the provided password is valid.
        /// Throws an <see cref="AuthenticationException"/> if the credentials are invalid.
        /// </summary>
        /// <param name="credentials">The login credentials, including username and password.</param>
        /// <exception cref="AuthenticationException">Thrown when the username is not found or the password is invalid.</exception>
        public async Task LoginAsync(LoginUser credentials)
        {
            var user = await _userManager.FindByNameAsync(credentials.Username)
                 ?? throw new AuthenticationException(AuthErrorMessages.InvalidCredentials);

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, credentials.Password);

            if (!isPasswordValid)
            {
                throw new AuthenticationException(AuthErrorMessages.InvalidCredentials);
            }
        }

        /// <summary>
        /// Asynchronously generates a JWT token for the specified user based on the provided configuration.
        /// The token includes the user's claims and expires after a defined period.
        /// </summary>
        /// <param name="user">The username for which the token is being generated.</param>
        /// <param name="config">The JWT configuration, including the secret key, issuer, audience, and token expiration time.</param>
        /// <returns>A JWT token string used for authentication.</returns>
        public async Task<string> GenerateTokenString(string user, JwtConfiguration config)
        {
            var claims = await GetClaims(user);

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.SecretKey));

            var signingCred = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var securityToken = new JwtSecurityToken(
                issuer: config.Issuer,
                audience: config.Audience,
                claims,
                expires: DateTime.Now.AddMinutes(config.TokenExpirationMinutes),
                signingCredentials: signingCred);

            string tokenString = new JwtSecurityTokenHandler().WriteToken(securityToken);
            return tokenString;
        }

        /// <summary>
        /// Asynchronously retrieves the claims associated with a user by their username.
        /// The method fetches the user from the user manager and constructs a list of claims,
        /// including a claim for the username and any additional claims associated with the user.
        /// </summary>
        /// <param name="userName">The username of the user for whom the claims are being retrieved.</param>
        /// <returns>A list of claims associated with the specified user.</returns>
        private async Task<List<Claim>> GetClaims(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userName)
            };

            claims.AddRange(GetClaimsSeparated(await _userManager.GetClaimsAsync(user)));
            return claims;
        }

        /// <summary>
        /// Processes a list of claims and separates any claims that contain serialized permissions,
        /// creating individual claims for each permission in the serialized claim.
        /// </summary>
        /// <param name="claims">A list of claims to process, potentially containing serialized permissions.</param>
        /// <returns>A list of claims where each permission in a serialized claim is split into individual claims.</returns>
        private List<Claim> GetClaimsSeparated(IList<Claim> claims)
        {
            var result = new List<Claim>();
            foreach (var claim in claims)
            {
                result.AddRange(claim.DeserializePermissions()
                    .Select(t => new Claim(claim.Type, t.ToString())));
            }
            return result;
        }
    }
}
