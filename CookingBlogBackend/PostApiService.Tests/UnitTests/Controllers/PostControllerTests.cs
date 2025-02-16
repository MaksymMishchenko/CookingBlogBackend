using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PostApiService.Controllers;
using PostApiService.Exceptions;
using PostApiService.Interfaces;
using PostApiService.Models;
using System.Net;

namespace PostApiService.Tests.UnitTests.Controllers
{
    public class PostControllerTests
    {
        private readonly Mock<ILogger<PostsController>> _mockLogger;
        private readonly Mock<IPostService> _mockPostService;
        private readonly PostsController _postsController;

        public PostControllerTests()
        {
            _mockLogger = new Mock<ILogger<PostsController>>();
            _mockPostService = new Mock<IPostService>();
            _postsController = new PostsController(_mockPostService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task OnGetAllPostsAsync_ShouldReturnBadRequest_WhenParametersAreInvalid()
        {
            // Act
            var result = await _postsController.GetAllPostsAsync(-1, 10);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Post>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal(PostErrorMessages.InvalidPageParameters, response.Message);
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnBadRequest_WhenPageSizeExceedsLimit()
        {
            // Act
            var result = await _postsController.GetAllPostsAsync(pageNumber: 1, pageSize: 11);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Post>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal(PostErrorMessages.PageSizeExceeded, response.Message);
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnNotFound_WhenPostsAreNotFound()
        {
            // Arrange            
            _mockPostService.Setup(service => service.GetAllPostsAsync(1,
                10,
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestDataHelper.GetEmptyPostList());

            // Act
            var result = await _postsController.GetAllPostsAsync(pageNumber: 1, pageSize: 10);

            // Assert
            var notFoundObjectResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Post>>(notFoundObjectResult.Value);
            Assert.False(response.Success);
            Assert.Equal(PostErrorMessages.NoPostsFound, response.Message);

            _mockPostService.Verify(s => s.GetAllPostsAsync(
                1,
                10,
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnOk_WhenPostsAreFound()
        {
            // Arrange            
            _mockPostService.Setup(service => service.GetAllPostsAsync(It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestDataHelper.GetPostsWithComments(count: 10, generateComments: false));

            // Act
            var result = await _postsController.GetAllPostsAsync(pageNumber: 1, pageSize: 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Post>>(okResult.Value);
            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(string.Format
                (PostSuccessMessages.PostsRetrievedSuccessfully, response.DataList.Count), response.Message);
            Assert.Equal(10, response.DataList.Count);

            _mockPostService.Verify(s => s.GetAllPostsAsync(
                1,
                10,
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnBadRequest_IfParameterIsInvalid()
        {
            // Act
            var result = await _postsController.GetPostByIdAsync(-1, true);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Post>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.Equal((int)HttpStatusCode.BadRequest, badRequestResult.StatusCode);
            Assert.Equal(PostErrorMessages.InvalidPageParameters, response.Message);            
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnStatusCode200_IfPostExists()
        {
            // Arrange
            var expectedPost = TestDataHelper.GetSinglePost();
            _mockPostService.Setup(s => s.GetPostByIdAsync(expectedPost.PostId, true))
                .ReturnsAsync(expectedPost);

            // Act
            var result = await _postsController.GetPostByIdAsync
                (expectedPost.PostId, true);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Post>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal(string.Format
                (PostSuccessMessages.PostRetrievedSuccessfully, expectedPost.PostId), response.Message);
            Assert.Equal((int)HttpStatusCode.OK, okResult.StatusCode);

            _mockPostService.Verify(s => s.GetPostByIdAsync(
                expectedPost.PostId,
                true), Times.Once);
        }

        [Theory]
        [MemberData(nameof(ModelValidationHelper.GetPostTestData), MemberType = typeof(ModelValidationHelper))]
        public async Task AddPostAsync_ShouldReturnBadRequest_WhenModelIsInvalid(Post post)
        {
            // Arrange            
            ModelValidationHelper.ValidateModel(post, _postsController);

            // Act
            var result = await _postsController.AddPostAsync(post);

            // Assert            
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Post>>(badRequestResult.Value);

            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Equal((int)HttpStatusCode.BadRequest, badRequestResult.StatusCode);
            Assert.Equal(PostErrorMessages.ValidationFailed, response.Message);
            Assert.NotEmpty(response.Errors);

            foreach (var validationResult in _postsController.ModelState.Values.SelectMany(v => v.Errors))
            {
                Assert.Contains(validationResult.ErrorMessage, response.Errors.Values.SelectMany(errors => errors));
            }
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturn201AndSuccessMessage_WhenPostIsAddedSuccessfully()
        {
            // Arrange
            var post = TestDataHelper.GetSinglePost();

            _mockPostService.Setup(s => s.AddPostAsync(post))
                .ReturnsAsync(post);

            // Act
            var result = await _postsController.AddPostAsync(post);

            // Assert            
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            var response = Assert.IsType<ApiResponse<Post>>(createdAtActionResult.Value);

            Assert.True(response.Success);
            Assert.Equal((int)HttpStatusCode.Created, createdAtActionResult.StatusCode);
            Assert.Equal(string.Format
                (PostSuccessMessages.PostAddedSuccessfully, post.PostId), response.Message);

            _mockPostService.Verify(s => s.AddPostAsync(post), Times.Once);
        }

        [Theory]
        [InlineData(-1, PostErrorMessages.InvalidPostOrId)]
        [InlineData(0, PostErrorMessages.InvalidPostOrId)]
        public async Task UpdatePostAsync_ShouldReturnBadRequest_IfPostIsNullOrIdLessOrEqualZero(int postId,
            string expectedMessage)
        {
            // Arrange
            Post post = postId > 0 ? new Post { PostId = postId } : null;

            // Act
            var result = await _postsController.UpdatePostAsync(post);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Post>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.Equal(expectedMessage, response.Message);
            Assert.Equal((int)HttpStatusCode.BadRequest, badRequestResult.StatusCode);
        }

        [Theory]
        [MemberData(nameof(ModelValidationHelper.GetPostTestData), MemberType = typeof(ModelValidationHelper))]
        public async Task UpdatePostAsync_ShouldReturnBadRequest_WhenModelIsInvalid(Post post)
        {
            // Arrange           
            ModelValidationHelper.ValidateModel(post, _postsController);

            // Act
            var result = await _postsController.UpdatePostAsync(post);

            // Assert            
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Post>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.Equal(PostErrorMessages.ValidationFailed, response.Message);
            Assert.NotEmpty(response.Errors);

            foreach (var validationResult in _postsController.ModelState.Values.SelectMany(v => v.Errors))
            {
                Assert.Contains(validationResult.ErrorMessage, response.Errors.Values.SelectMany(errors => errors));
            }
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnExpectedResult_WhenPostIsUpdated()
        {
            // Arrange
            var post = TestDataHelper.GetSinglePost();
            _mockPostService.Setup(service => service.UpdatePostAsync(post))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _postsController.UpdatePostAsync(post);

            // Assert            
            var okObjectResult = Assert.IsType<OkObjectResult>(result);
            var actualResponse = Assert.IsType<ApiResponse<Post>>(okObjectResult.Value);

            Assert.True(actualResponse.Success);
            Assert.Equal(string.Format
                (PostSuccessMessages.PostUpdatedSuccessfully, post.PostId), actualResponse.Message);
            Assert.Equal(post.PostId, actualResponse.EntityId);

            Assert.Equal((int)HttpStatusCode.OK, okObjectResult.StatusCode);

            _mockPostService.Verify(s => s.UpdatePostAsync(post), Times.Once);
        }

        [Theory]
        [InlineData(0, PostErrorMessages.InvalidPostIdParameter)]
        [InlineData(-1, PostErrorMessages.InvalidPostIdParameter)]
        public async Task DeletePostAsync_ShouldReturnBadRequest_IfParameterIsInvalid(
            int postId,
            string expectedMessage)
        {
            // Act
            var result = await _postsController.DeletePostAsync(postId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Post>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.Equal(expectedMessage, response.Message);
            Assert.Equal((int)HttpStatusCode.BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturnOk_IfPostRemovedSuccessfully()
        {
            // Arrange
            var postId = 1;
            _mockPostService
                 .Setup(s => s.DeletePostAsync(postId))
                 .Returns(Task.CompletedTask);

            // Act
            var result = await _postsController.DeletePostAsync(postId);

            // Assert
            var okObjectResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Post>>(okObjectResult.Value);

            Assert.True(response.Success);
            Assert.Equal((int)HttpStatusCode.OK, okObjectResult.StatusCode);
            Assert.Equal(string.Format
                (PostSuccessMessages.PostDeletedSuccessfully, postId), response.Message);

            _mockPostService.Verify(s => s.DeletePostAsync(postId), Times.Once);
        }
    }
}