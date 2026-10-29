using Microsoft.AspNetCore.Http;
using PostApiService.Helper;
using PostApiService.Infrastructure.Common;
using System.Linq.Expressions;

namespace PostApiService.Tests.UnitTests.Services.AdminPostServiceTests
{
    public class AddPostAsyncTests : AdminPostServiceTestsBase
    {
        [Fact]
        public async Task AddPostAsync_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange            
            var postCreateDto = TestDataHelper.GetPostCreateDto();

            _mockWebContext.UserId.Returns(string.Empty);

            // Act
            var result = await _adminPostService.AddPostAsync(postCreateDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Unauthorized, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccess, result.Message);
            Assert.Equal(Auth.LoginM.Errors.UnauthorizedAccessCode, result.ErrorCode);

            await _mockRepository.DidNotReceive().AddAsync(Arg.Any<Post>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnForbidden_WhenUserIsNotAdminOrContributor()
        {
            // Arrange            
            var postCreateDto = TestDataHelper.GetPostCreateDto();

            _mockWebContext.UserId.Returns("user-id");
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IsContributor.Returns(false);

            // Act
            var result = await _adminPostService.AddPostAsync(postCreateDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Forbidden, result.Status);
            Assert.Null(result.Value);

            await _mockRepository.DidNotReceive().AddAsync(Arg.Any<Post>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnInvalid_WhenPostIsEmpty()
        {
            // Arrange
            const string invalidPostContent = "<script></script>";
            var postCreateDto = TestDataHelper.GetPostCreateDto(invalidPostContent);

            _mockWebContext.UserId.Returns("3f2504e0-4f89-11d3-9a0c-0305e82c3301");
            _mockWebContext.IsContributor.Returns(true);
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns(string.Empty);

            // Act
            var result = await _adminPostService.AddPostAsync(postCreateDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Invalid, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(PostM.Errors.Empty, result.Message);
            Assert.Equal(PostM.Errors.EmptyCode, result.ErrorCode);

            await _mockRepository.DidNotReceive().AddAsync(Arg.Any<Post>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnConflict_WhenTitleAlreadyExists()
        {
            // Arrange
            var postCreateDto = TestDataHelper.GetPostCreateDto(title: "Existing Title");

            _mockWebContext.UserId.Returns("user-id");
            _mockWebContext.IsAdmin.Returns(true);
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns("Safe content");
            
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true, false);

            // Act
            var result = await _adminPostService.AddPostAsync(postCreateDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(string.Format(PostM.Errors.PostTitleAlreadyExist, postCreateDto.Title), result.Message);
            Assert.Equal(PostM.Errors.PostTitleAlreadyExistCode, result.ErrorCode);

            await _mockRepository.DidNotReceive().AddAsync(Arg.Any<Post>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnConflict_WhenSlugAlreadyExists()
        {
            // Arrange
            var postCreateDto = TestDataHelper.GetPostCreateDto(slug: "existing-slug");

            _mockWebContext.UserId.Returns("user-id");
            _mockWebContext.IsAdmin.Returns(true);
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns("Safe content");
           
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(false, true);

            // Act
            var result = await _adminPostService.AddPostAsync(postCreateDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(string.Format(PostM.Errors.PostSlugAlreadyExist, postCreateDto.Slug), result.Message);
            Assert.Equal(PostM.Errors.PostSlugAlreadyExistCode, result.ErrorCode);

            await _mockRepository.DidNotReceive().AddAsync(Arg.Any<Post>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnNotFound_WhenCategoryDoesNotExists()
        {
            // Arrange
            var postCreateDto = TestDataHelper.GetPostCreateDto();

            _mockWebContext.UserId.Returns("user-id");
            _mockWebContext.IsContributor.Returns(true);
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns("Safe content");
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>()).Returns(false);

            _mockCategoryService.ExistsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(false);

            var expectedMessage = CategoryM.Errors.CategoryNotFound;
            var expectedErrorCode = PostM.Errors.CategoryNotFoundCode;

            // Act
            var result = await _adminPostService.AddPostAsync(postCreateDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(expectedMessage, result.Message);
            Assert.Equal(expectedErrorCode, result.ErrorCode);

            await _mockRepository.Received(2)
                .AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>());

            await _mockCategoryService.Received(1)
                .ExistsAsync(Arg.Is(postCreateDto.CategoryId!.Value), Arg.Any<CancellationToken>());

            await _mockRepository.DidNotReceive().AddAsync(Arg.Any<Post>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddPostAsync_ShouldStripHtmlFromTitleAndSlug()
        {
            // Arrange            
            var postCreateDto = TestDataHelper.GetPostCreateDto(
                title: "<h1>Clean Title</h1>",
                slug: "<b>clean-slug</b>"
            );

            var ct = CancellationToken.None;

            _mockWebContext.UserId.Returns("user-id");
            _mockWebContext.IsAdmin.Returns(true);
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns("Safe content");
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), ct).Returns(false);
            _mockCategoryService.ExistsAsync(Arg.Any<int>(), ct).Returns(true);

            _mockFileStorageService.SavePostPreviewAsync(Arg.Any<IFormFile>(), Arg.Any<string>())
                .Returns(Result<string>.Success("/images/posts/clean-slug.webp"));

            var savedPost = TestDataHelper.ToEntity(postCreateDto, "Safe content", "user-id");
            savedPost.Title = postCreateDto.Title.StripHtml();
            savedPost.Slug = postCreateDto.Slug.StripHtml();

            _mockRepository.GetByIdWithAuthorsAsync(Arg.Any<int>(), ct)
                .Returns(savedPost);

            // Act
            var result = await _adminPostService.AddPostAsync(postCreateDto, ct);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Clean Title", result.Value!.Title);
            Assert.Equal("clean-slug", result.Value.Slug);

            await _mockRepository.Received(1).AddAsync(Arg.Any<Post>(), ct);
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnSuccess_WhenPostAddedSuccessfully()
        {
            // Arrange                       
            var postCreateDto = TestDataHelper.GetPostCreateDto();
            var token = CancellationToken.None;

            _mockWebContext.UserId.Returns("user-id");
            _mockWebContext.IsContributor.Returns(true);
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns("Safe content");
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), token)
                .Returns(false);
            _mockCategoryService.ExistsAsync(postCreateDto.CategoryId!.Value, token).Returns(true);

            _mockFileStorageService.SavePostPreviewAsync(Arg.Any<IFormFile>(), Arg.Any<string>())
                .Returns(Result<string>.Success("/images/posts/test-preview.webp"));

            var savedPost = TestDataHelper.ToEntity(postCreateDto, "Safe content", "user-id");
            _mockRepository.GetByIdWithAuthorsAsync(Arg.Any<int>(), token)
                .Returns(savedPost);

            // Act
            var result = await _adminPostService.AddPostAsync(postCreateDto, token);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.NotNull(result.Value);

            var data = result.Value!;
            Assert.Equal(postCreateDto.Title, data.Title);
            Assert.Equal(postCreateDto.Slug, data.Slug);
            Assert.Equal(postCreateDto.CategoryId, data.CategoryId);

            Assert.Equal(PostM.Success.PostAddedSuccessfully, result.Message);

            await _mockRepository.Received(1).AddAsync(Arg.Is<Post>(p =>
                 p.Title == postCreateDto.Title &&
                 p.Slug == postCreateDto.Slug &&
                 p.CategoryId == postCreateDto.CategoryId), token);
            await _mockRepository.Received(1).SaveChangesAsync(token);
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnInvalid_WhenImageSaveFails()
        {
            // Arrange
            var postCreateDto = TestDataHelper.GetPostCreateDto();
            var token = CancellationToken.None;

            _mockWebContext.UserId.Returns("user-id");
            _mockWebContext.IsContributor.Returns(true);
            _mockSanitizationService.SanitizePost(Arg.Any<string>()).Returns("Safe content");
            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), token)
                .Returns(false);
            _mockCategoryService.ExistsAsync(postCreateDto.CategoryId!.Value, token)
                .Returns(true);

            var expectedMessage = "Image is too small.";
            var expectedErrorCode = "IMAGE_TOO_SMALL";

            _mockFileStorageService.SavePostPreviewAsync(Arg.Any<IFormFile>(), Arg.Any<string>())
                .Returns(Result<string>.Invalid(expectedMessage, expectedErrorCode));

            // Act
            var result = await _adminPostService.AddPostAsync(postCreateDto, token);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Invalid, result.Status);
            Assert.Null(result.Value);
            Assert.Equal(expectedMessage, result.Message);
            Assert.Equal(expectedErrorCode, result.ErrorCode);

            await _mockRepository.DidNotReceive().AddAsync(Arg.Any<Post>(), token);
            await _mockRepository.DidNotReceive().SaveChangesAsync(token);
        }
    }
}