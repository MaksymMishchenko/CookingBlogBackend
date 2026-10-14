using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using PostApiService.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PostApiService.Tests.UnitTests.Services
{
    public class ImageServiceTests : IDisposable
    {
        private readonly IWebHostEnvironment _envMock;
        private readonly string _testWebRootPath;
        private readonly ImageService _imageService;
        
        private static readonly byte[] ValidPngBytes = CreateTinyPngBytes();

        public ImageServiceTests()
        {
            _envMock = Substitute.For<IWebHostEnvironment>();
            _testWebRootPath = Path.Combine(Path.GetTempPath(), "CookingBlogTests_" + Guid.NewGuid().ToString());
            _envMock.WebRootPath.Returns(_testWebRootPath);

            _imageService = new ImageService(_envMock);
        }

        [Fact]
        public async Task SavePostPreviewAsync_ValidImage_ReturnsSuccessPathAndSavesFile()
        {
            // Arrange
            var fileMock = CreateTestImageFile(300, 300);
            string slug = "test-recipe-slug";

            // Act
            var result = await _imageService.SavePostPreviewAsync(fileMock, slug);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.StartsWith("/images/posts/", result.Value);
            Assert.EndsWith(".webp", result.Value);
            
            var relativePath = result.Value!.TrimStart('/');
            var fullPath = Path.Combine(_testWebRootPath, relativePath);
            Assert.True(File.Exists(fullPath));
        }

        [Fact]
        public async Task SavePostPreviewAsync_ImageTooSmall_ReturnsInvalidResult()
        {
            // Arrange
            var fileMock = CreateTestImageFile(200, 200);
            string slug = "small-image-slug";

            // Act
            var result = await _imageService.SavePostPreviewAsync(fileMock, slug);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ImageM.Errors.ImageTooSmallCode, result.ErrorCode);
        }

        private static byte[] CreateTinyPngBytes()
        {
            using var ms = new MemoryStream();
            using var image = new Image<Rgba32>(300, 300);
            image.SaveAsPng(ms);
            return ms.ToArray();
        }

        private IFormFile CreateTestImageFile(int width, int height)
        {            
            if (width >= 250 && height >= 250)
            {
                var stream = new MemoryStream(ValidPngBytes);
                return new FormFile(stream, 0, stream.Length, "file", "test-image.png")
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "image/png"
                };
            }
            
            var memoryStream = new MemoryStream();
            using (var image = new Image<Rgba32>(width, height))
            {
                image.SaveAsPng(memoryStream);
            }
            memoryStream.Position = 0;

            return new FormFile(memoryStream, 0, memoryStream.Length, "file", "small-image.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
        }

        public void Dispose()
        {            
            if (Directory.Exists(_testWebRootPath))
            {
                Directory.Delete(_testWebRootPath, true);
            }
        }
    }
}