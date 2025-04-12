using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using PostApiService.Controllers;
using PostApiService.Exceptions;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Models.Dto.Requests;
using System.Net;

namespace PostApiService.Tests.UnitTests.Controllers
{
    public class PostControllerTests
    {
        private readonly IPostService _mockPostService;
        private readonly PostsController _postsController;

        public PostControllerTests()
        {
            _mockPostService = Substitute.For<IPostService>();
            _postsController = new PostsController(_mockPostService);
        }                

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnNotFound_WhenPostsAreNotFound()
        {
            // Arrange
            var postParameters = new PostQueryParameters();
            postParameters.PageNumber = 1;
            postParameters.PageSize = 10;

            var posts = TestDataHelper.GetEmptyPostList();

            _mockPostService.GetAllPostsAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(posts));            

            // Act
            var result = await _postsController.GetAllPostsAsync(postParameters);

            // Assert
            var notFoundObjectResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Post>>(notFoundObjectResult.Value);
            Assert.False(response.Success);
            Assert.Equal(PostErrorMessages.NoPostsFound, response.Message);

            await _mockPostService.Received(1)
                .GetAllPostsAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnOk_WhenPostsAreFound()
        {
            // Arrange
            var postParameters = new PostQueryParameters();
            postParameters.PageNumber = 1;
            postParameters.PageSize = 10;

            var posts = TestDataHelper.GetPostsWithComments(count: 10, generateComments: false);

            _mockPostService.GetAllPostsAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(posts));

            // Act
            var result = await _postsController.GetAllPostsAsync(postParameters);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Post>>(okResult.Value);
            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal(string.Format
                (PostSuccessMessages.PostsRetrievedSuccessfully, response.DataList.Count), response.Message);
            Assert.Equal(10, response.DataList.Count);

            await _mockPostService.Received(1)
                .GetAllPostsAsync(Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>());                
        }        

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnStatusCode200_IfPostExists()
        {
            // Arrange
            var expectedPost = TestDataHelper.GetSinglePost();
            _mockPostService.GetPostByIdAsync(expectedPost.Id, true)
                .Returns(expectedPost);

            // Act
            var result = await _postsController.GetPostByIdAsync
                (expectedPost.Id, true);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Post>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal(string.Format
                (PostSuccessMessages.PostRetrievedSuccessfully, expectedPost.Id), response.Message);
            Assert.Equal((int)HttpStatusCode.OK, okResult.StatusCode);

            await _mockPostService.Received(1)
                .GetPostByIdAsync(expectedPost.Id, true);
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturn201AndSuccessMessage_WhenPostIsAddedSuccessfully()
        {
            // Arrange
            var post = TestDataHelper.GetSinglePost();

            _mockPostService.AddPostAsync(post)
                .Returns(post);

            // Act
            var result = await _postsController.AddPostAsync(post);

            // Assert            
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            var response = Assert.IsType<ApiResponse<Post>>(createdAtActionResult.Value);

            Assert.True(response.Success);
            Assert.Equal((int)HttpStatusCode.Created, createdAtActionResult.StatusCode);
            Assert.Equal(string.Format
                (PostSuccessMessages.PostAddedSuccessfully, post.Id), response.Message);

            await _mockPostService.Received(1).AddPostAsync(post);
        }        

        [Fact]
        public async Task UpdatePostAsync_ShouldReturnExpectedResult_WhenPostIsUpdated()
        {
            // Arrange
            var postId = 1;
            var post = TestDataHelper.GetSinglePost();
            _mockPostService.UpdatePostAsync(post)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _postsController.UpdatePostAsync(postId, post);

            // Assert            
            var okObjectResult = Assert.IsType<OkObjectResult>(result);
            var actualResponse = Assert.IsType<ApiResponse<Post>>(okObjectResult.Value);

            Assert.True(actualResponse.Success);
            Assert.Equal(string.Format
                (PostSuccessMessages.PostUpdatedSuccessfully, post.Id), actualResponse.Message);
            Assert.Equal(post.Id, actualResponse.EntityId);

            Assert.Equal((int)HttpStatusCode.OK, okObjectResult.StatusCode);

            await _mockPostService.Received(1).UpdatePostAsync(post);
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
            _mockPostService.DeletePostAsync(postId)
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

            await _mockPostService.Received(1).DeletePostAsync(postId);
        }
    }
}