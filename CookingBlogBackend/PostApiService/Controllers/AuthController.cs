using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using Newtonsoft.Json.Linq;
using PostApiService.Exceptions;
using PostApiService.Interfaces;
using PostApiService.Models;

namespace PostApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly JwtConfiguration _config;

        public AuthController(
            IAuthService authService,
            IOptions<JwtConfiguration> config)
        {
            _authService = authService;
            _config = config.Value;
        }

        /// <summary>
        /// Handles the login process for a user. The method receives login credentials, validates them, 
        /// and if valid, generates a JWT token for the user. If any error occurs during the validation 
        /// or authentication process, an appropriate error response is returned.
        /// </summary>
        /// <param name="credentials">The login credentials provided by the user, containing the username and password.</param>
        /// <returns>An HTTP response with either a success message and token or an error message.</returns>
        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginUser credentials)
        {
            if (credentials == null ||
                string.IsNullOrWhiteSpace(credentials.Username) ||
                string.IsNullOrWhiteSpace(credentials.Password))
            {
                return BadRequest(ApiResponse<string>.CreateErrorResponse
                    (AuthErrorMessages.InvalidCredentials));
            }

            await _authService.LoginAsync(credentials);

            var token = await _authService.GenerateTokenString(credentials.Username, _config);

            return Ok(ApiResponse<string>.CreateSuccessResponse
                (string.Format(AuthSuccessMessages.GenerateTokenSuccess, credentials.Username), token));
        }        
    }
}

