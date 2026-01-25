using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Tests.Helper
{
    public static class AuthTestData
    {
        public static RegisterUserDto CreateRegisterUserDto(
            string userName = "correctUser",
            string email = "correctEmail@test.com") => new()
            {
                UserName = userName,
                Email = email,
                Password = "-Rtyuehe2-"
            };

        public static RegisteredUserDto CreateRegisteredUserDto(string id = "1", string userName = "correctUser", string email = "correctEmail@test.com")
            => new(id, userName, email);

        public static LoginUserDto CreateUserLoginDto() => new()
        {
            UserName = "testuser",
            Password = "SafePassword123!"
        };

        public static LoggedInUserDto CreateLoggedInUserDto(string userName = "testuser", string token = "fake-jwt-token")
        {
            return new LoggedInUserDto(token, userName);
        }

        public static IdentityUser CreateIdentityUser(string userName = "testuser") => new()
        {
            Id = Guid.NewGuid().ToString(),
            UserName = userName,
            Email = $"{userName}@example.com",
            NormalizedUserName = userName.ToUpper(),
            PasswordHash = "hashed_password_string"
        };
    }
}
