using PostApiService.Infrastructure.Services;
using PostApiService.Interfaces;
using PostApiService.Repositories;
using PostApiService.Services;

namespace PostApiService.Tests.IntegrationTests.Services
{
    public class CommentServiceIntegrationTests : IClassFixture<InMemoryDatabaseFixture>
    {
        private readonly InMemoryDatabaseFixture _fixture;        
        private readonly IdentityUser _testUser;
        private readonly IWebContext _webContextMock;       

        public CommentServiceIntegrationTests(InMemoryDatabaseFixture fixture)
        {
            _fixture = fixture;
            _webContextMock = Substitute.For<IWebContext>();            

            _testUser = new IdentityUser
            {
                Id = "testContId",
                UserName = "TestBob",
                Email = "bob@test.com"
            };
        }

        private record TestSetup(
          ApplicationDbContext Context,
          PostService PostService,
          CommentService CommentService,
          List<Post> Posts,
          List<Category> Categories);

        private TestSetup CreateTestSetup(ApplicationDbContext context, List<Post> posts, List<Category> categories)
        {
            var postRepo = new PostRepository(context);
            var commentRepo = new CommentRepository(context);
            var sanitizeServiceMock = Substitute.For<IHtmlSanitizationService>();                
            var catService = new CategoryService(new Repository<Category>(context), postRepo);
            var postService = new PostService(postRepo, _webContextMock, sanitizeServiceMock,
                catService, new SnippetGeneratorService());

            _webContextMock.UserId.Returns(_testUser.Id);
            sanitizeServiceMock.SanitizeComment(Arg.Any<string>()).Returns(x => x.Arg<string>());
            _webContextMock.UserName.Returns(_testUser.UserName);
            _webContextMock.IsAdmin.Returns(false);

            var commentService = new CommentService(commentRepo, sanitizeServiceMock, postRepo, _webContextMock);

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

        [Fact]
        public async Task UpdateCommentAsync_ShouldUpdateContentOfExistingComment()
        {
            // Arrange
            const int postCount = 1;
            const int commentCount = 1;
            const int commentId = 1;
            string content = "Edited comment content";

            var (context, _, commentService, _, _) = await SetupAsync(categories =>
                 _fixture.GeneratePosts(postCount, categories, commentCount));

            using (context)
            {
                // Act
                await commentService.UpdateCommentAsync(commentId, content);

                // Assert
                var editedComment = await context.Comments
                    .FirstOrDefaultAsync(c => c.Id == commentId);

                Assert.NotNull(editedComment);
                Assert.Equal(_testUser.Id, editedComment.UserId);
                Assert.Equal(content, editedComment.Content);
            }
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldRemoveCommentFromDataBase()
        {
            // Arrange
            const int postCount = 1;
            const int commentCount = 1;
            var commentIdToRemove = 1;

            var (context, _, commentService, _, _) = await SetupAsync(categories =>
                 _fixture.GeneratePosts(postCount, categories, commentCount));

            using (context)
            {
                var commentBeforeDelete = await context.Comments.FindAsync(commentIdToRemove);
                Assert.NotNull(commentBeforeDelete);
                Assert.Equal(_testUser.Id, commentBeforeDelete.UserId);

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

        [Fact]
        public async Task DeletePost_ShouldCascadeDeleteAllAssociatedComments()
        {
            // Arrange
            const int postId = 1;
            var (context, postService, _, _, _) = await SetupAsync(categories =>
                _fixture.GeneratePosts(1, categories, commentCount: 3));

            using (context)
            {
                var initialCommentsCount = await context.Comments.CountAsync(c => c.PostId == postId);
                Assert.Equal(3, initialCommentsCount);

                // Act
                var postToDelete = await postService.DeletePostAsync(postId);

                // Assert                
                var remainingCommentsCount = await context.Comments.CountAsync(c => c.PostId == postId);

                Assert.Equal(0, remainingCommentsCount);
            }
        }
    }
}

