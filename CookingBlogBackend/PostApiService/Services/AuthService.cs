using PostApiService.Helper;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
using PostApiService.Models.TypeSafe;
using PostApiService.Repositories;
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

        private async Task<List<Claim>> GetClaims(IdentityUser user,
            CancellationToken ct = default)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user!.Id),
                new Claim(ClaimTypes.Name, user.UserName!)
            };

            var userClaims = await _authRepository.GetClaimsAsync(user, ct);

            var serializedClaims = userClaims
                .Where(claim => claim.Type != ClaimTypes.NameIdentifier && claim.Type != ClaimTypes.Name)
                .ToList();

            var deserializedClaims = GetClaimsSeparated(serializedClaims);

            claims.AddRange(deserializedClaims);

            var roles = await _authRepository.GetRolesAsync(user, ct);
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

        private async Task<string> GenerateTokenString(IdentityUser user,
            CancellationToken ct = default)
        {
            var claims = await GetClaims(user, ct);

            return _tokenService.GenerateTokenString(claims);
        }

        /// <summary>
        /// Registers a new user in the system.        
        /// </summary>        
        public async Task<Result<RegisteredUserDto>> RegisterUserAsync(
            RegisterUserDto userDto, CancellationToken ct = default)
        {
            var user = await _authRepository.FindByNameAsync(userDto.UserName, ct);
            var email = await _authRepository.FindByEmailAsync(userDto.Email, ct);

            if (user != null || email != null)
            {
                Log.Warning(Authentication.RegistrationConflict, userDto.UserName, userDto.Email);

                return Result<RegisteredUserDto>.Conflict(Auth.Registration.Errors.UserAlreadyExists,
                    Auth.Registration.Errors.UserAlreadyExistsCode);
            }            

            var identityUser = userDto.ToEntity();

            var createResult = await _authRepository.CreateAsync(identityUser, userDto.Password, ct);
            if (!createResult.Succeeded)
            {
                var errorMsg = string.Join(", ", createResult.Errors.Select(e => e.Description));
                var errorCodes = string.Join(", ", createResult.Errors.Select(e => e.Code));

                Log.Warning(Authentication.RegistrationFailed, userDto.Email, errorCodes, errorMsg);

                return Result<RegisteredUserDto>.Invalid(errorMsg);
            }

            var claimResult = await _authRepository.AddClaimAsync(
                identityUser, GetContributorClaims(TS.Controller.Comment), ct);

            // TODO (TechDebt): #24 Implement TransactionScope or Rollback logic.           
            if (!claimResult.Succeeded)
            {
                Log.Error(Authentication.ClaimAssignmentFailed, identityUser.Id, userDto.Email);

                return Result<RegisteredUserDto>.Error(Auth.Registration.Errors.ClaimAssignmentFailed,
                    Auth.Registration.Errors.ClaimAssignmentFailedCode);
            }

            var createdUserDto = identityUser.ToRegisteredDto();

            Log.Information(Authentication.RegistrationSuccess, userDto.Email);

            return Result<RegisteredUserDto>.Success(createdUserDto, Auth.Registration.Success.RegisterOk);
        }

        /// <summary>
        /// Authenticates a user by verifying the provided credentials.
        /// </summary>       
        public async Task<Result<LoggedInUserDto>> AuthenticateAsync(LoginUserDto credentials,
            CancellationToken ct = default)
        {
            var user = await _authRepository.FindByNameAsync(credentials.UserName, ct);

            // TODO (TechDebt): #25 Implement Account Lockout logic (AccessFailedAsync, IsLockedOutAsync).
            // Currently, the system is vulnerable to brute-force attacks as it doesn't track failed attempts.
            if (user == null || !await _authRepository.CheckPasswordAsync(user, credentials.Password, ct))
            {
                return Result<LoggedInUserDto>.Unauthorized(Auth.LoginM.Errors.InvalidCredentials);
            }

            var token = await GenerateTokenString(user, ct);

            var responseDto = token.ToLoggedInUserDto(user.UserName!);            

            return Result<LoggedInUserDto>.Success(responseDto, Auth.LoginM.Success.LoginSuccess);
        }
    }
}
