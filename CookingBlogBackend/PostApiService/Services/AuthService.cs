using Microsoft.AspNetCore.Identity;
using PostApiService.Exceptions;
using PostApiService.Helper;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Models.TypeSafe;
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
    }
}
