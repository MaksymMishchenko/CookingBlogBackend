using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PostApiService.Controllers;
using PostApiService.Interfaces;
using PostApiService.Models;

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
            Assert.Equal("Parameters must be greater than 0.", response.Message);
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnBadRequest_WhenPageSizeExceedsLimit()
        {
            // Act
            var result = await _postsController.GetAllPostsAsync(pageNumber: 1, pageSize: 11);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<PostResponse>(badRequestResult.Value);
            Assert.Equal("Page size or comments per page exceeds the allowed maximum.", response.Message);
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnNotFound_WhenNoPostsFound()
        {
            // Arrange
            _mockPostService.Setup(service => service.GetAllPostsAsync(It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestDataHelper.GetEmptyPostList());

            // Act
            var result = await _postsController.GetAllPostsAsync(pageNumber: 1, pageSize: 10);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<PostResponse>(notFoundResult.Value);
            Assert.Equal("No posts found for the requested page.", response.Message);

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
            var returnValue = Assert.IsType<List<Post>>(okResult.Value);
            Assert.Equal(10, returnValue.Count);

            _mockPostService.Verify(s => s.GetAllPostsAsync(
                1,
                10,
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnInternalServerError_WhenExceptionIsThrown()
        {
            // Act
            var result = await _postsController.GetAllPostsAsync(pageNumber: 1, pageSize: 10);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);

            var response = Assert.IsType<PostResponse>(statusCodeResult.Value);
            Assert.Equal("An error occurred while processing your request.", response.Message);

            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnBadRequest_IfParameterIsInvalid()
        {
            // Act
            var result = await _postsController.GetPostByIdAsync(-1, true);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<PostResponse>(badRequestResult.Value);

            Assert.Equal(400, badRequestResult.StatusCode);
            Assert.Equal("Parameters must be greater than 0.", response.Message);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        public async Task GetPostByIdAsync_ShouldNotReturnBadRequest_IfParameterIsValid(int postId)
        {
            //Arrange
            _mockPostService
                 .Setup(s => s.GetPostByIdAsync(postId, true))
                 .ReturnsAsync(TestDataHelper.GetSinglePost());

            // Act
            var result = await _postsController.GetPostByIdAsync(postId);

            // Assert
            Assert.IsNotType<BadRequestObjectResult>(result);
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
            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal(expectedPost, okResult.Value);

            _mockPostService.Verify(s => s.GetPostByIdAsync(
                expectedPost.PostId,
                true), Times.Once);
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnNotFound_IfPostDoesNotExist()
        {
            // Arrange
            var postId = 999;

            _mockPostService.Setup(s => s.GetPostByIdAsync(postId, It.IsAny<bool>()))
                .ThrowsAsync(new KeyNotFoundException($"Post with id {postId} not found."));

            // Act
            var result = await _postsController.GetPostByIdAsync(postId, true);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<PostResponse>(notFoundResult.Value);

            Assert.Equal(404, notFoundResult.StatusCode);
            Assert.Equal($"Post with id {postId} not found.", response.Message);

            _mockPostService.Verify(s => s.GetPostByIdAsync(
                postId,
                true), Times.Once);
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnNotFound_IfKeyNotFoundException()
        {
            // Arrange
            var postId = 999;

            _mockPostService.Setup(s => s.GetPostByIdAsync(postId, It.IsAny<bool>()))
                .ThrowsAsync(new KeyNotFoundException($"Post with id {postId} not found."));

            // Act
            var result = await _postsController.GetPostByIdAsync(postId, true);

            // Assert
            var objectResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<PostResponse>(objectResult.Value);

            Assert.Equal(404, objectResult.StatusCode);
            Assert.Equal($"Post with id {postId} not found.", response.Message);

            _mockPostService.Verify(s => s.GetPostByIdAsync(
                postId,
                true), Times.Once);

            _mockLogger.Verify(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnInternalServerError_IfUnexpectedErrorOccurs()
        {
            // Arrange
            var postId = 999;

            _mockPostService.Setup(s => s.GetPostByIdAsync(postId, It.IsAny<bool>()))
                .ThrowsAsync(new Exception($"An error occurred while processing request to get post by id {postId}."));

            // Act
            var result = await _postsController.GetPostByIdAsync(postId, true);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<PostResponse>(objectResult.Value);

            Assert.Equal(500, objectResult.StatusCode);
            Assert.Equal($"An error occurred while processing request to get post by id {postId}.", response.Message);

            _mockPostService.Verify(s => s.GetPostByIdAsync(
                postId,
                true), Times.Once);

            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnBadRequest_IfPostIsNull()
        {
            // Act
            var result = await _postsController.AddPostAsync((Post)null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<PostResponse>(badRequestResult.Value);

            Assert.Equal(400, badRequestResult.StatusCode);
            Assert.Equal("Post cannot be null.", response.Message);
        }

        [Theory]
        [MemberData(nameof(ModelValidationHelper.GetPostTestData), MemberType = typeof(ModelValidationHelper))]
        public async Task AddPostAsync_ShouldReturnBadRequest_WhenModelIsInvalid(Post post, bool expectedIsValid)
        {
            // Arrange
            var controller = new PostsController(_mockPostService.Object, _mockLogger.Object);

            ModelValidationHelper.ValidateModel(post, controller);

            // Act
            var result = await controller.AddPostAsync(post);

            // Assert
            if (!expectedIsValid)
            {
                var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
                var response = Assert.IsType<PostResponse>(badRequestResult.Value);

                Assert.Equal("Validation failed.", response.Message);

                foreach (var validationResult in controller.ModelState.Values.SelectMany(v => v.Errors))
                {
                    Assert.Contains(validationResult.ErrorMessage, response.Errors.Values.SelectMany(errors => errors));
                }
            }
        }

        [Theory]
        [InlineData(true, 201, "Post added successfully.")]
        [InlineData(false, 409, "A post with this title already exists.")]
        public async Task AddPostAsync_ShouldReturnExpectedResult_BasedOnPostAddition(bool isPostAdded,
            int expectedStatusCode,
            string expectedMessage)
        {
            // Arrange
            var post = TestDataHelper.GetSinglePost();

            _mockPostService.Setup(s => s.AddPostAsync(post))
                .ReturnsAsync(isPostAdded ? post : null);

            // Act
            var result = await _postsController.AddPostAsync(post);

            // Assert
            if (isPostAdded)
            {
                var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
                var response = Assert.IsType<PostResponse>(createdAtActionResult.Value);

                Assert.Equal(expectedStatusCode, createdAtActionResult.StatusCode);
                Assert.Equal(expectedMessage, response.Message);

                _mockPostService.Verify(s => s.AddPostAsync(post), Times.Once);
            }
            else
            {
                var conflictObjectResult = Assert.IsType<ConflictObjectResult>(result);
                var response = Assert.IsType<PostResponse>(conflictObjectResult.Value);

                Assert.Equal(expectedStatusCode, conflictObjectResult.StatusCode);
                Assert.Equal(expectedMessage, response.Message);

                _mockPostService.Verify(s => s.AddPostAsync(post), Times.Once);
            }
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnConflict_IfInvalidOperationException()
        {
            // Arrange
            var exceptionMsg = "Failed to add post.";

            _mockPostService.Setup(s => s.AddPostAsync(It.IsAny<Post>()))
                .ThrowsAsync(new InvalidOperationException(exceptionMsg));

            // Act
            var result = await _postsController.AddPostAsync(new Post());

            // Assert
            var conflictObjectResult = Assert.IsType<ConflictObjectResult>(result);
            var response = Assert.IsType<PostResponse>(conflictObjectResult.Value);

            Assert.Equal(409, conflictObjectResult.StatusCode);
            Assert.Equal(exceptionMsg, response.Message);

            _mockPostService.Verify(s => s.AddPostAsync(It.IsAny<Post>()), Times.Once);

            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnInternalServerError_IfAnUnexpectedErrorOccurs()
        {
            // Arrange
            var exceptionMsg = "An unexpected error occurred while adding post";

            _mockPostService.Setup(s => s.AddPostAsync(It.IsAny<Post>()))
                .ThrowsAsync(new Exception(exceptionMsg));

            // Act
            var result = await _postsController.AddPostAsync(new Post());

            // Assert
            var iseObjectResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<PostResponse>(iseObjectResult.Value);

            Assert.Equal(500, iseObjectResult.StatusCode);
            Assert.Equal(exceptionMsg, response.Message);

            _mockPostService.Verify(s => s.AddPostAsync(It.IsAny<Post>()), Times.Once);

            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData(-1, "Post cannot be null, and ID should be greater than 0.")]
        [InlineData(0, "Post cannot be null, and ID should be greater than 0.")]
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

            Assert.Equal(400, badRequestResult.StatusCode);
            Assert.Equal(expectedMessage, response.Message);
        }

        [Theory]
        [MemberData(nameof(ModelValidationHelper.GetPostTestData), MemberType = typeof(ModelValidationHelper))]
        public async Task UpdatePostAsync_ShouldReturnBadRequest_WhenModelIsInvalid(Post post,
            bool expectedIsValid)
        {
            // Arrange
            var controller = new PostsController(_mockPostService.Object, _mockLogger.Object);

            ModelValidationHelper.ValidateModel(post, controller);

            // Act
            var result = await controller.UpdatePostAsync(post);

            // Assert
            if (!expectedIsValid)
            {
                var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
                var response = Assert.IsType<PostResponse>(badRequestResult.Value);

                Assert.Equal("Validation failed.", response.Message);

                foreach (var validationResult in controller.ModelState.Values.SelectMany(v => v.Errors))
                {
                    Assert.Contains(validationResult.ErrorMessage, response.Errors.Values.SelectMany(errors => errors));
                }
            }
        }

        [Fact]        
        public async Task UpdatePostAsync_ShouldReturnExpectedResult_WhenPostIsUpdated()
        {
            // Arrange
            var post = TestDataHelper.GetSinglePost();
            _mockPostService.Setup(service => service.UpdatePostAsync(post))
                .Returns(Task.CompletedTask);

            var controller = new PostsController(_mockPostService.Object, _mockLogger.Object);

            // Act
            var result = await controller.UpdatePostAsync(post);

            // Assert            
            var okObjectResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okObjectResult.StatusCode);

            _mockPostService.Verify(s => s.UpdatePostAsync(It.IsAny<Post>()), Times.Once);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnNotFound_IfPostNotFound()
        {
            // Arrange
            var post = TestDataHelper.GetSinglePost();
            var exceptionMsg = $"Post with ID {post.PostId} not found. Please check the Post ID.";

            _mockPostService.Setup(s => s.UpdatePostAsync(It.IsAny<Post>()))
                .ThrowsAsync(new KeyNotFoundException(exceptionMsg));

            // Act 
            var result = await _postsController.UpdatePostAsync(post);

            //Assert
            var notFoundObjectResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<PostResponse>(notFoundObjectResult.Value);

            Assert.Equal(404, notFoundObjectResult.StatusCode);
            Assert.Equal(exceptionMsg, response.Message);

            _mockPostService.Verify(s => s.UpdatePostAsync(It.IsAny<Post>()), Times.Once);

            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnConflict_WhenPostNotUpdated()
        {
            // Arrange
            var post = TestDataHelper.GetSinglePost();
            var exceptionMsg = $"No changes were made to post with ID {post.PostId}.";

            _mockPostService.Setup(s => s.UpdatePostAsync(It.IsAny<Post>()))
                .ThrowsAsync(new InvalidOperationException(exceptionMsg));

            // Act 
            var result = await _postsController.UpdatePostAsync(post);

            //Assert
            var conflictObjectResult = Assert.IsType<ConflictObjectResult>(result);
            var response = Assert.IsType<PostResponse>(conflictObjectResult.Value);

            Assert.Equal(409, conflictObjectResult.StatusCode);
            Assert.Equal(exceptionMsg, response.Message);

            _mockPostService.Verify(s => s.UpdatePostAsync(It.IsAny<Post>()), Times.Once);

            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnInternalServerError_WhenUnexpectedErrorOccurs()
        {
            // Arrange
            var post = TestDataHelper.GetSinglePost();
            var exceptionMsg = "An unexpected error occurred. Please try again later.";

            _mockPostService.Setup(s => s.UpdatePostAsync(It.IsAny<Post>()))
                .ThrowsAsync(new Exception(exceptionMsg));

            // Act 
            var result = await _postsController.UpdatePostAsync(post);

            // Assert
            var internalObjectResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<PostResponse>(internalObjectResult.Value);

            Assert.Equal(500, internalObjectResult.StatusCode);
            Assert.Equal(exceptionMsg, response.Message);

            _mockPostService.Verify(s => s.UpdatePostAsync(It.IsAny<Post>()), Times.Once);

            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
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