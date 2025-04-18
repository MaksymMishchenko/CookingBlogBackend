﻿using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Repositories;
using PostApiService.Services;

namespace PostApiService.Tests.IntegrationTests.Services
{
    public class CommentServiceIntegrationTests : IClassFixture<InMemoryDatabaseFixture>
    {
        private readonly InMemoryDatabaseFixture _fixture;
        private readonly IAuthService _authServiceMock;
        private readonly IdentityUser _testUser;

        public CommentServiceIntegrationTests(InMemoryDatabaseFixture fixture)
        {
            _fixture = fixture;
            _authServiceMock = Substitute.For<IAuthService>();
            _testUser = new IdentityUser { Id = "user123", UserName = "testuser", Email = "test@test.com" };

            _authServiceMock.GetCurrentUserAsync().Returns(_testUser);
        }

        private CommentService CreateCommentService()
        {
            var context = _fixture.CreateContext();
            var commentRepo = new Repository<Comment>(context);
            var postRepo = new Repository<Post>(context);
            return new CommentService(commentRepo, postRepo, _authServiceMock);
        }

        [Fact]
        public async Task AddCommentAsync_ShouldAddNewCommentToPostSuccessfully()
        {
            // Arrange
            var postId = 1;
            var commentService = CreateCommentService();
            using var context = _fixture.CreateContext();
            var initialCount = await context.Comments.CountAsync(c => c.PostId == postId);

            var comment = new Comment
            {
                Content = "Test comment from Bob",
                Author = "Bob"
            };

            // Act
            await commentService.AddCommentAsync(postId, comment);

            // Assert
            var addedComment = await context.Comments
                .FirstOrDefaultAsync(c => c.Content == comment.Content && c.Author == comment.Author);

            Assert.NotNull(addedComment);
            Assert.Equal(postId, addedComment.PostId);
            Assert.Equal(_testUser.Id, addedComment.UserId);
            Assert.Equal(initialCount + 1, await context.Comments.CountAsync(c => c.PostId == postId));
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldUpdateContentOfExistingComment()
        {
            // Arrange            
            using var context = _fixture.CreateContext();
            var commentService = CreateCommentService();

            int commentId = 2;
            var comment = new EditCommentModel
            {
                Content = "Edited comment content"
            };

            // Act
            await commentService.UpdateCommentAsync(commentId, comment);

            // Assert
            var editedComment = await context.Comments
                .FirstOrDefaultAsync(c => c.Id == commentId);

            Assert.NotNull(editedComment);
            Assert.Equal(comment.Content, editedComment.Content);
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldRemoveCommentFromDataBase()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var commentService = CreateCommentService();

            var commentIdToRemove = 1;
            var initialCount = await context.Comments.CountAsync();

            // Act
            await commentService.DeleteCommentAsync(commentIdToRemove);
            var finalCount = await context.Comments.CountAsync();

            // Assert
            var removedComment = await context.Comments.FindAsync(commentIdToRemove);
            Assert.Null(removedComment);
            Assert.Equal(initialCount - 1, finalCount);
        }
    }
}

