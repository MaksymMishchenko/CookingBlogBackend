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

        private record TestSetup(
           ApplicationDbContext Context,
           PostService PostService,
           CommentService CommentService,
           List<Post> Posts,
           List<Category> Categories);

        private TestSetup CreateTestSetup(ApplicationDbContext context, List<Post> posts, List<Category> categories)
        {
            var postRepo = new Repository<Post>(context);
            var commentRepo = new Repository<Comment>(context);
            var catService = new CategoryService(new Repository<Category>(context), postRepo);
            var postService = new PostService(postRepo, catService, new SnippetGeneratorService());
            var commentService = new CommentService(commentRepo, postRepo, _authServiceMock);

            return new TestSetup(context, postService, commentService, posts, categories);
        }

        private async Task<TestSetup> SetupAsync(Func<List<Category>, List<Post>> dataGenerator)
        {
            var context = _fixture.CreateUniqueContext();

            if (!await context.Users.AnyAsync(u => u.Id == _testUser.Id))
            {
                context.Users.Add(_testUser);
                await context.SaveChangesAsync();
            }

            var categories = TestDataHelper.GetCulinaryCategories();
            await _fixture.SeedCategoryAsync(context, categories);

            var postsToSeed = dataGenerator(categories);
            await _fixture.SeedDatabaseAsync(context, postsToSeed);

            return CreateTestSetup(context, postsToSeed, categories);
        }

        [Fact]
        public async Task AddCommentAsync_ShouldAddNewCommentToPostSuccessfully()
        {
            // Arrange
            const int postCount = 10;
            const int commentCount = 5;
            var (context, _, commentService, _, _) = await SetupAsync(categories =>
                _fixture.GeneratePosts(postCount, categories, commentCount));

            using (context)
            {
                var postId = 1;
                var initialCount = await context.Comments.CountAsync(c => c.PostId == postId);

                var content = "Test comment from Bob";

                // Act
                await commentService.AddCommentAsync(postId, content);

                // Assert
                var addedComment = await context.Comments
                    .FirstOrDefaultAsync(c => c.Content == content);

                Assert.NotNull(addedComment);
                Assert.Equal(postId, addedComment.PostId);
                Assert.Equal(_testUser.Id, addedComment.UserId);
                Assert.Equal(initialCount + 1, await context.Comments.CountAsync(c => c.PostId == postId));
            }
        }

        //[Fact]
        //public async Task UpdateCommentAsync_ShouldUpdateContentOfExistingComment()
        //{
        //    // Arrange
        //    ApplicationDbContext context;
        //    var commentService = CreateCommentServiceAndSeedUniqueDb(out context);
        //    using (context)
        //    {

        //        int commentId = 2;
        //        var comment = new EditCommentModel
        //        {
        //            Content = "Edited comment content"
        //        };

        //        // Act
        //        await commentService.UpdateCommentAsync(commentId, comment);

        //        // Assert
        //        var editedComment = await context.Comments
        //            .FirstOrDefaultAsync(c => c.Id == commentId);

        //        Assert.NotNull(editedComment);
        //        Assert.Equal(comment.Content, editedComment.Content);
        //    }
        //}

        //[Fact]
        //public async Task DeleteCommentAsync_ShouldRemoveCommentFromDataBase()
        //{
        //    // Arrange
        //    ApplicationDbContext context;
        //    var commentService = CreateCommentServiceAndSeedUniqueDb(out context);
        //    using (context)
        //    {
        //        var commentIdToRemove = 1;
        //        var initialCount = await context.Comments.CountAsync();

        //        // Act
        //        await commentService.DeleteCommentAsync(commentIdToRemove);

        //        // Assert
        //        var finalCount = await context.Comments.CountAsync();

        //        var removedComment = await context.Comments.FindAsync(commentIdToRemove);
        //        Assert.Null(removedComment);
        //        Assert.Equal(initialCount - 1, finalCount);
        //    }
        //}
    }
}

