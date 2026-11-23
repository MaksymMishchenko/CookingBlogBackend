using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using PostApiService.Infrastructure.Common;
using PostApiService.Infrastructure.Constants;
using PostApiService.Infrastructure.Services;
using PostApiService.Interfaces;
using PostApiService.Repositories;
using PostApiService.Services;

namespace PostApiService.Tests.UnitTests.Services
{
    public class FileStorageServiceTests
    {
        private readonly ICloudinaryWrapper _cloudinaryWrapperMock;
        private readonly IPostImageRepository _postImageRepositoryMock;
        private readonly IWebContext _webContextMock;
        private readonly FileStorageService _fileStorageService;

        public FileStorageServiceTests()
        {
            _cloudinaryWrapperMock = Substitute.For<ICloudinaryWrapper>();
            _postImageRepositoryMock = Substitute.For<IPostImageRepository>();
            _webContextMock = Substitute.For<IWebContext>();

            _fileStorageService = new FileStorageService(
                _cloudinaryWrapperMock,
                _postImageRepositoryMock,
                _webContextMock);
        }

        private static IFormFile CreateTestFormFile(string fileName = "test-image.png", string contentType = "image/png")
        {
            var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
            return new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        [Fact]
        public async Task SavePostPreviewAsync_ValidImage_ReturnsSuccessWithSecureUrl()
        {
            // Arrange
            var fileMock = CreateTestFormFile();
            const string slug = "test-recipe";
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
            var fileMock = CreateTestFormFile();
            const string slug = "test-recipe";

            _cloudinaryWrapperMock.UploadAsync(Arg.Any<ImageUploadParams>())
                .Returns(Task.FromResult(new ImageUploadResult
                {
                    Error = new Error { Message = "Cloudinary internal processing error" }
                }));

            // Act
            var result = await _fileStorageService.SavePostPreviewAsync(fileMock, slug);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(Messages.ImageM.Errors.UploadFailedCode, result.ErrorCode);
        }

        [Fact]
        public async Task UploadContentImageAsync_ValidUpload_AddsEntitySavesAndReturnsSuccess()
        {
            // Arrange
            var fileMock = CreateTestFormFile();
            const string alt = "Freshly baked bread";
            var expectedSecureUrl = new Uri("https://res.cloudinary.com/demo/image/upload/v123456/posts/content/img-123-abc.webp");

            _webContextMock.UserId.Returns("user-id");
            _webContextMock.IsContributor.Returns(true);

            _cloudinaryWrapperMock.UploadAsync(Arg.Any<ImageUploadParams>())
                .Returns(Task.FromResult(new ImageUploadResult
                {
                    SecureUrl = expectedSecureUrl,
                    Error = null
                }));

            // Act
            var result = await _fileStorageService.UploadContentImageAsync(fileMock, alt);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(expectedSecureUrl.ToString(), result.Value!.Url);
            Assert.Equal(alt, result.Value.Alt);

            await _postImageRepositoryMock.Received(1).AddAsync(
                Arg.Is<PostImage>(x =>
                    x.Url == expectedSecureUrl.ToString() &&
                    x.Alt == alt &&
                    x.PostId == null),
                Arg.Any<CancellationToken>());
            await _postImageRepositoryMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UploadContentImageAsync_TransientFailureThenSuccess_RetriesAndReturnsSuccess()
        {
            // Arrange
            var fileMock = CreateTestFormFile();
            var expectedSecureUrl = new Uri("https://res.cloudinary.com/demo/image/upload/v123456/posts/content/img-123-abc.webp");

            _webContextMock.UserId.Returns("user-id");
            _webContextMock.IsContributor.Returns(true);

            _cloudinaryWrapperMock.UploadAsync(Arg.Any<ImageUploadParams>())
                .Returns(
                    _ => Task.FromException<ImageUploadResult>(new HttpRequestException("Temporary network glitch")),
                    _ => Task.FromResult(new ImageUploadResult
                    {
                        SecureUrl = expectedSecureUrl,
                        Error = null
                    }));

            // Act
            var result = await _fileStorageService.UploadContentImageAsync(fileMock, null);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedSecureUrl.ToString(), result.Value!.Url);
            await _cloudinaryWrapperMock.Received(2).UploadAsync(Arg.Any<ImageUploadParams>());
            await _postImageRepositoryMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UploadContentImageAsync_AllRetriesFail_ReturnsBadGatewayWithoutSaving()
        {
            // Arrange
            var fileMock = CreateTestFormFile();

            _webContextMock.UserId.Returns("user-id");
            _webContextMock.IsContributor.Returns(true);

            _cloudinaryWrapperMock.UploadAsync(Arg.Any<ImageUploadParams>())
                .Returns(Task.FromResult(new ImageUploadResult
                {
                    Error = new Error { Message = "Cloudinary unavailable" }
                }));

            // Act
            var result = await _fileStorageService.UploadContentImageAsync(fileMock, "alt");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.BadGateway, result.Status);
            Assert.Equal(Messages.ImageM.Errors.UploadFailedCode, result.ErrorCode);

            await _postImageRepositoryMock.DidNotReceive().AddAsync(Arg.Any<PostImage>(), Arg.Any<CancellationToken>());
            await _postImageRepositoryMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
            await _cloudinaryWrapperMock.Received(3).UploadAsync(Arg.Any<ImageUploadParams>());
        }

        [Fact]
        public async Task DeleteByUrlAsync_ValidUrl_ReturnsSuccess()
        {
            // Arrange
            const string url = "https://res.cloudinary.com/demo/image/upload/v123456/posts/test-recipe-123456.webp";

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
            await _cloudinaryWrapperMock.Received(1)
                .DestroyAsync(Arg.Is<DeletionParams>(p => p.PublicId == "posts/test-recipe-123456"));
        }

        [Fact]
        public async Task DeleteByUrlAsync_CloudinaryReturnsError_ReturnsFailureResult()
        {
            // Arrange
            const string url = "https://res.cloudinary.com/demo/image/upload/v123456/posts/test-recipe-123456.webp";

            _cloudinaryWrapperMock.DestroyAsync(Arg.Any<DeletionParams>())
                .Returns(Task.FromResult(new DeletionResult
                {
                    Error = new Error { Message = "Not Found" }
                }));

            // Act
            var result = await _fileStorageService.DeleteByUrlAsync(url);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(Messages.ImageM.Errors.DeleteFailedCode, result.ErrorCode);
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
            Assert.Equal(Messages.ImageM.Errors.DeleteFailedCode, result.ErrorCode);

            await _cloudinaryWrapperMock.DidNotReceive().DestroyAsync(Arg.Any<DeletionParams>());
        }
    }
}