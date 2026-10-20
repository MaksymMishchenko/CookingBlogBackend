using MockQueryable;
using PostApiService.Infrastructure.Common;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Tests.UnitTests.Services.AdminPostServiceTests
{
    public class GetPostByIdAsyncTests : AdminPostServiceTestsBase
    {
        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnSuccessResult_WithCorrectData_WhenPostExists()
        {
            // Arrange
            int postId = 2;
            var token = CancellationToken.None;

            _mockWebContext.UserId.Returns(TestUserData.AdminId);
            _mockWebContext.IsAdmin.Returns(true);
            _mockWebContext.IsContributor.Returns(false);

            var categories = TestDataHelper.GetCulinaryCategories();

            string[] authorIds = new[] { TestUserData.AdminId, TestUserData.ContributorId };
            var testPosts = TestDataHelper.GetPostsWithComments(count: 5, categories, authorIds: authorIds, generateComments: false, generateIds: true);

            var expectedPost = testPosts.First(p => p.Id == postId);

            expectedPost.Author = new Microsoft.AspNetCore.Identity.IdentityUser
            {
                Id = expectedPost.AuthorId,
                UserName = TestUserData.AdminUserName
            };

            var mockQueryable = testPosts.AsQueryable().BuildMock();
            _mockRepository.AsQueryable().Returns(mockQueryable);

            // Act
            var result = await _adminPostService.GetPostByIdAsync(postId, token);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.NotNull(result.Value);
            Assert.IsType<PostAdminDetailsDto>(result.Value);

            Assert.Equal(expectedPost.Id, result.Value.Id);
            Assert.Equal(expectedPost.Title, result.Value.Title);
            Assert.Equal(expectedPost.Author.UserName, result.Value.Author);

            _mockRepository.Received(1).AsQueryable();
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnNotFound_WhenPostDoesNotExist()
        {
            // Arrange
            int nonExistentId = 999;

            _mockWebContext.UserId.Returns(TestUserData.AdminId);
            _mockWebContext.IsAdmin.Returns(true);
            _mockWebContext.IsContributor.Returns(false);

            var testPosts = new List<Post>().AsQueryable().BuildMock();
            _mockRepository.AsQueryable().Returns(mockQueryable => testPosts);

            var errorMessage = PostM.Errors.PostNotFound;
            var errorCode = PostM.Errors.PostNotFoundCode;

            // Act
            var result = await _adminPostService.GetPostByIdAsync(nonExistentId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal(errorMessage, result.Message);
            Assert.Equal(errorCode, result.ErrorCode);

            _mockRepository.Received(1).AsQueryable();
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnForbidden_WhenUserIsNotAdminOrContributor()
        {
            // Arrange
            int postId = 1;
            _mockWebContext.UserId.Returns("regular-user-id");
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IsContributor.Returns(false);

            // Act
            var result = await _adminPostService.GetPostByIdAsync(postId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Forbidden, result.Status);
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnForbidden_WhenContributorRequestsForeignPost()
        {
            // Arrange
            int postId = 1;
            string contributorId = "contributor-1-id";
            string anotherAuthorId = "contributor-2-id";

            _mockWebContext.UserId.Returns(contributorId);
            _mockWebContext.IsAdmin.Returns(false);
            _mockWebContext.IsContributor.Returns(true);

            var categories = TestDataHelper.GetCulinaryCategories();
            var testPosts = TestDataHelper.GetPostsWithComments(count: 1, categories, authorIds: new[] { anotherAuthorId }, generateComments: false, generateIds: true);
            testPosts.First().Id = postId;

            var mockQueryable = testPosts.AsQueryable().BuildMock();
            _mockRepository.AsQueryable().Returns(mockQueryable);

            // Act
            var result = await _adminPostService.GetPostByIdAsync(postId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Forbidden, result.Status);
        }
    }
}
