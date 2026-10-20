using PostApiService.Infrastructure.Common;

namespace PostApiService.Tests.UnitTests.Services.AdminPostServiceTests
{
    public class DeletePostAsyncTests : AdminPostServiceTestsBase
    {
        [Fact]
        public async Task DeletePostAsync_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            const int postId = 1;
            _mockWebContext.UserId.Returns(string.Empty);

            // Act
            var result = await _adminPostService.DeletePostAsync(postId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Unauthorized, result.Status);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccess, result.Message);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccessCode, result.ErrorCode);

            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
            await _mockFileStorageService.DidNotReceive().DeleteByUrlAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturnForbidden_WhenUserIsNotAdminOrContributor()
        {
            // Arrange
            const int postId = 1;
            _mockWebContext.UserId.Returns("regular-user-id");
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IsContributor.Returns(false);

            // Act
            var result = await _adminPostService.DeletePostAsync(postId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Forbidden, result.Status);

            await _mockRepository.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
            await _mockFileStorageService.DidNotReceive().DeleteByUrlAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturnForbidden_WhenContributorDeletesForeignPost()
        {
            // Arrange
            const int postId = 1;
            string contributorId = "contributor-1-id";
            string anotherAuthorId = "contributor-2-id";

            _mockWebContext.UserId.Returns(contributorId);
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IsContributor.Returns(true);

            _mockRepository.GetByIdAsync(postId, Arg.Any<CancellationToken>())
                .Returns(new Post { Id = postId, AuthorId = anotherAuthorId, PreviewImageUrl = OldPreviewUrl });

            // Act
            var result = await _adminPostService.DeletePostAsync(postId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Forbidden, result.Status);

            await _mockRepository.Received(1).GetByIdAsync(postId, Arg.Any<CancellationToken>());
            await _mockRepository.DidNotReceive().DeleteAsync(Arg.Any<Post>(), Arg.Any<CancellationToken>());
            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
            await _mockFileStorageService.DidNotReceive().DeleteByUrlAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturn404NotFound_WhenPostDoesNotExist()
        {
            // Arrange
            int postId = 99;

            _mockWebContext.UserId.Returns("user-id");
            _mockWebContext.IsAdmin.Returns(true);
            _mockRepository.GetByIdAsync(postId, Arg.Any<CancellationToken>())!
                .Returns((Post)null!);

            // Act
            var result = await _adminPostService.DeletePostAsync(postId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal(PostM.Errors.PostNotFound, result.Message);
            Assert.Equal(PostM.Errors.PostNotFoundCode, result.ErrorCode);

            await _mockRepository.Received(1).GetByIdAsync(postId, Arg.Any<CancellationToken>());
            await _mockRepository.DidNotReceive().DeleteAsync(Arg.Is<Post>(p =>
                    p.Id == postId), Arg.Any<CancellationToken>());
            await _mockFileStorageService.DidNotReceive().DeleteByUrlAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturn200Ok_WhenPostRemove()
        {
            // Arrange
            int postId = 1;
            string userId = "contributor-1-id";
            var existingPost = new Post { Id = postId, Title = "To be deleted", AuthorId = userId };

            _mockWebContext.UserId.Returns(userId);
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IsContributor.Returns(true);

            _mockRepository.GetByIdAsync(postId, Arg.Any<CancellationToken>())!
                .Returns(existingPost);

            var ct = CancellationToken.None;

            // Act
            var result = await _adminPostService.DeletePostAsync(postId, ct);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.Equal(PostM.Success.PostDeletedSuccessfully, result.Message);

            await _mockRepository.Received(1).DeleteAsync(existingPost, ct);
            await _mockRepository.Received(1).SaveChangesAsync(ct);
            await _mockFileStorageService.DidNotReceive().DeleteByUrlAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task DeletePostAsync_ShouldDeletePreviewImage_WhenPostHasPreview()
        {
            // Arrange
            int postId = 1;
            string userId = "contributor-1-id";
            var existingPost = new Post { Id = postId, AuthorId = userId, PreviewImageUrl = OldPreviewUrl };

            _mockWebContext.UserId.Returns(userId);
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IsContributor.Returns(true);

            _mockRepository.GetByIdAsync(postId, Arg.Any<CancellationToken>())!
                .Returns(existingPost);

            _mockFileStorageService.DeleteByUrlAsync(OldPreviewUrl).Returns(Result.Success());

            var ct = CancellationToken.None;

            // Act
            var result = await _adminPostService.DeletePostAsync(postId, ct);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(PostM.Success.PostDeletedSuccessfully, result.Message);

            await _mockRepository.Received(1).DeleteAsync(existingPost, ct);
            await _mockRepository.Received(1).SaveChangesAsync(ct);
            await _mockFileStorageService.Received(1).DeleteByUrlAsync(OldPreviewUrl);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturnSuccess_WhenPreviewDeletionFails()
        {
            // Arrange
            int postId = 1;
            string userId = "contributor-1-id";
            var existingPost = new Post { Id = postId, AuthorId = userId, PreviewImageUrl = OldPreviewUrl };

            _mockWebContext.UserId.Returns(userId);
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IsContributor.Returns(true);

            _mockRepository.GetByIdAsync(postId, Arg.Any<CancellationToken>())!
                .Returns(existingPost);

            _mockFileStorageService.DeleteByUrlAsync(OldPreviewUrl)
                .Returns(Result.Error(ImageM.Errors.DeleteFailed, ImageM.Errors.DeleteFailedCode));

            var ct = CancellationToken.None;

            // Act
            var result = await _adminPostService.DeletePostAsync(postId, ct);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(PostM.Success.PostDeletedSuccessfully, result.Message);

            await _mockRepository.Received(1).SaveChangesAsync(ct);
            await _mockFileStorageService.Received(1).DeleteByUrlAsync(OldPreviewUrl);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldDeletePreviewImage_WhenAdminDeletesForeignPost()
        {
            // Arrange
            int postId = 1;
            var existingPost = new Post { Id = postId, AuthorId = "another-author-id", PreviewImageUrl = OldPreviewUrl };

            _mockWebContext.UserId.Returns("admin-id");
            _mockWebContext.IsAdmin.Returns(true);

            _mockRepository.GetByIdAsync(postId, Arg.Any<CancellationToken>())!
                .Returns(existingPost);

            _mockFileStorageService.DeleteByUrlAsync(OldPreviewUrl).Returns(Result.Success());

            // Act
            var result = await _adminPostService.DeletePostAsync(postId);

            // Assert
            Assert.True(result.IsSuccess);
            await _mockFileStorageService.Received(1).DeleteByUrlAsync(OldPreviewUrl);
        }
    }
}
