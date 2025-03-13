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

        public Task RegisterUserAsync(RegisterUser user)
        {
            if (_exception != null)
                throw _exception;

            return Task.CompletedTask;
        }
    }
}
