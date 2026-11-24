using Microsoft.Extensions.DependencyInjection;
using PostApiService.Infrastructure.Common;
using PostApiService.Infrastructure.Jobs;
using PostApiService.Interfaces;
using PostApiService.Repositories;

namespace PostApiService.Tests.UnitTests.Jobs
{
    public class OrphanedPostImagesCleanupServiceTests
    {
        private readonly IPostImageRepository _mockPostImageRepository;
        private readonly IFileStorageService _mockFileStorageService;
        private readonly IServiceScopeFactory _mockScopeFactory;
        private readonly OrphanedPostImagesCleanupService _service;

        public OrphanedPostImagesCleanupServiceTests()
        {
            _mockPostImageRepository = Substitute.For<IPostImageRepository>();
            _mockFileStorageService = Substitute.For<IFileStorageService>();

            var mockServiceProvider = Substitute.For<IServiceProvider>();
            mockServiceProvider.GetService(typeof(IPostImageRepository))
                .Returns(_mockPostImageRepository);
            mockServiceProvider.GetService(typeof(IFileStorageService))
                .Returns(_mockFileStorageService);

            var mockScope = Substitute.For<IServiceScope>();
            mockScope.ServiceProvider.Returns(mockServiceProvider);

            _mockScopeFactory = Substitute.For<IServiceScopeFactory>();
            _mockScopeFactory.CreateScope().Returns(mockScope);

            _service = new OrphanedPostImagesCleanupService(_mockScopeFactory);
        }

        [Fact]
        public async Task CleanupAsync_ShouldDoNothing_WhenNoOrphanedImagesFound()
        {
            // Arrange
            var ct = CancellationToken.None;
            _mockPostImageRepository
                .GetOrphanedImagesAsync(Arg.Any<DateTime>(), ct)
                .Returns(new List<PostImage>());

            // Act
            await _service.CleanupAsync(ct);

            // Assert
            await _mockPostImageRepository.Received(1)
                .GetOrphanedImagesAsync(Arg.Any<DateTime>(), ct);
            await _mockFileStorageService.DidNotReceive()
                .DeleteByUrlAsync(Arg.Any<string>());
            await _mockPostImageRepository.DidNotReceive()
                .DeleteRangeAsync(Arg.Any<IEnumerable<PostImage>>(), Arg.Any<CancellationToken>());
            await _mockPostImageRepository.DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CleanupAsync_ShouldDeleteFilesAndDbRecords_WhenStorageDeletionSucceeds()
        {
            // Arrange
            var ct = CancellationToken.None;
            var img1 = new PostImage
            {
                Id = 1,
                PostId = null,
                Url = "https://res.cloudinary.com/demo/image/upload/post-content/1.webp"
            };
            var img2 = new PostImage
            {
                Id = 2,
                PostId = null,
                Url = "https://res.cloudinary.com/demo/image/upload/post-content/2.webp"
            };

            _mockPostImageRepository
                .GetOrphanedImagesAsync(Arg.Any<DateTime>(), ct)
                .Returns(new List<PostImage> { img1, img2 });

            _mockFileStorageService.DeleteByUrlAsync(img1.Url).Returns(Result.Success());
            _mockFileStorageService.DeleteByUrlAsync(img2.Url).Returns(Result.Success());

            // Act
            await _service.CleanupAsync(ct);

            // Assert
            await _mockFileStorageService.Received(1).DeleteByUrlAsync(img1.Url);
            await _mockFileStorageService.Received(1).DeleteByUrlAsync(img2.Url);

            await _mockPostImageRepository.Received(1).DeleteRangeAsync(
                Arg.Is<IEnumerable<PostImage>>(list =>
                    list.Count() == 2 && list.Contains(img1) && list.Contains(img2)),
                ct);
            await _mockPostImageRepository.Received(1).SaveChangesAsync(ct);
        }

        [Fact]
        public async Task CleanupAsync_ShouldKeepDbRecord_WhenStorageDeletionFails()
        {
            // Arrange
            var ct = CancellationToken.None;
            var failedImage = new PostImage
            {
                Id = 1,
                PostId = null,
                Url = "https://res.cloudinary.com/demo/image/upload/post-content/fail.webp"
            };
            var succeededImage = new PostImage
            {
                Id = 2,
                PostId = null,
                Url = "https://res.cloudinary.com/demo/image/upload/post-content/ok.webp"
            };

            _mockPostImageRepository
                .GetOrphanedImagesAsync(Arg.Any<DateTime>(), ct)
                .Returns(new List<PostImage> { failedImage, succeededImage });

            _mockFileStorageService.DeleteByUrlAsync(failedImage.Url)
                .Returns(Result.Error("Delete failed", "DELETE_FAILED"));
            _mockFileStorageService.DeleteByUrlAsync(succeededImage.Url)
                .Returns(Result.Success());

            // Act
            await _service.CleanupAsync(ct);

            // Assert
            await _mockPostImageRepository.Received(1).DeleteRangeAsync(
                Arg.Is<IEnumerable<PostImage>>(list =>
                    list.Count() == 1 && list.Contains(succeededImage) && !list.Contains(failedImage)),
                ct);
            await _mockPostImageRepository.Received(1).SaveChangesAsync(ct);
        }

        [Fact]
        public async Task CleanupAsync_ShouldRemoveRecordWithoutCallingStorage_WhenUrlIsEmpty()
        {
            // Arrange
            var ct = CancellationToken.None;
            var emptyUrlImage = new PostImage
            {
                Id = 1,
                PostId = null,
                Url = "   "
            };

            _mockPostImageRepository
                .GetOrphanedImagesAsync(Arg.Any<DateTime>(), ct)
                .Returns(new List<PostImage> { emptyUrlImage });

            // Act
            await _service.CleanupAsync(ct);

            // Assert
            await _mockFileStorageService.DidNotReceive()
                .DeleteByUrlAsync(Arg.Any<string>());
            await _mockPostImageRepository.Received(1).DeleteRangeAsync(
                Arg.Is<IEnumerable<PostImage>>(list =>
                    list.Count() == 1 && list.Contains(emptyUrlImage)),
                ct);
            await _mockPostImageRepository.Received(1).SaveChangesAsync(ct);
        }
    }
}