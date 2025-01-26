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
    }
}
