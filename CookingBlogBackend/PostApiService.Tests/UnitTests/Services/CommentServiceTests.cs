using Microsoft.AspNetCore.Identity;
using NSubstitute;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Repositories;
using PostApiService.Services;
using System.Linq.Expressions;

namespace PostApiService.Tests.UnitTests.Services
{
    public class CommentServiceTests
    {
        [Fact]
        public async Task AddCommentAsync_ShouldAddCommentToPost()
        {
            // Arrange            
            var postId = 1;
            var comment = new Comment { Content = "Test comment", Author = "Test author" };

            var mockCommentRepo = Substitute.For<IRepository<Comment>>();
            var mockPostRepo = Substitute.For<IRepository<Post>>();
            mockPostRepo.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var testUser = new IdentityUser { Id = "user123" };
            var mockAuthService = Substitute.For<IAuthService>();
            mockAuthService.GetCurrentUserAsync().Returns(Task.FromResult(testUser));

            var service = new CommentService(mockCommentRepo, mockPostRepo, mockAuthService);

            // Act
            await service.AddCommentAsync(postId, comment);

            // Assert
            await mockCommentRepo.Received(1)
                .AddAsync(comment, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldUpdateCommentContent_AndSaveChanges()
        {
            // Arrange
            var commentId = 1;
            var existingPost = TestDataHelper.GetListWithComments()
                .FirstOrDefault(c => c.Id == commentId);

            var mockCommentRepo = Substitute.For<IRepository<Comment>>();
            mockCommentRepo.GetByIdAsync(Arg.Any<int>()).Returns(existingPost);

            var mockPostRepo = Substitute.For<IRepository<Post>>();
            var mockAuthService = Substitute.For<IAuthService>();

            var updatedComment = new EditCommentModel { Content = "Content edited successfully" };

            var service = new CommentService(mockCommentRepo, mockPostRepo, mockAuthService);

            // Act
            await service.UpdateCommentAsync(commentId, updatedComment);

            // Assert
            await mockCommentRepo.Received(1)
                .GetByIdAsync(Arg.Any<int>());
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldRemoveCommentAndSaveChanges()
        {
            // Arrange
            var commentId = 1;

            var post = TestDataHelper.GetListWithComments()
                .FirstOrDefault(c => c.Id == commentId);

            var mockCommentRepo = Substitute.For<IRepository<Comment>>();
            mockCommentRepo.GetByIdAsync(Arg.Any<int>()).Returns(post);

            var mockPostRepo = Substitute.For<IRepository<Post>>();
            var mockAuthService = Substitute.For<IAuthService>();

            var service = new CommentService(mockCommentRepo, mockPostRepo, mockAuthService);

            // Act
            await service.DeleteCommentAsync(commentId);

            // Assert            
            await mockCommentRepo.Received(1)
                .GetByIdAsync(Arg.Any<int>());
        }
    }
}
