using PostApiService.Interfaces;
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

        private CommentService CreateCommentServiceAndSeedUniqueDb
            (out ApplicationDbContext context, int totalPostCount = 25, int commentCount = 5)
        {
            context = _fixture.CreateUniqueContext();

            var categories = TestDataHelper.GetCulinaryCategories();
            var postsToSeed = _fixture.GeneratePosts(totalPostCount, categories, commentCount);

            _fixture.SeedDatabaseAsync(context, postsToSeed).Wait();

            var commentRepo = new Repository<Comment>(context);
            var postRepo = new Repository<Post>(context);

            return new CommentService(commentRepo, postRepo, _authServiceMock);
        }

        [Fact]
        public async Task AddCommentAsync_ShouldAddNewCommentToPostSuccessfully()
        {
            // Arrange
            ApplicationDbContext context;
            var commentService = CreateCommentServiceAndSeedUniqueDb(out context);
            using (context)
            {
                var postId = 1;
                var initialCount = await context.Comments.CountAsync(c => c.PostId == postId);

                var comment = new Comment
                {
                    Content = "Test comment from Bob",
                };

                // Act
                await commentService.AddCommentAsync(postId, comment);

                // Assert
                var addedComment = await context.Comments
                    .FirstOrDefaultAsync(c => c.Content == comment.Content);

                Assert.NotNull(addedComment);
                Assert.Equal(postId, addedComment.PostId);
                Assert.Equal(_testUser.Id, addedComment.UserId);
                Assert.Equal(initialCount + 1, await context.Comments.CountAsync(c => c.PostId == postId));
            }
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldUpdateContentOfExistingComment()
        {
            // Arrange
            ApplicationDbContext context;
            var commentService = CreateCommentServiceAndSeedUniqueDb(out context);
            using (context)
            {

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
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldRemoveCommentFromDataBase()
        {
            // Arrange
            ApplicationDbContext context;
            var commentService = CreateCommentServiceAndSeedUniqueDb(out context);
            using (context)
            {
                var commentIdToRemove = 1;
                var initialCount = await context.Comments.CountAsync();

                // Act
                await commentService.DeleteCommentAsync(commentIdToRemove);

                // Assert
                var finalCount = await context.Comments.CountAsync();

                var removedComment = await context.Comments.FindAsync(commentIdToRemove);
                Assert.Null(removedComment);
                Assert.Equal(initialCount - 1, finalCount);
            }
        }
    }
}

