﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;
using PostApiService.Exceptions;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Services;

namespace PostApiService.Tests.UnitTests.Services
{
    public class CommentServiceTests : IClassFixture<InMemoryDatabaseFixture>
    {
        private readonly InMemoryDatabaseFixture _fixture;
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<ILogger<CommentService>> _mockLoggerService;
        private readonly CommentService _commentService;

        public CommentServiceTests(InMemoryDatabaseFixture fixture)
        {
            _fixture = fixture;
            _mockContext = new Mock<IApplicationDbContext>();
            _mockLoggerService = new Mock<ILogger<CommentService>>();
            _commentService = new CommentService(_mockContext.Object, _mockLoggerService.Object);
        }

        private ICommentService CreateCommentService()
        {
            var context = _fixture.CreateContext();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            return new CommentService(context, _mockLoggerService.Object);
        }

        [Fact]
        public async Task AddCommentAsync_ShouldThrowPostNotFoundException_IfPostDoesNotExist()
        {
            // Arrange
            var commentService = CreateCommentService();
            var postId = 2;

            var comment = new Comment { Content = "Test comment", Author = "Test author" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<PostNotFoundException>(() => commentService.AddCommentAsync(postId, comment));
            Assert.Equal(string.Format(PostErrorMessages.PostNotFound, postId), exception.Message);
        }

        [Fact]
        public async Task AddCommentAsync_ShouldAddCommentToPost()
        {
            // Arrange
            var postId = 1;
            var saveChangedResult = 1;

            _mockContext.Setup(c => c.Posts).ReturnsDbSet(TestDataHelper.GetListWithPost());
            _mockContext.Setup(c => c.Comments).ReturnsDbSet(TestDataHelper.GetEmptyCommentList());

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(saveChangedResult);

            var comment = new Comment { Content = "Test comment", Author = "Test author" };

            // Act
            await _commentService.AddCommentAsync(postId, comment);

            // Assert            
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AddCommentAsync_ShouldThrowAddCommentFailedException_WhenSaveChangesFails()
        {
            // Arrange
            var postId = 1;
            var saveChangedResult = 0;

            _mockContext.Setup(c => c.Posts).ReturnsDbSet(TestDataHelper.GetListWithPost());
            _mockContext.Setup(c => c.Comments).ReturnsDbSet(TestDataHelper.GetEmptyCommentList());
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(saveChangedResult);

            var comment = new Comment { Content = "Test comment", Author = "Test author" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AddCommentFailedException>(async () =>
            await _commentService.AddCommentAsync(postId, comment));

            Assert.Equal(string.Format
                (CommentErrorMessages.AddCommentFailed, postId), exception.Message);

            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldThrowInvalidOperationException_IfCommentDoesNotExist()
        {
            // Arrange
            var commentId = 1;
            _mockContext.Setup(c => c.Comments).ReturnsDbSet(TestDataHelper.GetListWithComments());

            var comment = new EditCommentModel { Content = "Updated comment" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _commentService.UpdateCommentAsync(commentId, comment));

            Assert.Equal($"Comment with ID {commentId} does not exist", exception.Message);
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldReturnTrue_IfCommentAddedSuccessfully()
        {
            // Arrange
            var commentId = 1;
            var saveChangedResult = 1;

            _mockContext.Setup(c => c.Comments.FindAsync(It.Is<int>(id => id == commentId)))
                .ReturnsAsync(TestDataHelper.GetListWithComments()
                .FirstOrDefault(c => c.CommentId == commentId));

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(saveChangedResult);

            var updatedComment = new EditCommentModel { Content = "Content edited successfully" };

            // Act
            var result = await _commentService.UpdateCommentAsync(commentId, updatedComment);

            // Assert            
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldReturnFalse_WhenSaveChangesFails()
        {
            // Arrange
            var commentId = 1;
            var saveChangedResult = 0;

            _mockContext.Setup(c => c.Comments.FindAsync(It.Is<int>(id => id == commentId)))
                .ReturnsAsync(TestDataHelper.GetListWithComments()
                .FirstOrDefault(c => c.CommentId == commentId));

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(saveChangedResult);

            var updatedComment = new EditCommentModel { Content = "Content edited successfully" };

            // Act
            var result = await _commentService.UpdateCommentAsync(commentId, updatedComment);

            // Assert            
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldThrowDbUpdateConcurrencyException_WhenDbSaveFailsDueToConcurrency()
        {
            // Arrange
            var commentId = 1;

            _mockContext.Setup(c => c.Comments.FindAsync(It.Is<int>(id => id == commentId)))
                .ReturnsAsync(TestDataHelper.GetListWithComments()
                .FirstOrDefault(c => c.CommentId == commentId));

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException());

            var updatedComment = new EditCommentModel { Content = "Content edited successfully" };

            // Act & Assert            
            var concurrencyException = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => _commentService
            .UpdateCommentAsync(commentId, updatedComment));

            Assert.Equal($"Concurrency issue while updating comment ID {commentId}.", concurrencyException.Message);
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldThrowException_IfUnexpectedErrorOccurs()
        {
            // Arrange
            var commentId = 1;

            _mockContext.Setup(c => c.Comments.FindAsync(It.Is<int>(id => id == commentId)))
                .ReturnsAsync(TestDataHelper.GetListWithComments()
                .FirstOrDefault(c => c.CommentId == commentId));

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception());

            var updatedComment = new EditCommentModel { Content = "Content edited successfully" };

            // Act & Assert            
            var exception = await Assert.ThrowsAsync<Exception>(() => _commentService
            .UpdateCommentAsync(commentId, updatedComment));

            Assert.Equal("An unexpected error occurred. Please try again later.", exception.Message);
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldThrowKeyNotFoundException_IfPostDoesNotExist()
        {
            // Arrange
            var commentId = 1;

            _mockContext.Setup(c => c.Comments).ReturnsDbSet(TestDataHelper.GetEmptyCommentList());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _commentService.DeleteCommentAsync(commentId));

            Assert.Equal($"Comment with ID {commentId} does not exist", exception.Message);
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldRemoveCommentAndSaveChanges()
        {
            // Arrange
            var commentId = 1;
            var saveChangedResult = 1;

            _mockContext.Setup(c => c.Comments.FindAsync(It.Is<int>(id => id == commentId)))
                .ReturnsAsync(TestDataHelper.GetListWithComments()
                .FirstOrDefault(c => c.CommentId == commentId));

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(saveChangedResult);

            // Act
            await _commentService.DeleteCommentAsync(commentId);

            // Assert            
            _mockContext.Verify(c => c.Comments.Remove(It.Is<Comment>(c => c.CommentId == commentId)), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldThrowDbUpdateConcurrencyException_WhenDbSaveFailsDueToConcurrency()
        {
            // Arrange
            var commentId = 1;

            _mockContext.Setup(c => c.Comments.FindAsync(It.Is<int>(id => id == commentId)))
                .ReturnsAsync(TestDataHelper.GetListWithComments()
                .FirstOrDefault(c => c.CommentId == commentId));

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException());

            // Act & Assert            
            var concurrencyException = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => _commentService
            .DeleteCommentAsync(commentId));

            Assert.Equal($"Concurrency issue while removing comment ID {commentId}.", concurrencyException.Message);
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldThrowException_IfUnexpectedErrorOccurs()
        {
            // Arrange
            var commentId = 1;

            _mockContext.Setup(c => c.Comments.FindAsync(It.Is<int>(id => id == commentId)))
                .ReturnsAsync(TestDataHelper.GetListWithComments()
                .FirstOrDefault(c => c.CommentId == commentId));

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception());

            // Act & Assert            
            var exception = await Assert.ThrowsAsync<Exception>(() => _commentService
            .DeleteCommentAsync(commentId));

            Assert.Equal("An unexpected error occurred. Please try again later.", exception.Message);
        }
    }
}
