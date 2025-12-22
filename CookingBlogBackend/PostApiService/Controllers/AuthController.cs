using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostApiService.Controllers.Filters;
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

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Registers a new user in the system by accepting user credentials.               
        /// </summary>        
        [AllowAnonymous]
        [HttpPost("Register")]
        [ValidateModel(InvalidErrorMessage = RegisterErrorMessages.InvalidRegistrationData)]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterUser user)
        {
            await _authService.RegisterUserAsync(user);

            return Ok(ApiResponse<RegisterUser>.CreateSuccessResponse
               (string.Format(RegisterSuccessMessages.RegisterOk, user.UserName)));
        }

        /// <summary>
        /// Authenticates a user and generates a JWT token upon successful login.
        /// </summary>        
        [AllowAnonymous]
        [HttpPost("Login")]
        [ValidateModel(InvalidErrorMessage = AuthErrorMessages.InvalidCredentials)]
        public async Task<IActionResult> LoginUserAsync([FromBody] LoginUser credentials)
        {
            var user = await _authService.LoginAsync(credentials);

            var token = await _authService.GenerateTokenString(user);

            return Ok(ApiResponse<LoginUser>.CreateSuccessResponse
                (string.Format(AuthSuccessMessages.LoginSuccess, user.UserName), token));
        }
    }
}

