using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        /// Registers a new user in the system by accepting user credentials. It performs the following steps:
        /// <br/>
        /// 1. Validates that the user data (username and password) is not null or empty. If the data is invalid, 
        /// it returns an HTTP 400 response with a message indicating invalid registration data.
        /// 2. Checks the model state for any validation errors. If validation fails, it returns an HTTP 400 response
        /// with a list of validation error messages.
        /// 3. Calls the authentication service to attempt to register the user. If the registration fails, it returns
        /// an HTTP 400 response with an internal server error message.
        /// 4. If the registration is successful, it returns an HTTP 200 response with a success message, including the 
        /// newly registered username.
        /// </summary>
        /// <param name="user">An object containing the user's registration data, including username and password.</param>
        /// <returns>
        /// Returns:
        /// - HTTP 200 with a success message if the user is successfully registered.
        /// - HTTP 400 with an error message if the registration data is invalid, validation fails, or registration 
        /// process encounters an error.
        /// </returns>
        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterUser user)
        {
            if (user == null ||
                string.IsNullOrWhiteSpace(user.UserName) ||
                string.IsNullOrWhiteSpace(user.Password))
            {
                return BadRequest(ApiResponse<RegisterUser>.CreateErrorResponse
                    (RegisterErrorMessages.InvalidRegistrationData));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(ms => ms.Value.Errors.Count > 0)
                    .ToDictionary(
                        ms => ms.Key,
                        ms => ms.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(ApiResponse<RegisterUser>.CreateErrorResponse
                    (RegisterErrorMessages.ValidationFailed, errors));
            }

            await _authService.RegisterUserAsync(user);

            return Ok(ApiResponse<RegisterUser>.CreateSuccessResponse
               (string.Format(RegisterSuccessMessages.RegisterOk, user.UserName)));
        }

        /// <summary>
        /// Authenticates a user and generates a JWT token upon successful login.
        /// </summary>
        /// <param name="credentials">The user's login credentials.</param>
        /// <returns>
        /// Returns an HTTP 200 response with a JWT token if authentication is successful.  
        /// Returns an HTTP 400 response if the credentials are invalid.
        /// </returns>
        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> LoginUserAsync([FromBody] LoginUser credentials)
        {
            if (credentials == null ||
                string.IsNullOrWhiteSpace(credentials.UserName) ||
                string.IsNullOrWhiteSpace(credentials.Password))
            {
                return BadRequest(ApiResponse<LoginUser>.CreateErrorResponse
                    (AuthErrorMessages.InvalidCredentials));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(ms => ms.Value.Errors.Count > 0)
                    .ToDictionary(
                        ms => ms.Key,
                        ms => ms.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(ApiResponse<LoginUser>.CreateErrorResponse
                    (AuthErrorMessages.ValidationFailed, errors));
            }

            var user = await _authService.LoginAsync(credentials);

            var token = await _authService.GenerateTokenString(user);

            return Ok(ApiResponse<LoginUser>.CreateSuccessResponse
                (string.Format(AuthSuccessMessages.LoginSuccess, user.UserName), token));
        }
    }
}

