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
            _mockPostService.Setup(service => service.GetAllPostsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(TestDataHelper.GetEmptyPostList());

            // Act
            var result = await _postsController.GetAllPostsAsync(pageNumber: 1, pageSize: 10);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<PostResponse>(notFoundResult.Value);
            Assert.Equal("No posts found for the requested page.", response.Message);
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnOk_WhenPostsAreFound()
        {
            // Arrange            
            _mockPostService.Setup(service => service.GetAllPostsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(TestDataHelper.GetPostsWithComments(count: 10, generateComments: false));

            // Act
            var result = await _postsController.GetAllPostsAsync(pageNumber: 1, pageSize: 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<List<Post>>(okResult.Value);
            Assert.Equal(10, returnValue.Count);
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnInternalServerError_WhenExceptionIsThrown()
        {
            // Arrange
            _mockPostService.Setup(service => service.GetAllPostsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception("An error occurred while processing your request."));

            // Act
            var result = await _postsController.GetAllPostsAsync(pageNumber: 1, pageSize: 10);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);

            var response = Assert.IsType<PostResponse>(statusCodeResult.Value);
            Assert.Equal("An error occurred while processing your request.", response.Message);
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

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnStatusCode200_IfPostExists()
        {
            // Arrange
            var expectedPost = TestDataHelper.GetSinglePost();
            _mockPostService.Setup(s => s.GetPostByIdAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(expectedPost);

            // Act
            var result = await _postsController.GetPostByIdAsync(1, true);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal(expectedPost, okResult.Value);
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnNotFound_IfPostDoesNotExist()
        {
            // Arrange
            var postId = 999;

            _mockPostService.Setup(s => s.GetPostByIdAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .ThrowsAsync(new KeyNotFoundException($"Post with id {postId} not found."));

            // Act
            var result = await _postsController.GetPostByIdAsync(postId, true);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<PostResponse>(notFoundResult.Value);

            Assert.Equal(404, notFoundResult.StatusCode);
            Assert.Equal($"Post with id {postId} not found.", response.Message);
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnInternalServerError_IfUnexpectedErrorOccurs()
        {
            // Arrange
            var postId = 999;

            _mockPostService.Setup(s => s.GetPostByIdAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception($"An error occurred while processing request to get post by id {postId}."));

            // Act
            var result = await _postsController.GetPostByIdAsync(postId, true);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<PostResponse>(objectResult.Value);

            Assert.Equal(500, objectResult.StatusCode);
            Assert.Equal($"An error occurred while processing request to get post by id {postId}.", response.Message);
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnBadRequest_IfPostIsNull()
        {
            // Act
            var result = await _postsController.AddPostAsync(null);

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
    }
}
