using Microsoft.Extensions.Options;
using PostApiService.Infrastructure.Configuration;
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
        public void GenerateTokenString_ValidClaims_ReturnToken()
        {
            // Arrange           
            var mockOptions = Substitute.For<IOptions<JwtConfiguration>>();
            
            mockOptions.Value.Returns(_validConfig);

            var service = new TokenService(mockOptions);

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
