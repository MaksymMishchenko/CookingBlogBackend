using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using PostApiService.Interfaces;
using PostApiService.Services;

namespace PostApiService.Tests.UnitTests.Services
{
    public class FileStorageServiceTests
    {
        private readonly ICloudinaryWrapper _cloudinaryWrapperMock;
        private readonly FileStorageService _fileStorageService;

        public FileStorageServiceTests()
        {
            _cloudinaryWrapperMock = Substitute.For<ICloudinaryWrapper>();

            _fileStorageService = new FileStorageService(_cloudinaryWrapperMock);
        }

        [Fact]
        public async Task SavePostPreviewAsync_ValidImage_ReturnsSuccessWithSecureUrl()
        {
            // Arrange
            var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
            var fileMock = new FormFile(stream, 0, stream.Length, "file", "test-image.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            string slug = "test-recipe";

            var expectedSecureUrl = new Uri("https://res.cloudinary.com/demo/image/upload/v123456/posts/test-recipe-123456.webp");

            _cloudinaryWrapperMock.UploadAsync(Arg.Any<ImageUploadParams>())
                .Returns(Task.FromResult(new ImageUploadResult
                {
                    SecureUrl = expectedSecureUrl,
                    Error = null
                }));

            // Act
            var result = await _fileStorageService.SavePostPreviewAsync(fileMock, slug);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedSecureUrl.ToString(), result.Value);

            await _cloudinaryWrapperMock.Received(1).UploadAsync(Arg.Any<ImageUploadParams>());
        }

        [Fact]
        public async Task SavePostPreviewAsync_CloudinaryReturnsError_ReturnsFailureResult()
        {
            // Arrange
            var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
            var fileMock = new FormFile(stream, 0, stream.Length, "file", "test-image.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            string slug = "test-recipe";

            _cloudinaryWrapperMock.UploadAsync(Arg.Any<ImageUploadParams>())
                .Returns(Task.FromResult(new ImageUploadResult
                {
                    Error = new Error { Message = "Cloudinary internal processing error" }
                }));

            // Act
            var result = await _fileStorageService.SavePostPreviewAsync(fileMock, slug);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ImageM.Errors.UploadFailedCode, result.ErrorCode);
        }

        [Fact]
        public async Task DeleteByUrlAsync_ValidUrl_ReturnsSuccess()
        {
            // Arrange
            var url = "https://res.cloudinary.com/demo/image/upload/v123456/posts/test-recipe-123456.webp";

            _cloudinaryWrapperMock.DestroyAsync(Arg.Any<DeletionParams>())
                .Returns(Task.FromResult(new DeletionResult
                {
                    Result = "ok",
                    Error = null
                }));

            // Act
            var result = await _fileStorageService.DeleteByUrlAsync(url);

            // Assert
            Assert.True(result.IsSuccess);
            await _cloudinaryWrapperMock.Received(1).DestroyAsync(Arg.Is<DeletionParams>(p => p.PublicId == "posts/test-recipe-123456"));
        }

        [Fact]
        public async Task DeleteByUrlAsync_CloudinaryReturnsError_ReturnsFailureResult()
        {
            // Arrange
            var url = "https://res.cloudinary.com/demo/image/upload/v123456/posts/test-recipe-123456.webp";

            _cloudinaryWrapperMock.DestroyAsync(Arg.Any<DeletionParams>())
                .Returns(Task.FromResult(new DeletionResult
                {
                    Error = new Error { Message = "Not Found" }
                }));

            // Act
            var result = await _fileStorageService.DeleteByUrlAsync(url);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ImageM.Errors.DeleteFailedCode, result.ErrorCode);
        }

        [Theory]
        [InlineData("https://invalid-url-format.com/image.png")]
        [InlineData("https://res.cloudinary.com/demo/image/not-upload-path/v123/image.webp")]
        [InlineData("just-a-random-string")]
        public async Task DeleteByUrlAsync_InvalidUrl_ReturnsFailureResultWithoutCallingCloudinary(string invalidUrl)
        {
            // Act
            var result = await _fileStorageService.DeleteByUrlAsync(invalidUrl);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ImageM.Errors.DeleteFailedCode, result.ErrorCode);

            await _cloudinaryWrapperMock.DidNotReceive().DestroyAsync(Arg.Any<DeletionParams>());
        }
    }
}