using PostApiService.Infrastructure.Common;
using PostApiService.Repositories;
using PostApiService.Services;

namespace PostApiService.Tests.UnitTests.Services
{
    public class UserServiceTests
    {
        private readonly IUserRepository _mockUserRepository;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _mockUserRepository = Substitute.For<IUserRepository>();
            _userService = new UserService(_mockUserRepository);
        }

        [Fact]
        public async Task GetAuthorsAsync_ShouldReturnSuccess_WithAuthorsList()
        {
            // Arrange
            var ct = CancellationToken.None;
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

            Assert.Equal(Auth.AdminM.Success.ContributorsRetrievedSuccessfully, result.Message);

            await _mockUserRepository.Received(1).GetAdminAndContributorUsersAsync(ct);
        }
    }
}
