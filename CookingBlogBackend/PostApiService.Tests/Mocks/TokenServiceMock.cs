using PostApiService.Interfaces;
using System.Security.Claims;

namespace PostApiService.Tests.Mocks
{
    class TokenServiceMock : ITokenService
    {
        private readonly Exception? _exception;

        public TokenServiceMock(Exception? exception = null)
        {
            _exception = exception;
        }

        public string GenerateTokenString(IEnumerable<Claim> claims)
        {
            if (_exception != null)
                throw _exception;

            return "";
        }
    }
}