using PostApiService.Controllers;
using PostApiService.Interfaces;
using PostApiService.Models.Common;

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
        public async Task OnAddCommentAsync_ShouldReturnSuccessResponse_WhenCommentIsAddedSuccessfully()
        {
            // Arrange
            var postId = 1;
            var newComment = new Comment
            {                
                Content = "Lorem Ipsum is simply dummy text of the printing and typesetting industry."
            };

            using var cts = new CancellationTokenSource();
            var token = cts.Token;

            _mockCommentService.AddCommentAsync(postId, newComment, token)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _commentController.AddCommentAsync(postId, newComment, token);

            // Assert                        
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult);
            Assert.Equal(CommentM.Success.CommentAddedSuccessfully, ((ApiResponse<Comment>)okResult.Value!).Message);

            await _mockCommentService.Received(1)
                .AddCommentAsync(postId, newComment, token);
        }

        [Fact]
        public async Task OnUpdateCommentAsync_ShouldUpdateCommentSuccessfully()
        {
            // Arrange
            int commentId = 1;
            var updatedComment = new EditCommentModel
            {
                Content = "It is a long established fact that a reader will be distracted."
            };

            using var cts = new CancellationTokenSource();
            var token = cts.Token;

            _mockCommentService.UpdateCommentAsync(commentId, updatedComment, token)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _commentController.UpdateCommentAsync(commentId, updatedComment, token);

            // Assert            
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult);
            Assert.Equal(CommentM.Success.CommentUpdatedSuccessfully, ((ApiResponse<Comment>)okResult.Value!).Message);

            await _mockCommentService.Received(1)
                .UpdateCommentAsync(commentId, updatedComment, token);
        }

        [Fact]
        public async Task OnDeleteCommentAsync_ShouldHandleSuccessAndFailureCorrectly()
        {
            // Arrange
            var commentId = 1;
            using var cts = new CancellationTokenSource();
            var token = cts.Token;

            _mockCommentService.DeleteCommentAsync(commentId, token)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _commentController.DeleteCommentAsync(commentId, token);

            // Assert           
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult);
            Assert.Equal(CommentM.Success.CommentDeletedSuccessfully,
                ((ApiResponse<Comment>)okResult.Value!).Message);

            await _mockCommentService.Received(1)
                .DeleteCommentAsync(commentId, token);
        }
    }
}
