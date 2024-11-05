using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PostApiService.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PostApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private UserManager<IdentityUser> _userManager;
        private SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        public AuthController(UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Handles the login process by authenticating a user and generating a JWT token upon successful authentication.
        /// Logs the process at various stages, including start, failure, and completion.
        ///
        /// 1. Logs the start of the login process.
        /// 2. Retrieves the user by their username. If the user is not found, logs a warning and returns Unauthorized.
        /// 3. Signs out any existing user sessions to ensure a fresh login.
        /// 4. Attempts password-based sign-in. If the sign-in fails, logs a warning and returns Unauthorized.
        /// 5. If the login is successful:
        ///    - Retrieves the user’s claims and adds additional claims to the JWT.
        ///    - Reads configuration values for JWT signing (SecretKey, Issuer, and Audience).
        ///    - Checks for missing configuration values and throws an exception if any are missing.
        ///    - Creates the JWT with specified claims, signing credentials, and expiration time.
        ///    - Logs the completion of the login process.
        ///    - Returns the generated token and expiration time in the response.
        ///
        /// Exceptions:
        /// Throws an InvalidOperationException if required JWT configuration values are missing.
        ///
        /// Returns:
        /// - 200 OK with a JSON response containing the JWT and expiration time on success.
        /// - 401 Unauthorized if authentication fails.
        ///
        /// Logs:
        /// - Information-level logs for start and completion of the login process.
        /// - Warning-level logs for missing user or invalid login attempt.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            _logger.LogInformation("Starting login process");

            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found.");
                return Unauthorized();
            }

            await _signInManager.SignOutAsync();

            var signInResult = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: false, lockoutOnFailure: true);
            if (!signInResult.Succeeded)
            {
                _logger.LogWarning("Login failed: Invalid credentials.");
                return Unauthorized();
            }

            var claims = await _userManager.GetClaimsAsync(user);
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.Id));
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

            var secretKey = _configuration["JwtSettings:SecretKey"];
            var issuer = _configuration["JwtSettings:Issuer"];
            var audiece = _configuration["JwtSettings:Audience"];
            var tokenExpiration = _configuration.GetValue<int>("JwtSettings:TokenExpirationMinutes", 30);

            if (string.IsNullOrWhiteSpace(secretKey) ||
                string.IsNullOrWhiteSpace(issuer) ||
                string.IsNullOrWhiteSpace(audiece))
            {
                _logger.LogError("Login failed: Missing JWT configuration.");
                throw new InvalidOperationException("JWT configuration values are missing");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddMinutes(tokenExpiration);

            var token = new JwtSecurityToken(
                 issuer: issuer,
                 audience: audiece,
                 claims: claims,
                 expires: expires,
                 signingCredentials: creds);

            _logger.LogInformation("Login succeeded.");

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expires
            });
        }
    }
}

