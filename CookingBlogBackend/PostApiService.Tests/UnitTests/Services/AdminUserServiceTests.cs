using PostApiService.Infrastructure.Common;
using PostApiService.Infrastructure.Services;
using PostApiService.Repositories;
using PostApiService.Services;

namespace PostApiService.Tests.UnitTests.Services
{
    public class AdminUserServiceTests
    {
        private readonly IUserRepository _mockUserRepository;
        private readonly IWebContext _mockWebContext;
        private readonly AdminUserService _userService;

        public AdminUserServiceTests()
        {
            _mockUserRepository = Substitute.For<IUserRepository>();
            _mockWebContext = Substitute.For<IWebContext>();
            _userService = new AdminUserService(_mockUserRepository, _mockWebContext);
        }

        [Fact]
        public async Task GetAdminAndContributorUsersAsync_ShouldReturnSuccess_WhenUserIsAdmin()
        {
            // Arrange
            var ct = CancellationToken.None;

            _mockWebContext.UserId.Returns("admin-id-1");
            _mockWebContext.IsAdmin.Returns(true);

            var users = new List<IdentityUser>
            {
                new IdentityUser { Id = "1", UserName = "admin1" },
                new IdentityUser { Id = "2", UserName = "admin2" }
            };

            _mockUserRepository.GetAdminAndContributorUsersAsync(ct)
                .Returns(Task.FromResult(users));

            // Act
            var result = await _userService.GetAdminAndContributorUsersAsync(ct);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value.Count);

            Assert.Equal("1", result.Value[0].Id);
            Assert.Equal("admin1", result.Value[0].UserName);

            Assert.Equal("2", result.Value[1].Id);
            Assert.Equal("admin2", result.Value[1].UserName);

            Assert.Equal(UserM.Success.AdminAndContributorUsersRetrievedSuccessfully, result.Message);

            await _mockUserRepository.Received(1).GetAdminAndContributorUsersAsync(ct);
        }

        [Fact]
        public async Task GetAdminAndContributorUsersAsync_ShouldReturnUnauthorized_WhenUserIsNotLoggedIn()
        {
            // Arrange
            var ct = CancellationToken.None;
           
            _mockWebContext.UserId.Returns((string?)null);

            // Act
            var result = await _userService.GetAdminAndContributorUsersAsync(ct);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Unauthorized, result.Status);
            
            await _mockUserRepository.DidNotReceive().GetAdminAndContributorUsersAsync(ct);
        }

        [Fact]
        public async Task GetAdminAndContributorUsersAsync_ShouldReturnForbidden_WhenUserIsNotAdmin()
        {
            // Arrange
            var ct = CancellationToken.None;
            
            _mockWebContext.UserId.Returns("user-id-1");
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IsContributor.Returns(false);

            // Act
            var result = await _userService.GetAdminAndContributorUsersAsync(ct);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Forbidden, result.Status);
            
            await _mockUserRepository.DidNotReceive().GetAdminAndContributorUsersAsync(ct);
        }
    }
}
