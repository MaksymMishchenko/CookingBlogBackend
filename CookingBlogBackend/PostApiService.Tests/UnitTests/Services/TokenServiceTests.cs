using Microsoft.Extensions.Options;
using Moq;
using PostApiService.Models;
using PostApiService.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PostApiService.Tests.UnitTests.Services
{
    public class TokenServiceTests
    {
        private readonly JwtConfiguration _validConfig = new()
        {
            SecretKey = "E4FEFC03-F5A9-4851-86AB-7EB386D3B9AA",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            TokenExpirationMinutes = 60
        };

        [Fact]
        public async Task GenerateTokenString_ValidClaims_ReturnToken()
        {
            // Arrange
            var mock = new Mock<IOptions<JwtConfiguration>>();
            mock.Setup(v => v.Value).Returns(_validConfig);
            var service = new TokenService(mock.Object);

            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };

            // Act
            var token = service.GenerateTokenString(claims);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            Assert.Equal("TestIssuer", jwtToken.Issuer);
            Assert.Equal("TestAudience", jwtToken.Audiences.First());
        }
    }
}
