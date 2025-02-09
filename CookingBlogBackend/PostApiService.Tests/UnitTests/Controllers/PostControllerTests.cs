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
            var response = Assert.IsType<PostResponse>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal(ErrorMessages.InvalidPageParameters, response.Message);
            Assert.Empty(response.Errors);
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnBadRequest_WhenPageSizeExceedsLimit()
        {
            // Act
            var result = await _postsController.GetAllPostsAsync(pageNumber: 1, pageSize: 11);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<PostResponse>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal(ErrorMessages.PageSizeExceeded, response.Message);
            Assert.Empty(response.Errors);
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
            var response = Assert.IsType<PostResponse>(notFoundObjectResult.Value);
            Assert.False(response.Success);
            Assert.Equal(ErrorMessages.NoPostsFound, response.Message);
            Assert.Empty(response.Errors);

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
            var response = Assert.IsType<PostResponse>(okResult.Value);
            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(string.Format
                (SuccessMessages.PostsRetrievedSuccessfully, response.Posts.Count), response.Message);
            Assert.Equal(10, response.Posts.Count);            

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
            var response = Assert.IsType<PostResponse>(badRequestResult.Value);

            Assert.Equal((int)HttpStatusCode.BadRequest, badRequestResult.StatusCode);
            Assert.Equal(ErrorMessages.InvalidPageParameters, response.Message);
            Assert.Empty(response.Errors);
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
            Assert.Equal((int)HttpStatusCode.OK, okResult.StatusCode);
            Assert.Equal(expectedPost, okResult.Value);

            _mockPostService.Verify(s => s.GetPostByIdAsync(
                expectedPost.PostId,
                true), Times.Once);
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnBadRequest_IfPostIsNull()
        {
            // Act
            var result = await _postsController.AddPostAsync(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<PostResponse>(badRequestResult.Value);

            Assert.Equal((int)HttpStatusCode.BadRequest, badRequestResult.StatusCode);
            Assert.Equal(ErrorMessages.PostCannotBeNull, response.Message);
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
            var response = Assert.IsType<PostResponse>(badRequestResult.Value);

            Assert.Equal(ErrorMessages.ValidationFailed, response.Message);

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
            var response = Assert.IsType<PostResponse>(createdAtActionResult.Value);

            Assert.Equal((int)HttpStatusCode.Created, createdAtActionResult.StatusCode);
            Assert.Equal(SuccessMessages.PostAddedSuccessfully, response.Message);

            _mockPostService.Verify(s => s.AddPostAsync(post), Times.Once);
        }

        [Theory]
        [InlineData(-1, ErrorMessages.InvalidPostOrId)]
        [InlineData(0, ErrorMessages.InvalidPostOrId)]
        public async Task UpdatePostAsync_ShouldReturnBadRequest_IfPostIsNullOrIdLessOrEqualZero(int postId,
            string expectedMessage)
        {
            // Arrange
            Post post = postId > 0 ? new Post { PostId = postId } : null;

            // Act
            var result = await _postsController.UpdatePostAsync(post);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<PostResponse>(badRequestResult.Value);

            Assert.Equal((int)HttpStatusCode.BadRequest, badRequestResult.StatusCode);
            Assert.Equal(expectedMessage, response.Message);
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
            var response = Assert.IsType<PostResponse>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.Equal(ErrorMessages.ValidationFailed, response.Message);
            Assert.NotNull(response.Errors);

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
            Assert.Equal((int)HttpStatusCode.OK, okObjectResult.StatusCode);

            var actualResponse = Assert.IsType<PostResponse>(okObjectResult.Value);
            Assert.True(actualResponse.Success);
            Assert.Equal(string.Format
                (SuccessMessages.PostUpdatedSuccessfully, post.PostId), actualResponse.Message);
            Assert.Equal(post.PostId, actualResponse.PostId);

            _mockPostService.Verify(s => s.UpdatePostAsync(post), Times.Once);
        }

        [Theory]
        [InlineData(0, "Parameters must be greater than 0.")]
        [InlineData(-1, "Parameters must be greater than 0.")]
        public async Task DeletePostAsync_ShouldReturnBadRequest_IfParameterIsInvalid(
            int postId,
            string expectedMessage)
        {
            // Act
            var result = await _postsController.DeletePostAsync(postId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<PostResponse>(badRequestResult.Value);

            Assert.Equal(400, badRequestResult.StatusCode);
            Assert.Equal(expectedMessage, response.Message);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        public async Task DeletePostAsync_ShouldNotReturnBadRequest_IfParameterIsValid(int postId)
        {
            //Arrange
            _mockPostService
                 .Setup(s => s.DeletePostAsync(postId))
                 .Returns(Task.CompletedTask);

            // Act
            var result = await _postsController.DeletePostAsync(postId);

            // Assert
            Assert.IsNotType<BadRequestObjectResult>(result);

            _mockPostService.Verify(s => s.DeletePostAsync(postId), Times.Once);
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
            var response = Assert.IsType<PostResponse>(okObjectResult.Value);

            Assert.Equal(200, okObjectResult.StatusCode);
            Assert.Equal("Post was deleted successfully.", response.Message);

            _mockPostService.Verify(s => s.DeletePostAsync(postId), Times.Once);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturnNotFound_IfPostNotFound()
        {
            // Arrange
            var post = TestDataHelper.GetSinglePost();
            var exceptionMsg = $"Post with ID {post.PostId} not found. Please check the Post ID.";

            _mockPostService.Setup(s => s.DeletePostAsync(It.IsAny<int>()))
                .ThrowsAsync(new KeyNotFoundException(exceptionMsg));

            // Act 
            var result = await _postsController.DeletePostAsync(post.PostId);

            //Assert
            var notFoundObjectResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<PostResponse>(notFoundObjectResult.Value);

            Assert.Equal(404, notFoundObjectResult.StatusCode);
            Assert.Equal(exceptionMsg, response.Message);

            _mockPostService.Verify(s => s.DeletePostAsync(post.PostId), Times.Once);

            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturnConflict_IfPostNotDeleted()
        {
            // Arrange
            var post = TestDataHelper.GetSinglePost();
            var exceptionMsg = $"Failed to delete post with ID {post.PostId}.";

            _mockPostService.Setup(s => s.DeletePostAsync(It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException(exceptionMsg));

            // Act 
            var result = await _postsController.DeletePostAsync(post.PostId);

            //Assert
            var conflictObjectResult = Assert.IsType<ConflictObjectResult>(result);
            var response = Assert.IsType<PostResponse>(conflictObjectResult.Value);

            Assert.Equal(409, conflictObjectResult.StatusCode);
            Assert.Equal(exceptionMsg, response.Message);

            _mockPostService.Verify(s => s.DeletePostAsync(post.PostId), Times.Once);

            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldReturnInternalServerError_WhenUnexpectedErrorOccurs()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            var post = TestDataHelper.GetSinglePost();

            _mockPostService.Setup(s => s.DeletePostAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("An unexpected error occurred"));

            // Act 
            var result = await _postsController.DeletePostAsync(post.PostId);

            //Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<PostResponse>(objectResult.Value);

            Assert.Equal(500, objectResult.StatusCode);
            Assert.Contains("An unexpected error occurred", response.Message);
            Assert.Contains("Request ID:", response.Message);

            _mockPostService.Verify(s => s.DeletePostAsync(post.PostId), Times.Once);

            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}