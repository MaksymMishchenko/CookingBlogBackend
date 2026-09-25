using PostApiService.Infrastructure.Common;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Tests.IntegrationTests.Services
{
    [Collection("SharedDatabase")]
    public class AdminUserServiceIntegrationTests
    {
        private readonly ServiceTestFixture _fixture;

        public AdminUserServiceIntegrationTests(ServiceTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GetAuthorsAsync_ShouldReturnAuthors_WhenAuthorsExist()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedAdminAsync();

            var (service, _, _) = _fixture.GetScopedService<IAdminUserService>();

            // Act
            var result = await service.GetAdminAndContributorUsersAsync();

            // Assert
            var data = Assert.IsType<Result<List<AuthorsDto>>>(result);

            Assert.True(data.IsSuccess);
            Assert.Equal(ResultStatus.Success, data.Status);
            Assert.NotNull(data.Value);
            Assert.NotEmpty(data.Value);

            var adminDto = data.Value.FirstOrDefault();
            Assert.NotNull(adminDto);
            Assert.False(string.IsNullOrEmpty(adminDto.Id));
            Assert.False(string.IsNullOrEmpty(adminDto.UserName));

            Assert.Equal(UserM.Success.AdminAndContributorUsersRetrievedSuccessfully, data.Message);
        }
    }
}
