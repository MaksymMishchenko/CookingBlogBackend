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

        [Fact]
        public async Task SavePostContentImagesAsync_ValidFiles_ReturnsSuccessWithOrderedUrls()
        {
            // Arrange
            var file1 = CreateFormFile("image1.png");
            var file2 = CreateFormFile("image2.png");
            var files = new List<IFormFile> { file1, file2 };
            string slug = "my-awesome-post";

            var url1 = new Uri("https://res.cloudinary.com/demo/image/upload/v1/posts/content/my-awesome-post-1-abc.webp");
            var url2 = new Uri("https://res.cloudinary.com/demo/image/upload/v1/posts/content/my-awesome-post-2-def.webp");
            
            _cloudinaryWrapperMock.UploadAsync(Arg.Any<ImageUploadParams>())
                .Returns(
                    Task.FromResult(new ImageUploadResult { SecureUrl = url1, Error = null }),
                    Task.FromResult(new ImageUploadResult { SecureUrl = url2, Error = null })
                );

            // Act
            var result = await _fileStorageService.SavePostContentImagesAsync(files, slug);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value.Count);
            Assert.Equal(url1.ToString(), result.Value[0]);
            Assert.Equal(url2.ToString(), result.Value[1]);
            
            await _cloudinaryWrapperMock.Received(2).UploadAsync(Arg.Any<ImageUploadParams>());
        }

        [Fact]
        public async Task SavePostContentImagesAsync_FailureOnSubsequentImage_TriggersRollbackAndDeleteOfUploaded()
        {
            // Arrange
            var file1 = CreateFormFile("image1.png");
            var file2 = CreateFormFile("image2.png");
            var files = new List<IFormFile> { file1, file2 };
            string slug = "my-awesome-post";

            var url1 = new Uri("https://res.cloudinary.com/demo/image/upload/v1/posts/content/my-awesome-post-1-abc.webp");
            
            _cloudinaryWrapperMock.UploadAsync(Arg.Any<ImageUploadParams>())
                .Returns(
                    Task.FromResult(new ImageUploadResult { SecureUrl = url1, Error = null }),
                    Task.FromResult(new ImageUploadResult { Error = new Error { Message = "Upload limit exceeded" } })
                );
            
            _cloudinaryWrapperMock.DeleteResourcesAsync(Arg.Any<DelResParams>())
                .Returns(Task.FromResult(new DelResResult { Error = null }));

            // Act
            var result = await _fileStorageService.SavePostContentImagesAsync(files, slug);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ImageM.Errors.UploadFailedCode, result.ErrorCode);
            
            await _cloudinaryWrapperMock.Received(1).DeleteResourcesAsync(
                Arg.Is<DelResParams>(p => p.PublicIds.Contains("posts/content/my-awesome-post-1-abc"))
            );
        }

        [Fact]
        public async Task DeleteByUrlsAsync_MultipleValidUrls_CallsBulkDeleteSuccessfully()
        {
            // Arrange
            var urls = new List<string>
            {
                "https://res.cloudinary.com/demo/image/upload/v1/posts/content/post-1-abc.webp",
                "https://res.cloudinary.com/demo/image/upload/v2/posts/content/post-2-def.webp"
            };

            _cloudinaryWrapperMock.DeleteResourcesAsync(Arg.Any<DelResParams>())
                .Returns(Task.FromResult(new DelResResult { Error = null }));

            // Act
            var result = await _fileStorageService.DeleteByUrlsAsync(urls);

            // Assert
            Assert.True(result.IsSuccess);
            
            await _cloudinaryWrapperMock.Received(1).DeleteResourcesAsync(
                Arg.Is<DelResParams>(p => p.PublicIds.Count == 2 &&
                                          p.PublicIds.Contains("posts/content/post-1-abc") &&
                                          p.PublicIds.Contains("posts/content/post-2-def"))
            );
        }

        [Fact]
        public async Task DeleteByUrlsAsync_EmptyCollection_ReturnsSuccessWithoutCallingCloudinary()
        {
            // Arrange
            var urls = new List<string>();

            // Act
            var result = await _fileStorageService.DeleteByUrlsAsync(urls);

            // Assert
            Assert.True(result.IsSuccess);
            await _cloudinaryWrapperMock.DidNotReceive().DeleteResourcesAsync(Arg.Any<DelResParams>());
        }
        
        private IFormFile CreateFormFile(string fileName)
        {
            var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
            return new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
        }
    }
}