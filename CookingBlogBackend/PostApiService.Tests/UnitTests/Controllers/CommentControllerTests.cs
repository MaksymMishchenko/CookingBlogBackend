using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using PostApiService.Controllers;
using PostApiService.Exceptions;
using PostApiService.Interfaces;
using PostApiService.Models;

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
            Assert.Equal(CommentSuccessMessages.CommentAddedSuccessfully, ((ApiResponse<Comment>)okResult.Value).Message);

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
            Assert.Equal(CommentSuccessMessages.CommentUpdatedSuccessfully, ((ApiResponse<Comment>)okResult.Value).Message);

            await _mockCommentService.Received(1)
                .UpdateCommentAsync(commentId, updatedComment);
        }

        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, false)]
        [InlineData(1, true)]
        public async Task OnDeleteCommentAsync_ShouldHandleSuccessAndFailureCorrectly(int commentId, bool isSuccess)
        {
            // Arrange                        
            _mockCommentService.DeleteCommentAsync(commentId)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _commentController.DeleteCommentAsync(commentId);

            // Assert
            if (isSuccess)
            {
                var okResult = Assert.IsType<OkObjectResult>(result);
                Assert.NotNull(okResult);
                Assert.Equal(CommentSuccessMessages.CommentDeletedSuccessfully,
                    ((ApiResponse<Comment>)okResult.Value).Message);

                await _mockCommentService.Received(1)
                    .DeleteCommentAsync(commentId);
            }
            else
            {
                var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
                var returnValue = Assert.IsType<ApiResponse<Comment>>(badRequestResult.Value);

                Assert.False(returnValue.Success);
                Assert.Equal(CommentErrorMessages.InvalidCommentIdParameter, returnValue.Message);
            }
        }
    }

}
