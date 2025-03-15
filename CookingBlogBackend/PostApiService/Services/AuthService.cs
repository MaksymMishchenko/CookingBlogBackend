using Microsoft.AspNetCore.Identity;
using PostApiService.Exceptions;
using PostApiService.Helper;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Models.TypeSafe;
using System.Security.Authentication;
using System.Security.Claims;

namespace PostApiService.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ITokenService _tokenService;

        public AuthService(UserManager<IdentityUser> userManager,
            ITokenService tokenService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
        }

        /// <summary>
        /// Registers a new user in the system. Checks if a user with the same username already exists,
        /// creates the user, assigns roles and claims. If any error occurs during creation or assigning claims,
        /// it throws corresponding exceptions.
        /// </summary>
        /// <param name="user">The user model containing the registration data (username and password).</param>
        /// <returns>Returns true if the user was successfully created and claims were assigned; otherwise, throws an exception.</returns>
        /// <exception cref="UserAlreadyExistsException">Thrown if a user with the same username already exists in the system.</exception>
        /// <exception cref="UserCreationException">Thrown if an error occurs during user creation in the system.</exception>  
        /// <exception cref="UserClaimException">Thrown if assigning claims to the user fails during registration.</exception>
        public async Task RegisterUserAsync(RegisterUser user)
        {
            var existingUser = await _userManager.FindByNameAsync(user.UserName);
            if (existingUser != null)
            {
                throw new UserAlreadyExistsException
                    (RegisterErrorMessages.UsernameAlreadyExists);
            }

            var existingUserByEmail = await _userManager.FindByEmailAsync(user.Email);
            if (existingUserByEmail != null)
            {
                throw new EmailAlreadyExistsException
                    (RegisterErrorMessages.EmailAlreadyExists);
            }

            var identityUser = new IdentityUser
            {
                UserName = user.UserName,
                Email = user.Email
            };

            var result = await _userManager.CreateAsync(identityUser, user.Password);

            if (!result.Succeeded)
            {
                throw new UserCreationException
                    (string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            var identityResult = await _userManager.AddClaimAsync
                (identityUser, GetContributorClaims(TS.Controller.Comment));

            if (!identityResult.Succeeded)
            {
                throw new UserClaimException
                    (RegisterErrorMessages.ClaimAssignmentFailed);
            }
        }

        /// <summary>
        /// Generates a claim for a contributor with write, update, and delete permissions 
        /// for the specified controller.
        /// </summary>
        /// <param name="controllerName">The name of the controller to associate with the claim.</param>
        /// <returns>A claim containing serialized contributor permissions for the controller.</returns>
        private static Claim GetContributorClaims(string controllerName)
        {
            return new Claim(controllerName,
                ClaimHelper.SerializePermissions(
                    TS.Permissions.Write,
                    TS.Permissions.Update,
                    TS.Permissions.Delete
                ));
        }

        /// <summary>
        /// Authenticates a user by verifying the provided credentials.
        /// </summary>
        /// <param name="credentials">The user's login credentials.</param>
        /// <returns>The authenticated <see cref="IdentityUser"/> if the credentials are valid.</returns>
        /// <exception cref="AuthenticationException">
        /// Thrown when the username is not found or the password is incorrect.
        /// </exception>
        public async Task<IdentityUser> LoginAsync(LoginUser credentials)
        {
            var user = await _userManager.FindByNameAsync(credentials.UserName)
                 ?? throw new AuthenticationException(AuthErrorMessages.InvalidCredentials);

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, credentials.Password);

            if (!isPasswordValid)
            {
                throw new AuthenticationException
                    (AuthErrorMessages.InvalidCredentials);
            }

            return user;
        }

        /// <summary>
        /// Retrieves a list of claims for a specified user.
        /// </summary>
        /// <param name="userName">The username of the user whose claims are to be retrieved.</param>
        /// <returns>A list of claims associated with the user.</returns>
        /// <exception cref="UserNotFoundException">Thrown when the user with the specified username is not found.</exception>
        private async Task<List<Claim>> GetClaims(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);

            if (user == null)
            {
                throw new UserNotFoundException
                    (AuthErrorMessages.UserNotFound);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userName)
            };

            claims.AddRange(GetClaimsSeparated(await _userManager.GetClaimsAsync(user)));

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

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

        /// <summary>
        /// Generates a JWT token string for the specified user based on their claims.
        /// </summary>
        /// <param name="user">The authenticated user for whom the token is generated.</param>
        /// <returns>A JWT token string.</returns> 
        public async Task<string> GenerateTokenString(IdentityUser user)
        {
            var claims = await GetClaims(user.UserName);

            return _tokenService.GenerateTokenString(claims);
        }
    }
}
