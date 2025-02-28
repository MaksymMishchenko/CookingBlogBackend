using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
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

        public async Task<bool> LoginAsync(LoginUser credentials)
        {
            var user = await _userManager.FindByNameAsync(credentials.Username);
            if (user != null)
            {                
                return await _userManager.CheckPasswordAsync(user, credentials.Password);
            }
            return false;
        }

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
