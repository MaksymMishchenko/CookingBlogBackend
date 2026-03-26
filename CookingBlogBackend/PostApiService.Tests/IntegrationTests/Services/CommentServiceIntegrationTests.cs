using PostApiService.Interfaces;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Tests.IntegrationTests.Services
{
    [Collection("SharedDatabase")]
    public class CommentServiceIntegrationTests
    {
        private readonly ServiceTestFixture _fixture;        

        public CommentServiceIntegrationTests(ServiceTestFixture fixture)
        {
            _fixture = fixture;            
        }

        [Fact]
        public async Task GetCommentsByPostIdAsync_ShouldReturnHierarchicalCommentsFlatly()
        {
            // Arrange
            const int ExpectedRootCount = 1;
            const int ExpectedRepliesCount = 2;
            const int TotalCommentsInHierarchy = ExpectedRootCount + ExpectedRepliesCount;
            const int TestPageSize = 10;
            const int InitialLastId = 0;

            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();

            var categories = TestDataHelper.GetCulinaryCategories();

            var posts = TestDataHelper.GetPostsWithComments(1, categories, commentCount: 0)
                .WithCommentHierarchy(commentCount: TotalCommentsInHierarchy, userId: TestUserData.AdminId);

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var targetPost = posts.First();
            var rootComment = targetPost.Comments.First(c => c.ParentId == null);
            var replies = rootComment.Replies.OrderBy(r => r.CreatedAt).ToList();

            var (service, _, _) = _fixture.GetScopedService<ICommentService>();

            // Act
            var result = await service.GetCommentsByPostIdAsync(targetPost.Id, InitialLastId, TestPageSize);

            // Assert                                
            Assert.True(result.IsSuccess);

            var data = Assert.IsType<CommentScrollResponse<CommentDto>>(result.Value);
            var items = data.Items.ToList();

            Assert.Equal(ExpectedRootCount, data.TotalCount);
            Assert.Equal(TotalCommentsInHierarchy, items.Count);

            Assert.Equal(rootComment.Content, items[0].Content);
            Assert.Equal(TestUserData.AdminUserName, items[0].Author);
            Assert.Null(items[0].ParentId);

            Assert.Equal(replies[0].Content, items[1].Content);
            Assert.Equal(rootComment.Id, items[1].ParentId);

            Assert.Equal(replies[1].Content, items[2].Content);
            Assert.Equal(rootComment.Id, items[2].ParentId);
            Assert.Equal(rootComment.Id, data.LastId);
        }

        [Fact]
        public async Task AddCommentAsync_ShouldPersistCommentWithCorrectHierarchy()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();

            var (commentService, dbContext, webContext) = _fixture.GetScopedService<ICommentService>();
            webContext.UserId = TestUserData.AdminId;
            webContext.UserName = TestUserData.AdminUserName;
            webContext.IsAdmin = true;

            var posts = TestDataHelper.GetPostsWithComments(1, TestDataHelper.GetCulinaryCategories(), commentCount: 0);
            await _fixture.Services!.SeedBlogDataAsync(posts, new List<Category>());
            int postId = (await dbContext.Posts.FirstAsync()).Id;

            // Act
            var rootRes = await commentService.AddCommentAsync(postId, "Root", null);
            var replyRes = await commentService.AddCommentAsync(postId, "Reply", rootRes.Value!.Id);

            // Assert
            Assert.True(replyRes.IsSuccess);

            var dto = Assert.IsType<CommentCreatedDto>(replyRes.Value);

            Assert.Equal(CommentM.Success.CommentAddedSuccessfully, replyRes.Message);
            Assert.Equal("Reply", dto.Content);

            var dbReply = await dbContext.Comments.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == dto.Id);

            Assert.NotNull(dbReply);
            Assert.Equal(rootRes.Value!.Id, dbReply.ParentId);
            Assert.Equal(webContext.UserId, dbReply.UserId);
        }

        [Fact]
        public async Task UpdateCommentAsync_ShouldUpdateContentOfExistingComment()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();

            var (commentService, dbContext, webContext) = _fixture.GetScopedService<ICommentService>();
            webContext.UserId = TestUserData.AdminId;
            webContext.UserName = TestUserData.AdminUserName;
            webContext.IsAdmin = true;

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(1, categories, commentCount: 1);
            posts.First().Comments.First().UserId = TestUserData.AdminId;

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var commentInDb = await dbContext.Comments.AsNoTracking().FirstAsync();
            const string NewContent = "Edited comment content";

            // Act
            var result = await commentService.UpdateCommentAsync(commentInDb.Id, NewContent);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(CommentM.Success.CommentUpdatedSuccessfully, result.Message);

            var dto = Assert.IsType<CommentUpdatedDto>(result.Value);
            Assert.Equal(NewContent, dto.Content);
            Assert.Equal(webContext.UserName, dto.Author);

            var editedComment = await dbContext.Comments.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == commentInDb.Id);

            Assert.NotNull(editedComment);
            Assert.Equal(NewContent, editedComment.Content);
            Assert.Equal(TestUserData.AdminId, editedComment.UserId);
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldRemoveCommentFromDatabase()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();                      

            var (commentService, dbContext, webContext) = _fixture.GetScopedService<ICommentService>();
            webContext.UserId = TestUserData.AdminId;
            webContext.UserName = TestUserData.AdminUserName;
            webContext.IsAdmin = true;

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(1, categories, commentCount: 1);
            posts.First().Comments.First().UserId = TestUserData.AdminId;

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var commentInDb = await dbContext.Comments.AsNoTracking().FirstAsync();
            int initialCount = await dbContext.Comments.CountAsync();

            // Act
            var result = await commentService.DeleteCommentAsync(commentInDb.Id);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(CommentM.Success.CommentDeletedSuccessfully, result.Message);

            var finalCount = await dbContext.Comments.CountAsync();
            var exists = await dbContext.Comments.AnyAsync(c => c.Id == commentInDb.Id);

            Assert.False(exists);
            Assert.Equal(initialCount - 1, finalCount);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldCascadeDeleteAllAssociatedComments()
        {
            // Arrange
            const int ExpectedCommentsCount = 3;
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();

            var (postService, dbContext, webContext) = _fixture.GetScopedService<IPostService>();
            webContext.UserId = TestUserData.AdminId;
            webContext.IsAdmin = true;

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(1, categories, commentCount: ExpectedCommentsCount);

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);
            int postId = posts[0].Id;

            var initialCommentsCount = await dbContext.Comments.CountAsync(c => c.PostId == postId);
            Assert.Equal(ExpectedCommentsCount, initialCommentsCount);

            // Act
            var result = await postService.DeletePostAsync(postId);

            // Assert
            Assert.True(result.IsSuccess);

            var postExists = await dbContext.Posts.AnyAsync(p => p.Id == postId);
            var remainingCommentsCount = await dbContext.Comments.CountAsync(c => c.PostId == postId);

            Assert.False(postExists);
            Assert.Equal(0, remainingCommentsCount);
        }
    }
}

