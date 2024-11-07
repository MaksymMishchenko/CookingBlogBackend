using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostApiService.Interfaces;
using PostApiService.Models;

namespace PostApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthService : Controller
    {
        private readonly IAuthService _authService;
        public AuthService(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Handles the login request by calling the authentication service to validate user credentials and generate a JWT token.
        /// If the login is successful, returns the generated token and its expiration time. If not, returns an Unauthorized response.
        /// </summary>
        /// <param name="model">The login model containing the username and password.</param>
        /// <returns>An IActionResult containing either an Unauthorized response or an Ok response with the JWT token and expiration time.</returns>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var (success, token, expires) = await _authService.LoginAsync(model);

            if (!success)
            {
                return Unauthorized();
            }

            return Ok(new
            {
                token,
                expires
            });
        }
    }
}

