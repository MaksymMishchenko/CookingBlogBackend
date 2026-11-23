using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PostApiService.Infrastructure.Common;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Response;
using PostApiService.Tests.Fixtures;

namespace PostApiService.Tests.IntegrationTests.Services
{
    public class FileStorageServiceIntegrationTests : IClassFixture<RealFileStorageTestFixture>
    {
        private readonly RealFileStorageTestFixture _fixture;

        public FileStorageServiceIntegrationTests(RealFileStorageTestFixture fixture)
        {
            _fixture = fixture;
        }

        private static IFormFile CreateFormFile(string fileName)
        {
            var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
            return new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
        }

        [Fact]
        public async Task UploadContentImageAsync_ShouldSavePostImageAndReturnDto_WhenAdminUploadsValidFile()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();

            var (service, _, webContext) = _fixture.GetScopedService<IFileStorageService>();
            webContext.UserId = TestUserData.AdminId;
            webContext.UserName = TestUserData.AdminUserName;
            webContext.IsAdmin = true;

            var file = CreateFormFile("recipe-step.png");
            const string alt = "Step 1: Chop onions";

            // Act
            var result = await service.UploadContentImageAsync(file, alt);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);

            var dto = Assert.IsType<PostImageUploadResponseDto>(result.Value);
            Assert.True(dto.Id > 0);
            Assert.False(string.IsNullOrWhiteSpace(dto.Url));
            Assert.Equal(alt, dto.Alt);

            var (_, dbContextAfter, _) = _fixture.GetScopedService<IFileStorageService>();
            var savedImage = await dbContextAfter.PostImages
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            Assert.NotNull(savedImage);
            Assert.Null(savedImage.PostId);
            Assert.Equal(dto.Url, savedImage.Url);
            Assert.Equal(alt, savedImage.Alt);
            Assert.False(string.IsNullOrWhiteSpace(savedImage.PublicId));
        }
    }
}