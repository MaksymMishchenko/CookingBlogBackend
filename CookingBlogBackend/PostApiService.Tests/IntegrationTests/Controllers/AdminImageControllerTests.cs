using Microsoft.Extensions.DependencyInjection;
using PostApiService.Infrastructure.Constants;
using PostApiService.Interfaces;
using PostApiService.Models.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PostApiService.Tests.IntegrationTests.Controllers
{
    public class AdminImageControllerTests : IClassFixture<RealFileStorageTestFixture>
    {
        private readonly RealFileStorageTestFixture _fixture;
        private readonly HttpClient _client;

        public AdminImageControllerTests(RealFileStorageTestFixture fixture)
        {
            _fixture = fixture;
            _client = fixture.Client!;
        }

        [Fact]
        public async Task UploadContentImage_ShouldReturn502_WhenCloudinaryFailsAfterRetries()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();
            _fixture.LoginAsAdmin();

            var cloudinaryMock = _fixture.Services!.GetRequiredService<ICloudinaryWrapper>();
            cloudinaryMock.UploadAsync(Arg.Any<CloudinaryDotNet.Actions.ImageUploadParams>())
                .Returns(_ => Task.FromResult(new CloudinaryDotNet.Actions.ImageUploadResult
                {
                    Error = new CloudinaryDotNet.Actions.Error { Message = "Cloudinary unavailable" }
                }));

            using var content = new MultipartFormDataContent();
            var fileBytes = new byte[] { 1, 2, 3, 4, 5 };
            using var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(fileContent, "ContentImage", "step.png");
            content.Add(new StringContent("Test alt"), "Alt");

            // Act
            var response = await _client.PostAsync("/api/admin/image", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

            var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
            Assert.NotNull(errorResponse);
            Assert.False(errorResponse.Success);
            Assert.Equal(Messages.ImageM.Errors.UploadFailedCode, errorResponse.ErrorCode);
            Assert.Equal(Messages.ImageM.Errors.UploadFailed, errorResponse.Message);

            using var scope = _fixture.Services!.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.False(await dbContext.PostImages.AnyAsync());
        }
    }
}