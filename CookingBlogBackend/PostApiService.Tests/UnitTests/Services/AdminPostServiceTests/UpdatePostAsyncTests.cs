using Microsoft.AspNetCore.Http;
using PostApiService.Infrastructure.Common;
using PostApiService.Models.Dto.Response;
using System.Linq.Expressions;

namespace PostApiService.Tests.UnitTests.Services.AdminPostServiceTests
{
    public class UpdatePostAsyncTests : AdminPostServiceTestsBase
    {
        [Fact]
        public async Task UpdatePostAsync_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            const int postId = 1;
            var dto = TestDataHelper.GetPostUpdateDto();

            _mockWebContext.UserId.Returns(string.Empty);

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Unauthorized, result.Status);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccess, result.Message);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccessCode, result.ErrorCode);

            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnForbidden_WhenUserIsNotAdminOrContributor()
        {
            // Arrange
            const int postId = 1;
            var dto = TestDataHelper.GetPostUpdateDto();

            _mockWebContext.UserId.Returns("regular-user-id");
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IsContributor.Returns(false);

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Forbidden, result.Status);
            Assert.Null(result.Value);

            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnForbidden_WhenContributorUpdatesForeignPost()
        {
            // Arrange
            const int postId = 1;
            var post = ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            post.AuthorId = "contributor-2-id";

            _mockWebContext.UserId.Returns("contributor-1-id");
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IsContributor.Returns(true);

            var dto = TestDataHelper.GetPostUpdateDto(
                title: "Title", slug: "slug", content: "Some valid content");

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Forbidden, result.Status);
            Assert.Null(result.Value);

            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldSucceed_WhenAdminUpdatesForeignPost()
        {
            // Arrange
            const int postId = 1;
            var post = ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            post.AuthorId = "another-author-id";

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, TestDataHelper.GetPostUpdateDto());

            // Assert
            Assert.True(result.IsSuccess);
            await _mockRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnInvalid_WhenPostIsEmpty()
        {
            // Arrange
            const int postId = 1;
            ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns(string.Empty);

            var dto = TestDataHelper.GetPostUpdateDto(content: "<script></script>");

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Invalid, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(PostM.Errors.Empty, result.Message);
            Assert.Equal(PostM.Errors.EmptyCode, result.ErrorCode);

            _mockSanitizationService.Received(1).SanitizePost(Arg.Any<string>());
            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnNotFound_WhenPostDoesNotExist()
        {
            // Arrange
            const int postId = 1;
            ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            _mockRepository.GetByIdWithAuthorsAsync(postId, Arg.Any<CancellationToken>())
                .Returns((Post?)null);

            var dto = TestDataHelper.GetPostUpdateDto(content: "<script>alert('xss')</script>");

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal(PostM.Errors.PostNotFound, result.Message);
            Assert.Equal(PostM.Errors.PostNotFoundCode, result.ErrorCode);

            await _mockRepository.Received(1).GetByIdWithAuthorsAsync(postId, Arg.Any<CancellationToken>());
            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnConflict_WhenTitleAlreadyExistsForAnotherPost()
        {
            // Arrange
            const int postId = 1;
            ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var dto = TestDataHelper.GetPostUpdateDto(content: "Test post content");

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Equal(string.Format(PostM.Errors.PostTitleOrSlugAlreadyExist,
                dto.Title, dto.Slug), result.Message);
            Assert.Equal(PostM.Errors.PostAlreadyExistCode, result.ErrorCode);

            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnNotFound_WhenCategoryDoesNotExists()
        {
            // Arrange
            const int postId = 1;
            ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            _mockCategoryService.ExistsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(false);

            var dto = TestDataHelper.GetPostUpdateDto(categoryId: 2);

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(CategoryM.Errors.CategoryNotFound, result.Message);
            Assert.Equal(PostM.Errors.CategoryNotFoundCode, result.ErrorCode);

            await _mockCategoryService.Received(1)
                .ExistsAsync(Arg.Is(dto.CategoryId!.Value), Arg.Any<CancellationToken>());
            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldNotCheckCategory_WhenCategoryUnchanged()
        {
            // Arrange
            const int postId = 1;
            var post = ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            post.CategoryId = 1;

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, TestDataHelper.GetPostUpdateDto());

            // Assert
            Assert.True(result.IsSuccess);
            await _mockCategoryService.DidNotReceive()
                .ExistsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnSuccess_WhenPostUpdatedSuccessfully()
        {
            // Arrange
            const int postId = 1;
            var ct = CancellationToken.None;
            ArrangeSuccessfulUpdate(postId, OldPreviewUrl);

            var dto = TestDataHelper.GetPostUpdateDto();

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto, ct);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.Equal(PostM.Success.PostUpdatedSuccessfully, result.Message);

            var data = Assert.IsType<PostAdminDetailsDto>(result.Value);
            Assert.Equal(dto.Title, data.Title);
            Assert.Equal(dto.Slug, data.Slug);

            await _mockRepository.Received(1).SaveChangesAsync(ct);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldStripHtmlFromTitleAndSlug()
        {
            // Arrange
            const int postId = 1;
            var ct = CancellationToken.None;
            ArrangeSuccessfulUpdate(postId, OldPreviewUrl);

            var dto = TestDataHelper.GetPostUpdateDto(
                title: "<h1>Clean Title</h1>",
                slug: "<b>clean-slug</b>");

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto, ct);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Clean Title", result.Value!.Title);
            Assert.Equal("clean-slug", result.Value.Slug);

            await _mockRepository.Received(1).SaveChangesAsync(ct);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReplacePreviewAndDeleteOld_WhenNewFileProvided()
        {
            // Arrange
            const int postId = 1;
            var post = ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            var file = Substitute.For<IFormFile>();
            var dto = TestDataHelper.GetPostUpdateDto(slug: "<b>clean-slug</b>", previewImage: file);

            _mockFileStorageService.SavePostPreviewAsync(file, "clean-slug")
                .Returns(Result<string>.Success(NewPreviewUrl));
            _mockFileStorageService.DeleteByUrlAsync(OldPreviewUrl).Returns(Result.Success());

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(NewPreviewUrl, post.PreviewImageUrl);
            Assert.Equal(NewPreviewUrl, result.Value!.PreviewImageUrl);

            await _mockFileStorageService.Received(1).SavePostPreviewAsync(file, "clean-slug");
            await _mockFileStorageService.Received(1).DeleteByUrlAsync(OldPreviewUrl);
            await _mockRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldKeepPreview_WhenNoFileProvided()
        {
            // Arrange
            const int postId = 1;
            var post = ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            var dto = TestDataHelper.GetPostUpdateDto();

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(OldPreviewUrl, post.PreviewImageUrl);

            await _mockFileStorageService.DidNotReceive()
                .SavePostPreviewAsync(Arg.Any<IFormFile>(), Arg.Any<string>());
            await _mockFileStorageService.DidNotReceive().DeleteByUrlAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnInvalid_WhenImageUploadFails()
        {
            // Arrange
            const int postId = 1;
            var post = ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            var dto = TestDataHelper.GetPostUpdateDto(previewImage: Substitute.For<IFormFile>());

            _mockFileStorageService.SavePostPreviewAsync(Arg.Any<IFormFile>(), Arg.Any<string>())
                .Returns(Result<string>.Error(ImageM.Errors.UploadFailed, ImageM.Errors.UploadFailedCode));

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Invalid, result.Status);
            Assert.Equal(ImageM.Errors.UploadFailed, result.Message);
            Assert.Equal(OldPreviewUrl, post.PreviewImageUrl);

            await _mockRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
            await _mockFileStorageService.DidNotReceive().DeleteByUrlAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldStillSucceed_WhenDeletingOldPreviewFails()
        {
            // Arrange
            const int postId = 1;
            var post = ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            var dto = TestDataHelper.GetPostUpdateDto(previewImage: Substitute.For<IFormFile>());

            _mockFileStorageService.SavePostPreviewAsync(Arg.Any<IFormFile>(), Arg.Any<string>())
                .Returns(Result<string>.Success(NewPreviewUrl));
            _mockFileStorageService.DeleteByUrlAsync(OldPreviewUrl)
                .Returns(Result.Error(ImageM.Errors.DeleteFailed, ImageM.Errors.DeleteFailedCode));

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(NewPreviewUrl, post.PreviewImageUrl);
            await _mockRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldNotUploadImage_WhenSlugConflicts()
        {
            // Arrange
            const int postId = 1;
            ArrangeSuccessfulUpdate(postId, OldPreviewUrl);
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var dto = TestDataHelper.GetPostUpdateDto(previewImage: Substitute.For<IFormFile>());

            // Act
            var result = await _adminPostService.UpdatePostAsync(postId, dto);

            // Assert
            Assert.Equal(ResultStatus.Conflict, result.Status);
            await _mockFileStorageService.DidNotReceive()
                .SavePostPreviewAsync(Arg.Any<IFormFile>(), Arg.Any<string>());
        }
    }
}