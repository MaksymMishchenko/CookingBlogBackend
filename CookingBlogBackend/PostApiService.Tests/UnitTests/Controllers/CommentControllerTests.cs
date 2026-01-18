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

        [Theory]
        [InlineData(ResultStatus.Unauthorized, 401, typeof(UnauthorizedObjectResult))]
        [InlineData(ResultStatus.Invalid, 400, typeof(BadRequestObjectResult))]
        [InlineData(ResultStatus.NotFound, 404, typeof(NotFoundObjectResult))]
        public async Task AddCommentAsync_ShouldReturnCorrectStatusCode_ForNegativeResults(
            ResultStatus status,
            int expectedStatusCode,
            Type expectedResultType)
        {
            // Arrange
            var msg = "Error message";
            var code = "ERR_CODE";

            var serviceResult = status switch
            {
                ResultStatus.Unauthorized => Result<CommentCreatedDto>.Unauthorized(msg, code),
                ResultStatus.Invalid => Result<CommentCreatedDto>.Invalid(msg, code),
                ResultStatus.NotFound => Result<CommentCreatedDto>.NotFound(msg, code),
                _ => throw new ArgumentException($"Unsupported status: {status}")
            };

            _mockCommentService
                .AddCommentAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(serviceResult);

            // Act
            var result = await _commentController.AddCommentAsync(1, new CommentCreateDto { Content = "New content" });

            // Assert
            Assert.IsType(expectedResultType, result);

            var objectResult = result as ObjectResult;
            Assert.NotNull(objectResult);
            Assert.Equal(expectedStatusCode, objectResult.StatusCode);

            var response = Assert.IsType<ApiResponse>(objectResult.Value);
            Assert.False(response.Success);
            Assert.Equal(msg, response.Message);
            Assert.Equal(code, response.ErrorCode);

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

            var serviceResult = Result<CommentCreatedDto>.Success(responseDto, successMessage);
            _mockCommentService.AddCommentAsync(postId, newComment.Content, token)
                .Returns(serviceResult);

            // Act
            var result = await _commentController.AddCommentAsync(postId, newComment, token);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<CommentCreatedDto>>(okResult.Value);

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

        [Theory]
        [InlineData(ResultStatus.Unauthorized, 401, typeof(UnauthorizedObjectResult))]
        [InlineData(ResultStatus.Invalid, 400, typeof(BadRequestObjectResult))]
        [InlineData(ResultStatus.NotFound, 404, typeof(NotFoundObjectResult))]
        [InlineData(ResultStatus.Forbidden, 403, typeof(ObjectResult))]
        public async Task UpdateCommentAsync_ShouldReturnCorrectStatusCode_ForNegativeResults(
            ResultStatus status,
            int expectedStatusCode,
            Type expectedResultType)
        {
            // Arrange
            var msg = "Error message";
            var code = "ERR_CODE";

            var serviceResult = status switch
            {
                ResultStatus.Unauthorized => Result<CommentUpdatedDto>.Unauthorized(msg, code),
                ResultStatus.Invalid => Result<CommentUpdatedDto>.Invalid(msg, code),
                ResultStatus.NotFound => Result<CommentUpdatedDto>.NotFound(msg, code),
                ResultStatus.Forbidden => Result<CommentUpdatedDto>.Forbidden(msg, code),
                _ => throw new ArgumentException($"Unsupported status: {status}")
            };

            _mockCommentService
                .UpdateCommentAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(serviceResult);

            // Act
            var result = await _commentController.UpdateCommentAsync(1, new CommentUpdateDto { Content = "New content" });

            // Assert
            Assert.IsType(expectedResultType, result);

            var objectResult = result as ObjectResult;
            Assert.NotNull(objectResult);
            Assert.Equal(expectedStatusCode, objectResult.StatusCode);

            var response = Assert.IsType<ApiResponse>(objectResult.Value);
            Assert.False(response.Success);
            Assert.Equal(msg, response.Message);
            Assert.Equal(code, response.ErrorCode);

            await _mockCommentService.Received(1)
               .UpdateCommentAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldUpdateCommentSuccessfully()
        {
            // Arrange
            int commentId = 1;
            var updatedComment = new CommentUpdateDto { Content = "New content" };
            var responseDto = TestDataHelper.UpdateCommentResponse();
            var successMessage = CommentM.Success.CommentUpdatedSuccessfully;
            var token = CancellationToken.None;

            var serviceResult = Result<CommentUpdatedDto>.Success(responseDto, successMessage);
            _mockCommentService.UpdateCommentAsync(commentId, updatedComment.Content, token)
                .Returns(serviceResult);

            // Act
            var result = await _commentController.UpdateCommentAsync(commentId, updatedComment, token);

            // Assert            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<CommentUpdatedDto>>(okResult.Value);

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

        [Theory]
        [InlineData(ResultStatus.Unauthorized, 401, typeof(UnauthorizedObjectResult))]
        [InlineData(ResultStatus.NotFound, 404, typeof(NotFoundObjectResult))]
        [InlineData(ResultStatus.Forbidden, 403, typeof(ObjectResult))]
        public async Task DeleteCommentAsync_ShouldReturnCorrectStatusCode_ForNegativeResults(
            ResultStatus status,
            int expectedStatusCode,
            Type expectedResultType)
        {
            // Arrange
            var msg = "Error message";
            var code = "ERR_CODE";

            var serviceResult = status switch
            {
                ResultStatus.Unauthorized => Result.Unauthorized(msg, code),
                ResultStatus.NotFound => Result.NotFound(msg, code),
                ResultStatus.Forbidden => Result.Forbidden(msg, code),
                _ => throw new ArgumentException($"Unsupported status: {status}")
            };

            _mockCommentService
                .DeleteCommentAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(serviceResult);

            // Act
            var result = await _commentController.DeleteCommentAsync(1);

            // Assert
            Assert.IsType(expectedResultType, result);

            var objectResult = result as ObjectResult;
            Assert.NotNull(objectResult);
            Assert.Equal(expectedStatusCode, objectResult.StatusCode);

            var response = Assert.IsType<ApiResponse>(objectResult.Value);
            Assert.False(response.Success);
            Assert.Equal(msg, response.Message);
            Assert.Equal(code, response.ErrorCode);

            await _mockCommentService.Received(1).DeleteCommentAsync(Arg.Any<int>());
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
