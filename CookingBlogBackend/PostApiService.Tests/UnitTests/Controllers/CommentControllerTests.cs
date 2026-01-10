using PostApiService.Controllers;
using PostApiService.Infrastructure.Common;
using PostApiService.Interfaces;
using PostApiService.Models.Common;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
using System.Net;

namespace PostApiService.Tests.UnitTests.Controllers
{
    public class CommentControllerTests
    {
        private readonly ICommentService _mockCommentService;
        private readonly CommentsController _commentController;
        public CommentControllerTests()
        {
            _mockCommentService = Substitute.For<ICommentService>();
            _commentController = new CommentsController
                 (_mockCommentService);
        }

        [Fact]
        public async Task AddCommentAsync_ShouldReturnNotFoundResponse_IfPostDoesNotExist()
        {
            // Arrange
            var postId = 1;
            var newComment = TestDataHelper.CreateCommentRequest("Test comment");
            var errorMessage = PostM.Errors.PostTitleOrSlugAlreadyExist;
            var errorCode = PostM.Errors.PostAlreadyExistCode;

            var serviceResult = Result<CommentDto>.NotFound(errorMessage, errorCode);
            _mockCommentService.AddCommentAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(serviceResult);

            // Act
            var result = await _commentController.AddCommentAsync(postId, newComment);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(notFoundResult.Value);

            Assert.False(response.Success);
            Assert.Equal(errorMessage, response.Message);
            Assert.Equal(errorCode, response.ErrorCode);
            Assert.Equal((int)HttpStatusCode.NotFound, notFoundResult.StatusCode);

            await _mockCommentService.Received(1)
                .AddCommentAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddCommentAsync_ShouldReturnSuccessResponse_IfCommentAddedSuccessfully()
        {
            // Arrange
            var postId = 1;
            var newComment = TestDataHelper.CreateCommentRequest("Test comment");
            var responseDto = TestDataHelper.CreateCommentResponse();
            var token = CancellationToken.None;
            string successMessage = CommentM.Success.CommentAddedSuccessfully;

            var serviceResult = Result<CommentDto>.Success(responseDto, successMessage);
            _mockCommentService.AddCommentAsync(postId, newComment.Content, token)
                .Returns(serviceResult);

            // Act
            var result = await _commentController.AddCommentAsync(postId, newComment, token);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<CommentDto>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal((int)HttpStatusCode.OK, okResult.StatusCode);
            Assert.Equal(successMessage, response.Message);
            Assert.NotNull(response.Data);

            var data = response.Data;
            Assert.Equal(responseDto.Content, data.Content);
            Assert.Equal(responseDto.Author, data.Author);
            Assert.Equal(responseDto.UserId, data.UserId);

            await _mockCommentService.Received(1).AddCommentAsync(postId, newComment.Content, token);
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldReturnNotFound_IfCommentDoesNotExist()
        {
            // Arrange
            int commentId = 99999;
            var updatedComment = new CommentUpdateDto { Content = "Valid comment" };
            var errorMessage = CommentM.Errors.NotFound;
            var errorCode = CommentM.Errors.NotFoundCode;

            var serviceResult = Result<CommentDto>.NotFound(errorMessage, errorCode);
            _mockCommentService.UpdateCommentAsync(commentId, updatedComment.Content, Arg.Any<CancellationToken>())
                .Returns(serviceResult);

            // Act
            var result = await _commentController.UpdateCommentAsync(commentId, updatedComment);

            // Assert            
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(notFoundResult.Value);

            Assert.False(response.Success);
            Assert.Equal((int)HttpStatusCode.NotFound, notFoundResult.StatusCode);
            Assert.Equal(errorMessage, response.Message);
            Assert.Equal(errorCode, response.ErrorCode);

            await _mockCommentService.Received(1).UpdateCommentAsync(commentId, updatedComment.Content);
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldReturnForbidden_WhenUserIsNotOwner()
        {
            // Arrange
            int commentId = 1;
            var updatedComment = new CommentUpdateDto { Content = "New content" };
            var errorMessage = CommentM.Errors.AccessDenied;
            var errorCode = CommentM.Errors.AccessDeniedCode;

            var serviceResult = Result<CommentDto>.Forbidden(errorMessage, errorCode);
            _mockCommentService.UpdateCommentAsync(commentId, updatedComment.Content, Arg.Any<CancellationToken>())
                .Returns(serviceResult);

            // Act
            var result = await _commentController.UpdateCommentAsync(commentId, updatedComment);

            // Assert            
            var forbiddenResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal((int)HttpStatusCode.Forbidden, forbiddenResult.StatusCode);

            var response = Assert.IsType<ApiResponse>(forbiddenResult.Value);
            Assert.False(response.Success);
            Assert.Equal(errorMessage, response.Message);
            Assert.Equal(errorCode, response.ErrorCode);

            await _mockCommentService.Received(1).UpdateCommentAsync(commentId, updatedComment.Content);
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldUpdateCommentSuccessfully()
        {
            // Arrange
            int commentId = 1;
            var updatedComment = new CommentUpdateDto { Content = "New content" };
            var responseDto = TestDataHelper.CreateCommentResponse();
            var successMessage = CommentM.Success.CommentUpdatedSuccessfully;
            var token = CancellationToken.None;

            var serviceResult = Result<CommentDto>.Success(responseDto, successMessage);
            _mockCommentService.UpdateCommentAsync(commentId, updatedComment.Content, token)
                .Returns(serviceResult);

            // Act
            var result = await _commentController.UpdateCommentAsync(commentId, updatedComment, token);

            // Assert            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<CommentDto>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal((int)HttpStatusCode.OK, okResult.StatusCode);
            Assert.Equal(successMessage, response.Message);
            Assert.NotNull(response.Data);

            var data = response.Data;
            Assert.Equal(responseDto.Content, data.Content);
            Assert.Equal(responseDto.Author, data.Author);
            Assert.Equal(responseDto.UserId, data.UserId);

            await _mockCommentService.Received(1)
                .UpdateCommentAsync(commentId, updatedComment.Content, token);
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldReturnNotFound_IfCommentDoesNotExist()
        {
            // Arrange
            int commentId = 99999;
            var errorMessage = CommentM.Errors.NotFound;
            var errorCode = CommentM.Errors.NotFoundCode;

            var serviceResult = Result.NotFound(errorMessage, errorCode);
            _mockCommentService.DeleteCommentAsync(commentId, Arg.Any<CancellationToken>())
                .Returns(serviceResult);

            // Act
            var result = await _commentController.DeleteCommentAsync(commentId);

            // Assert            
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiResponse>(notFoundResult.Value);

            Assert.False(response.Success);
            Assert.Equal((int)HttpStatusCode.NotFound, notFoundResult.StatusCode);
            Assert.Equal(errorMessage, response.Message);
            Assert.Equal(errorCode, response.ErrorCode);

            await _mockCommentService.Received(1).DeleteCommentAsync(commentId);
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldReturnForbidden_WhenUserIsNotOwner()
        {
            // Arrange
            int commentId = 1;
            var errorMessage = CommentM.Errors.AccessDenied;
            var errorCode = CommentM.Errors.AccessDeniedCode;

            var serviceResult = Result.Forbidden(errorMessage, errorCode);
            _mockCommentService.DeleteCommentAsync(commentId, Arg.Any<CancellationToken>())
                .Returns(serviceResult);

            // Act
            var result = await _commentController.DeleteCommentAsync(commentId);

            // Assert            
            var forbiddenResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal((int)HttpStatusCode.Forbidden, forbiddenResult.StatusCode);

            var response = Assert.IsType<ApiResponse>(forbiddenResult.Value);
            Assert.False(response.Success);
            Assert.Equal(errorMessage, response.Message);
            Assert.Equal(errorCode, response.ErrorCode);

            await _mockCommentService.Received(1).DeleteCommentAsync(commentId);
        }


        [Fact]
        public async Task OnDeleteComment_ShouldReturnOk_WhenCommentIsDeletedSuccessfully()
        {
            // Arrange
            var commentId = 1;
            string successMessage = CommentM.Success.CommentDeletedSuccessfully;
            var token = CancellationToken.None;

            var serviceResult = Result.Success(successMessage);
            _mockCommentService.DeleteCommentAsync(commentId, token)
                .Returns(serviceResult);

            // Act
            var result = await _commentController.DeleteCommentAsync(commentId, token);

            // Assert           
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal((int)HttpStatusCode.OK, okResult.StatusCode);

            var response = Assert.IsType<ApiResponse>(okResult.Value);
            Assert.True(response.Success);

            Assert.Equal(successMessage, response.Message);

            await _mockCommentService.Received(1)
                .DeleteCommentAsync(commentId, token);
        }
    }
}
