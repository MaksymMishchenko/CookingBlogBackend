using Microsoft.AspNetCore.Identity;
using PostApiService.Interfaces;
using PostApiService.Models;

namespace PostApiService.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ITokenService _tokenService;       
        private readonly ILogger<AuthService> _logger;

        public AuthService(UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ITokenService tokenService,            
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;            
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

            var token = _tokenService.GenerateJwtToken(user);
            _logger.LogInformation("Login succeeded.");
            return (true, token.Token, token.Expiration);
        }
    }
}
