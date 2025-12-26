using PostApiService.Controllers;
using PostApiService.Interfaces;

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
                Author = "Brian",
                Content = "Lorem Ipsum is simply dummy text of the printing and typesetting industry."
            };

            _mockCommentService.AddCommentAsync(postId, newComment)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _commentController.AddCommentAsync(postId, newComment);

            // Assert                        
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult);
            Assert.Equal(CommentM.Success.CommentAddedSuccessfully, ((ApiResponse<Comment>)okResult.Value!).Message);

            await _mockCommentService.Received(1)
                .AddCommentAsync(postId, newComment);
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

            _mockCommentService.UpdateCommentAsync(commentId, updatedComment)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _commentController.UpdateCommentAsync(commentId, updatedComment);

            // Assert            
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult);
            Assert.Equal(CommentM.Success.CommentUpdatedSuccessfully, ((ApiResponse<Comment>)okResult.Value!).Message);

            await _mockCommentService.Received(1)
                .UpdateCommentAsync(commentId, updatedComment);
        }

        [Fact]
        public async Task OnDeleteCommentAsync_ShouldHandleSuccessAndFailureCorrectly()
        {
            // Arrange
            var commentId = 1;
            _mockCommentService.DeleteCommentAsync(commentId)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _commentController.DeleteCommentAsync(commentId);

            // Assert           
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult);
            Assert.Equal(CommentM.Success.CommentDeletedSuccessfully,
                ((ApiResponse<Comment>)okResult.Value!).Message);

            await _mockCommentService.Received(1)
                .DeleteCommentAsync(commentId);
        }
    }
}
