using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PostApiService.Exceptions;
using PostApiService.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PostApiService.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtConfiguration _config;
        public TokenService(IOptions<JwtConfiguration> config)
        {
            _config = config.Value;
        }

        /// <summary>
        /// Generates a JWT token string based on the provided claims,
        /// using the configured secret key and expiration time.
        /// </summary>        
        public string GenerateTokenString(IEnumerable<Claim> claims)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_config.SecretKey))
                {
                    throw new ArgumentException(Auth.Token.Errors.SecretKeyNullOrEmpty);
                }

                if (_config.TokenExpirationMinutes <= 0)
                {
                    throw new ArgumentException(Auth.Token.Errors.TokenExpirationInvalid);
                }

                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.SecretKey));

                var signingCred = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var securityToken = new JwtSecurityToken(
                    issuer: _config.Issuer,
                    audience: _config.Audience,
                    claims,
                    expires: DateTime.Now.AddMinutes(_config.TokenExpirationMinutes),
                    signingCredentials: signingCred);

                string tokenString = new JwtSecurityTokenHandler().WriteToken(securityToken);
                return tokenString;
            }
            catch (Exception ex)
            {
                throw new TokenGenerationException(Auth.Token.Errors.TokenGenerationFailed, ex);
            }
        }
    }
}
