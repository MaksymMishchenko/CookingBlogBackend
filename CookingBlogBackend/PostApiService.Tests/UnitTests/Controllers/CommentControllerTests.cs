﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PostApiService.Controllers;
using PostApiService.Exceptions;
using PostApiService.Interfaces;
using PostApiService.Models;

namespace PostApiService.Tests.UnitTests.Controllers
{
    public class CommentControllerTests
    {
        private readonly Mock<ICommentService> _mockCommentService;
        private readonly Mock<ILogger<CommentsController>> _mockLoggerService;
        private readonly CommentsController _commentController;
        public CommentControllerTests()
        {
            _mockCommentService = new Mock<ICommentService>();
            _mockLoggerService = new Mock<ILogger<CommentsController>>();
            _commentController = new CommentsController
                 (_mockCommentService.Object, _mockLoggerService.Object);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task OnAddCommentAsync_ShouldReturnsBadRequest_IfPostIdLessThanOrEqualZero(int invalidPostId)
        {
            // Arrange                        
            var newComment = new Comment
            {
                Author = "Bob",
                Content = "Lorem Ipsum is simply dummy text of the printing and typesetting industry."
            };

            // Act
            var result = await _commentController.AddCommentAsync(invalidPostId, newComment);

            //Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Comment>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.Equal(PostErrorMessages.InvalidPostIdParameter, response.Message);
        }

        [Fact]
        public async Task OnAddCommentAsync_ShouldReturnBadRequest_IfCommentIsNull()
        {
            // Arrange            
            var validPostId = 1;

            // Act
            var result = await _commentController.AddCommentAsync(validPostId, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Comment>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.Equal(CommentErrorMessages.CommentCannotBeNull, response.Message);
        }

        [Theory]
        [MemberData(nameof(ModelValidationHelper.GetCommentTestDataWithAuthor), MemberType = typeof(ModelValidationHelper))]
        public async Task OnAddCommentAsync_ShouldReturnBadRequest_WhenModelIsInvalid(
            string author,
            string content,
            bool expectedIsValid)
        {
            // Arrange
            var postId = 1;
            _mockCommentService
                 .Setup(service => service.AddCommentAsync(It.IsAny<int>(), It.IsAny<Comment>()))
                 .Returns(Task.CompletedTask);

            var comment = new Comment
            {
                Author = author,
                Content = content,
                PostId = 1
            };

            ModelValidationHelper.ValidateModel(comment, _commentController);

            // Act
            var result = await _commentController.AddCommentAsync(postId, comment);

            // Assert
            if (expectedIsValid)
            {
                var okResult = Assert.IsType<OkObjectResult>(result);
                Assert.NotNull(okResult);
                Assert.Equal(CommentSuccessMessages.CommentAddedSuccessfully, ((ApiResponse<Comment>)okResult.Value).Message);
            }
            else
            {
                var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
                var response = Assert.IsType<ApiResponse<Comment>>(badRequestResult.Value);

                Assert.Equal(CommentErrorMessages.ValidationFailed, response.Message);

                foreach (var validationResult in _commentController.ModelState.Values.SelectMany(v => v.Errors))
                {
                    Assert.Contains(validationResult.ErrorMessage, response.Errors.Values.SelectMany(errors => errors));
                }
            }
        }

        [Fact]
        public async Task OnAddCommentAsync_ShouldReturnBadRequest_IfPostIdDoesNotMatchCommentPostId()
        {
            // Arrange                        
            var routePostId = 1;
            var mismatchedPostId = 2;
            var newComment = new Comment
            {
                PostId = mismatchedPostId,
                Author = "Bob",
                Content = "Lorem Ipsum is simply dummy text of the printing and typesetting industry."
            };

            // Act
            var result = await _commentController.AddCommentAsync(routePostId, newComment);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<Comment>>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.Equal(CommentErrorMessages.MismatchedPostId, response.Message);
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

            _mockCommentService.Setup(t => t.AddCommentAsync(postId, newComment))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _commentController.AddCommentAsync(postId, newComment);

            // Assert                        
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult);
            Assert.Equal(CommentSuccessMessages.CommentAddedSuccessfully, ((ApiResponse<Comment>)okResult.Value).Message);

            _mockCommentService.Verify(s => s.AddCommentAsync(postId, newComment), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task OnUpdateCommentAsync_CommentIdLessThanOrEqualZero_ReturnsBadRequest(int invalidCommentId)
        {
            // Arrange            
            var commentServiceMock = new Mock<ICommentService>();
            var loggerServiceMock = new Mock<ILogger<CommentsController>>();

            var controller = new CommentsController(commentServiceMock.Object, loggerServiceMock.Object);

            // Act
            var result = await controller.UpdateCommentAsync(invalidCommentId, new EditCommentModel());

            //Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<CommentResponse>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.Equal("Comment ID must be greater than zero.", response.Message);
        }

        [Fact]
        public async Task OnUpdateCommentAsync_CommentIsNull_ReturnsBadRequest()
        {
            // Arrange
            var commentServiceMock = new Mock<ICommentService>();
            var loggerServiceMock = new Mock<ILogger<CommentsController>>();

            var validPostId = 1;

            var controller = new CommentsController(commentServiceMock.Object, loggerServiceMock.Object);

            // Act
            var result = await controller.UpdateCommentAsync(validPostId, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<CommentResponse>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.Equal("Comment cannot be null.", response.Message);
        }

        [Fact]
        public async Task OnUpdateCommentAsync_CommentContentIsNull_ReturnsBadRequest()
        {
            // Arrange
            var commentServiceMock = new Mock<ICommentService>();
            var loggerServiceMock = new Mock<ILogger<CommentsController>>();

            var validPostId = 1;

            var controller = new CommentsController(commentServiceMock.Object, loggerServiceMock.Object);

            // Act
            var result = await controller.UpdateCommentAsync(validPostId, new EditCommentModel() { Content = null });

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<CommentResponse>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.Equal("Content is required.", response.Message);
        }

        [Theory]
        [MemberData(nameof(ModelValidationHelper.GetCommentTestData), MemberType = typeof(ModelValidationHelper))]
        public async Task UpdateCommentAsync_ShouldReturnBadRequest_WhenModelIsInvalid(
            string content,
            bool expectedIsValid)
        {
            // Arrange
            var commentServiceMock = new Mock<ICommentService>();
            var loggerServiceMock = new Mock<ILogger<CommentsController>>();

            //commentServiceMock
            //     .Setup(service => service.UpdateCommentAsync(It.IsAny<int>(), It.IsAny<EditCommentModel>()))
            //     .ReturnsAsync(true);

            var controller = new CommentsController(commentServiceMock.Object, loggerServiceMock.Object);

            var validCommentId = 1;
            var comment = new EditCommentModel
            {
                Content = content,
            };

            ModelValidationHelper.ValidateModel(comment, controller);

            // Act
            var result = await controller.UpdateCommentAsync(validCommentId, comment);

            // Assert
            if (expectedIsValid)
            {
                var okResult = Assert.IsType<OkObjectResult>(result);
                Assert.NotNull(okResult);
                Assert.Equal("Comment updated successfully.", ((CommentResponse)okResult.Value).Message);
            }
            else
            {
                var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
                var response = Assert.IsType<CommentResponse>(badRequestResult.Value);

                Assert.Equal("Validation failed.", response.Message);

                foreach (var validationResult in controller.ModelState.Values.SelectMany(v => v.Errors))
                {
                    Assert.Contains(response.Errors, error => error == validationResult.ErrorMessage);
                }
            }
        }

        //[Theory]
        //[InlineData(true)]
        //[InlineData(false)]
        //public async Task OnUpdateCommentAsync_ShouldHandleSuccessAndFailureCorrectly(bool isSuccess)
        //{
        //    // Arrange
        //    var commentServiceMock = new Mock<ICommentService>();
        //    var loggerServiceMock = new Mock<ILogger<CommentsController>>();

        //    var controller = new CommentsController(commentServiceMock.Object, loggerServiceMock.Object);

        //    //commentServiceMock.Setup(t => t.UpdateCommentAsync(It.IsAny<int>(), It.IsAny<EditCommentModel>())).ReturnsAsync(isSuccess);

        //    int commentId = 1;
        //    var updatedComment = new EditCommentModel { Content = "It is a long established fact that a reader will be distracted." };

        //    // Act
        //    var result = await controller.UpdateCommentAsync(commentId, updatedComment);

        //    // Assert
        //    if (isSuccess)
        //    {
        //        var okResult = Assert.IsType<OkObjectResult>(result);
        //        Assert.NotNull(okResult);
        //        Assert.Equal("Comment updated successfully.", ((CommentResponse)okResult.Value).Message);
        //    }
        //    else
        //    {
        //        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        //        var returnValue = Assert.IsType<CommentResponse>(badRequestResult.Value);

        //        Assert.False(returnValue.Success);
        //        Assert.Equal($"Failed to update comment ID {commentId}.", returnValue.Message);
        //    }
        //}

        //[Fact]
        //public async Task OnUpdateCommentAsync_CommentNotFound_ReturnsNotFound()
        //{
        //    // Arrange
        //    var commentServiceMock = new Mock<ICommentService>();
        //    var loggerServiceMock = new Mock<ILogger<CommentsController>>();

        //    var commentId = 999;

        //    commentServiceMock.Setup(t => t.UpdateCommentAsync(It.IsAny<int>(), It.IsAny<EditCommentModel>()))
        //        .ThrowsAsync(new InvalidOperationException($"Comment with ID {commentId} does not exist"));

        //    var controller = new CommentsController(commentServiceMock.Object, loggerServiceMock.Object);

        //    // Act
        //    var result = await controller.UpdateCommentAsync(commentId, new EditCommentModel() { Content = "Lorem ipsum dolor sit." });

        //    // Assert
        //    var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        //    var response = Assert.IsType<CommentResponse>(notFoundResult.Value);

        //    Assert.Equal($"Comment with ID {commentId} does not exist", response.Message);
        //}

        //[Fact]
        //public async Task OnUpdateCommentAsync_ShouldReturnConcurrencyIssue_WithStatusCode409()
        //{
        //    // Arrange
        //    var commentServiceMock = new Mock<ICommentService>();
        //    var loggerServiceMock = new Mock<ILogger<CommentsController>>();

        //    var commentId = 1;

        //    commentServiceMock.Setup(t => t.UpdateCommentAsync(It.IsAny<int>(), It.IsAny<EditCommentModel>()))
        //        .ThrowsAsync(new DbUpdateConcurrencyException($"Concurrency issue while updating comment ID {commentId}."));

        //    var controller = new CommentsController(commentServiceMock.Object, loggerServiceMock.Object);

        //    // Act
        //    var result = await controller.UpdateCommentAsync(commentId, new EditCommentModel() { Content = "Lorem ipsum dolor sit." });

        //    // Assert            

        //    var conflictResult = Assert.IsType<ObjectResult>(result);
        //    Assert.Equal(409, conflictResult.StatusCode);

        //    var response = Assert.IsType<CommentResponse>(conflictResult.Value);
        //    Assert.Equal($"Concurrency issue while updating comment ID {commentId}.", response.Message);
        //}

        //[Fact]
        //public async Task OnUpdateCommentAsync_UnhandledException_ReturnsServerError()
        //{
        //    // Arrange
        //    var commentServiceMock = new Mock<ICommentService>();
        //    var loggerServiceMock = new Mock<ILogger<CommentsController>>();

        //    commentServiceMock.Setup(t => t.UpdateCommentAsync(It.IsAny<int>(), It.IsAny<EditCommentModel>()))
        //        .ThrowsAsync(new Exception("An unexpected error occurred. Please try again later."));

        //    var validPostId = 1;

        //    var controller = new CommentsController(commentServiceMock.Object, loggerServiceMock.Object);

        //    // Act
        //    var result = await controller.UpdateCommentAsync(validPostId, new EditCommentModel { Content = "Lorem ipsum dolor sit amet" });

        //    // Assert
        //    var serverErrorResult = Assert.IsType<ObjectResult>(result);
        //    Assert.Equal(500, serverErrorResult.StatusCode);

        //    var response = Assert.IsType<CommentResponse>(serverErrorResult.Value);
        //    Assert.Equal("An unexpected error occurred. Please try again later.", response.Message);
        //}

        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, false)]
        [InlineData(1, true)]
        public async Task OnDeleteCommentAsync_ShouldHandleSuccessAndFailureCorrectly(int commentId, bool isSuccess)
        {
            // Arrange
            var commentServiceMock = new Mock<ICommentService>();
            var loggerServiceMock = new Mock<ILogger<CommentsController>>();

            var controller = new CommentsController(commentServiceMock.Object, loggerServiceMock.Object);

            commentServiceMock.Setup(t => t.DeleteCommentAsync(It.IsAny<int>()));

            // Act
            var result = await controller.DeleteCommentAsync(commentId);

            // Assert
            if (isSuccess)
            {
                var okResult = Assert.IsType<OkObjectResult>(result);
                Assert.NotNull(okResult);
                Assert.Equal("Comment deleted successfully", ((CommentResponse)okResult.Value).Message);
            }
            else
            {
                var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
                var returnValue = Assert.IsType<CommentResponse>(badRequestResult.Value);

                Assert.False(returnValue.Success);
                Assert.Equal("Comment ID must be greater than zero.", returnValue.Message);
            }
        }

        [Fact]
        public async Task OnDeleteCommentAsync_CommentNotFound_ReturnsNotFound()
        {
            // Arrange
            var commentServiceMock = new Mock<ICommentService>();
            var loggerServiceMock = new Mock<ILogger<CommentsController>>();

            var commentNotFoundId = 999;

            commentServiceMock.Setup(t => t.DeleteCommentAsync(It.IsAny<int>()))
                .ThrowsAsync(new KeyNotFoundException($"Comment with ID {commentNotFoundId} does not exist"));

            var controller = new CommentsController(commentServiceMock.Object, loggerServiceMock.Object);

            // Act
            var result = await controller.DeleteCommentAsync(commentNotFoundId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<CommentResponse>(notFoundResult.Value);

            Assert.Equal($"Comment with ID {commentNotFoundId} does not exist", response.Message);
        }

        [Fact]
        public async Task OnDeleteCommentAsync_ShouldReturnConcurrencyIssue_WithStatusCode409()
        {
            // Arrange
            var commentServiceMock = new Mock<ICommentService>();
            var loggerServiceMock = new Mock<ILogger<CommentsController>>();

            var commentId = 1;

            commentServiceMock.Setup(t => t.DeleteCommentAsync(It.IsAny<int>()))
                .ThrowsAsync(new DbUpdateConcurrencyException($"Concurrency issue while removing comment ID {commentId}."));

            var controller = new CommentsController(commentServiceMock.Object, loggerServiceMock.Object);

            // Act
            var result = await controller.DeleteCommentAsync(commentId);

            // Assert            
            var conflictResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(409, conflictResult.StatusCode);

            var response = Assert.IsType<CommentResponse>(conflictResult.Value);
            Assert.Equal($"Concurrency issue while removing comment ID {commentId}.", response.Message);
        }

        [Fact]
        public async Task OnDeleteCommentAsync_UnhandledException_ReturnsServerError()
        {
            // Arrange
            var commentServiceMock = new Mock<ICommentService>();
            var loggerServiceMock = new Mock<ILogger<CommentsController>>();

            commentServiceMock.Setup(t => t.DeleteCommentAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("An unexpected error occurred. Please try again later."));

            var validPostId = 1;
            var controller = new CommentsController(commentServiceMock.Object, loggerServiceMock.Object);

            // Act
            var result = await controller.DeleteCommentAsync(validPostId);

            // Assert
            var serverErrorResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverErrorResult.StatusCode);

            var response = Assert.IsType<CommentResponse>(serverErrorResult.Value);
            Assert.Equal("An unexpected error occurred. Please try again later.", response.Message);
        }
    }
}
