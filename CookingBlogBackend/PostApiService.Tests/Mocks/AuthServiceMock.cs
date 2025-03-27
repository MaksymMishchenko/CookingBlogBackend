using Microsoft.AspNetCore.Identity;
using PostApiService.Interfaces;
using PostApiService.Models;

namespace PostApiService.Tests.Mocks
{
    class AuthServiceMock : IAuthService
    {
        private readonly Exception? _exception;

        public AuthServiceMock(Exception? exception = null)
        {
            _exception = exception;
        }

        public Task<string> GenerateTokenString(IdentityUser user)
        {
            if (_exception != null)
                throw _exception;

            return Task.FromResult("mocked_token");
        }

        public Task<IdentityUser> GetCurrentUserAsync()
        {
            if (_exception != null)
                throw _exception;

            return Task.FromResult(new IdentityUser { UserName = "testuser" });
        }

        public Task<IdentityUser> LoginAsync(LoginUser credentials)
        {
            if (_exception != null)
                throw _exception;

            return Task.FromResult(new IdentityUser { UserName = "testuser" });
        }

        public Task RegisterUserAsync(RegisterUser user)
        {
            if (_exception != null)
                throw _exception;

            return Task.CompletedTask;
        }
    }
}
