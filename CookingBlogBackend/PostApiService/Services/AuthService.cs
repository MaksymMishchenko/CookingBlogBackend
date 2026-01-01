using PostApiService.Exceptions;
using PostApiService.Helper;
using PostApiService.Interfaces;
using PostApiService.Models.TypeSafe;
using PostApiService.Repositories;
using System.Security.Authentication;
using System.Security.Claims;

namespace PostApiService.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly ITokenService _tokenService;

        public AuthService(IAuthRepository authRepository,
            ITokenService tokenService)
        {
            _authRepository = authRepository;
            _tokenService = tokenService;
        }

        private static Claim GetContributorClaims(string controllerName)
        {
            return new Claim(controllerName,
                ClaimHelper.SerializePermissions(
                    TS.Permissions.Write,
                    TS.Permissions.Update,
                    TS.Permissions.Delete
                ));
        }

        private async Task<List<Claim>> GetClaims(string userName)
        {
            var user = await _authRepository.FindByNameAsync(userName);

            if (user == null)
            {
                throw new UserNotFoundException(Auth.LoginM.Errors.UserNotFound);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, userName)
            };

            var userClaims = await _authRepository.GetClaimsAsync(user);

            var serializedClaims = userClaims
                .Where(claim => claim.Type != ClaimTypes.NameIdentifier && claim.Type != ClaimTypes.Name)
                .ToList();

            var deserializedClaims = GetClaimsSeparated(serializedClaims);

            claims.AddRange(deserializedClaims);

            var roles = await _authRepository.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

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

        /// <summary>
        /// Registers a new user in the system.        
        /// </summary>        
        public async Task RegisterUserAsync(RegisterUser user)
        {
            var existingUser = await _authRepository.FindByNameAsync(user.UserName);
            if (existingUser != null)
            {
                throw new UserAlreadyExistsException
                    (Auth.Registration.Errors.UsernameAlreadyExists);
            }

            var existingUserByEmail = await _authRepository.FindByEmailAsync(user.Email);
            if (existingUserByEmail != null)
            {
                throw new EmailAlreadyExistsException
                    (Auth.Registration.Errors.EmailAlreadyExists);
            }

            var identityUser = new IdentityUser
            {
                UserName = user.UserName,
                Email = user.Email
            };

            var result = await _authRepository.CreateAsync(identityUser, user.Password);

            if (!result.Succeeded)
            {
                throw new UserCreationException
                    (string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            var identityResult = await _authRepository.AddClaimAsync
                (identityUser, GetContributorClaims(TS.Controller.Comment));

            if (!identityResult.Succeeded)
            {
                throw new UserClaimException
                    (Auth.Registration.Errors.ClaimAssignmentFailed);
            }
        }

        /// <summary>
        /// Authenticates a user by verifying the provided credentials.
        /// </summary>       
        public async Task<IdentityUser> LoginAsync(LoginUser credentials)
        {
            var user = await _authRepository.FindByNameAsync(credentials.UserName)
                 ?? throw new AuthenticationException(Auth.LoginM.Errors.InvalidCredentials);

            var isPasswordValid = await _authRepository.CheckPasswordAsync(user, credentials.Password);

            if (!isPasswordValid)
            {
                throw new AuthenticationException
                    (Auth.LoginM.Errors.InvalidCredentials);
            }

            return user;
        }

        /// <summary>
        /// Retrieves the currently authenticated user from the HTTP context.
        /// </summary>
        public async Task<IdentityUser> GetCurrentUserAsync()
        {
            var user = await _authRepository.GetUserAsync();

            if (user == null)
            {
                throw new UnauthorizedAccessException
                    (Auth.LoginM.Errors.UnauthorizedAccess);
            }

            return user;
        }

        /// <summary>
        /// Generates a JWT token string for the specified user based on their claims.
        /// </summary>        
        public async Task<string> GenerateTokenString(IdentityUser user)
        {
            var claims = await GetClaims(user.UserName);

            return _tokenService.GenerateTokenString(claims);
        }
    }
}
