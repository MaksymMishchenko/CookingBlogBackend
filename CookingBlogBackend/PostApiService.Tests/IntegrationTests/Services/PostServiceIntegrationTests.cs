using PostApiService.Infrastructure.Common;
using PostApiService.Repositories;
using PostApiService.Services;

namespace PostApiService.Tests.IntegrationTests.Services
{
    public class PostServiceIntegrationTests : IClassFixture<InMemoryDatabaseFixture>
    {
        private readonly InMemoryDatabaseFixture _fixture;

        public PostServiceIntegrationTests(InMemoryDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        private (PostService Service, List<Post> SeededPosts) CreatePostServiceAndSeedUniqueDb
            (out ApplicationDbContext context, int totalPostCount = 25, int commentCount = 5)
        {
            context = _fixture.CreateUniqueContext();

            var categories = TestDataHelper.GetCulinaryCategories();
            var postsToSeed = _fixture.GeneratePosts(totalPostCount, categories, commentCount);

            _fixture.SeedDatabaseAsync(context, postsToSeed).Wait();

            var repo = new Repository<Post>(context);
            var categoryRepo = new Repository<Category>(context);
            var categoryService = new CategoryService(categoryRepo, repo);
            var snippet = new SnippetGeneratorService();
            var service = new PostService(repo, categoryService, snippet);

            return (service, postsToSeed);
        }

        private (PostService Service, List<Post> SeededPosts) CreatePostServiceAndSeedUniqueDbForSearch
            (out ApplicationDbContext context, string keyword, int totalPostCount = 25)
        {
            context = _fixture.CreateUniqueContext();

            var categories = TestDataHelper.GetCulinaryCategories();
            var postsToSeed = _fixture.GeneratePostsWithKeywords(keyword, categories, totalPostCount);

            _fixture.SeedDatabaseAsync(context, postsToSeed).Wait();

            var repo = new Repository<Post>(context);
            var categoryRepo = new Repository<Category>(context);
            var categoryService = new CategoryService(categoryRepo, repo);
            var snippet = new SnippetGeneratorService();
            var service = new PostService(repo, categoryService, snippet);

            return (service, postsToSeed);
        }

        private (PostService Service, List<Post> SeededPosts) CreatePostServiceAndSeedUniqueDbForSearch
            (out ApplicationDbContext context)
        {
            context = _fixture.CreateUniqueContext();

            var categories = TestDataHelper.GetCulinaryCategories();
            var postsToSeed = _fixture.GeneratePostsWithKeywords(categories);

            _fixture.SeedDatabaseAsync(context, postsToSeed).Wait();

            var repo = new Repository<Post>(context);
            var categoryRepo = new Repository<Category>(context);
            var categoryService = new CategoryService(categoryRepo, repo);
            var snippet = new SnippetGeneratorService();
            var service = new PostService(repo, categoryService, snippet);

            return (service, postsToSeed);
        }

        private record TestSetup(
            ApplicationDbContext Context,
            PostService Service,
            List<Post> Posts,
            List<Category> Categories
        );

        private async Task<TestSetup> SetupFullService()
        {
            var context = _fixture.CreateUniqueContext();

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = _fixture.GeneratePosts(10, categories, 2);

            await _fixture.SeedCategoryAsync(context, categories);
            await _fixture.SeedDatabaseAsync(context, posts);

            var repo = new Repository<Post>(context);
            var catRepo = new Repository<Category>(context);
            var catService = new CategoryService(catRepo, repo);
            var snippet = new SnippetGeneratorService();

            var postService = new PostService(repo, catService, snippet);

            return new TestSetup(context, postService, posts, categories);
        }

        [Fact]
        public async Task GetPostsWithTotalPostCountAsync_ReturnsCorrectPageWithPostCountAndCommentCount()
        {
            // Arrange
            ApplicationDbContext context;

            const int ExpectedPageNumber = 1;
            const int ExpectedPageSize = 10;
            const int ExpectedTotalPostCount = 25;
            const int ExpectedCommentCountPerPost = 5;

            var (postService, seededPosts) = CreatePostServiceAndSeedUniqueDb(out context);
            using (context)
            {
                var expectedPostsOnPage = seededPosts
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((ExpectedPageNumber - 1) * ExpectedPageSize)
                    .Take(ExpectedPageSize)
                    .ToList();

                // Act
                var result = await postService.GetPostsWithTotalPostCountAsync
                    (ExpectedPageNumber, ExpectedPageSize);

                // Assert                
                Assert.True(result.IsSuccess);
                Assert.Equal(ResultStatus.Success, result.Status);
                var data = result.Value!;
                Assert.Equal(ExpectedTotalPostCount, data.TotalCount);
                Assert.Equal(ExpectedPageSize, data.PageSize);
                Assert.Equal(1, data.Items.First().Id);
                Assert.Equal(10, data.Items.Last().Id);

                Assert.All(data.Items, (actualDto, index) =>
                {
                    var expectedPost = expectedPostsOnPage[index];

                    TestDataHelper.AssertPostListDtoMapping
                    (expectedPost, actualDto, ExpectedCommentCountPerPost);
                });
            }
        }

        [Fact]
        public async Task SearchPosts_ShouldFindQuery_InTitleOrDescriptionOrContent()
        {
            // Arrange
            ApplicationDbContext context;
            const string Query = "Chili";

            var (postService, seededPosts) = CreatePostServiceAndSeedUniqueDbForSearch(out context);

            var expectedIds = seededPosts
                .Where(p => p.Title.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                            p.Description.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                            p.Content.Contains(Query, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Id)
                .OrderBy(id => id)
                .ToList();

            // Act
            var result = await postService.SearchPostsWithTotalCountAsync(Query);

            // Assert           
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            var data = result.Value!;

            var actualIds = data.Items.Select(r => r.Id).OrderBy(id => id).ToList();
            Assert.Equal(expectedIds, actualIds);

            Assert.All(data.Items, item =>
            {
                bool foundInTitle = item.Title.Contains(Query, StringComparison.OrdinalIgnoreCase);
                Assert.True(foundInTitle || expectedIds.Contains(item.Id));
            });
        }

        [Fact]
        public async Task SearchPostsWithTotalCountAsync_ShouldReturnPagesSearchedPosts_WithTotalCount()
        {
            // Arrange
            ApplicationDbContext context;
            const int PageNumber = 1;
            const int PageSize = 10;
            const int PostsCountToSeed = 10;
            const string Query = "Chili";

            var (postService, seededPosts) = CreatePostServiceAndSeedUniqueDbForSearch
                (out context, Query, PostsCountToSeed);

            // Act            
            var result = await postService.SearchPostsWithTotalCountAsync(Query, PageNumber, PageSize);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            var data = result.Value!;

            Assert.Equal(seededPosts.Count, data.TotalSearchCount);
            Assert.Equal(PageSize, data.Items.Count());
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnSuccess_IfPostExistsInDb()
        {
            // Arrange
            ApplicationDbContext context;
            var (postService, seededPosts) = CreatePostServiceAndSeedUniqueDb(out context);

            using (context)
            {
                var targetPost = seededPosts.First();

                // Act
                var result = await postService.GetPostByIdAsync(targetPost.Id);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.Equal(ResultStatus.Success, result.Status);
                Assert.NotNull(result.Value);

                Assert.Equal(targetPost.Title, result.Value.Title);
                Assert.Equal(targetPost.Author, result.Value.Author);
                Assert.Equal(targetPost.Slug, result.Value.Slug);
            }
        }

        [Fact]
        public async Task GetPostByIdAsync_Integration_ShouldReturnNotFound_IfIdDoesNotExist()
        {
            // Arrange
            ApplicationDbContext context;
            var (postService, _) = CreatePostServiceAndSeedUniqueDb(out context);

            using (context)
            {
                var nonExistentId = 999999;

                // Act
                var result = await postService.GetPostByIdAsync(nonExistentId);

                // Assert
                Assert.False(result.IsSuccess);
                Assert.Equal(ResultStatus.NotFound, result.Status);
                Assert.Equal(PostM.Errors.PostNotFoundCode, result.ErrorCode);

                var expectedMessage = string.Format(PostM.Errors.PostNotFound, nonExistentId);
                Assert.Equal(expectedMessage, result.Message);
            }
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnSuccess_WhenPostAddedSuccessfully()
        {
            // Arrange                      
            var (context, postService, seededPosts, seededCategories) = await SetupFullService();
            var existingCategoryId = seededCategories.First().Id;

            using (context)
            {
                var newPost = TestDataHelper.GetSinglePostWithCategoryId(existingCategoryId);
                var postDto = TestDataHelper.ToPostCreateDto(newPost);

                var initialCount = await context.Posts.CountAsync();

                // Act
                var result = await postService.AddPostAsync(postDto);

                // Assert                
                Assert.True(result.IsSuccess);
                Assert.Equal(ResultStatus.Success, result.Status);

                var data = result.Value!;
                Assert.NotNull(data);

                Assert.Equal(postDto.Title, data.Title);
                Assert.Equal(postDto.Slug, data.Slug);
                Assert.Equal(existingCategoryId, data.CategoryId);

                var addedPostInDb = await context.Posts
                    .FirstOrDefaultAsync(p => p.Title == postDto.Title);
                Assert.NotNull(addedPostInDb);
                Assert.Equal(postDto.Content, addedPostInDb.Content);
                Assert.Equal(postDto.CategoryId, addedPostInDb.CategoryId);

                var postCount = await context.Posts.CountAsync();
                Assert.Equal(initialCount + 1, postCount);
            }
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldUpdatedExistingPostSuccessfully()
        {
            // Arrange
            ApplicationDbContext context;
            var (postService, seededPosts) = CreatePostServiceAndSeedUniqueDb(out context);
            using (context)
            {
                var postId = 1;
                var existingPost = await context.Posts.FindAsync(postId);
                Assert.NotNull(existingPost);

                var updatedTitle = "New Receipt Title";

                var updateDto = TestDataHelper.ToPostUpdateDto(existingPost, updatedTitle);

                // Act                
                var result = await postService.UpdatePostAsync(postId, updateDto);

                // Assert                
                Assert.NotNull(result);
                Assert.True(result.IsSuccess);
                Assert.Equal(ResultStatus.Success, result.Status);

                var postInDb = await context.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == postId);

                Assert.NotNull(postInDb);
                Assert.Equal(updateDto.Title, postInDb.Title);
                Assert.Equal(updateDto.Content, postInDb.Content);
                Assert.Equal(PostM.Success.PostUpdatedSuccessfully, result.Message);
            }
        }

        [Fact]
        public async Task DeletePostAsync_ShouldRemovePostSuccessfully()
        {
            // Arrange
            ApplicationDbContext context;
            var (postService, seededPosts) = CreatePostServiceAndSeedUniqueDb(out context);
            using (context)
            {
                var initialCount = await context.Posts.CountAsync();
                var postId = 1;

                var successMessage = PostM.Success.PostDeletedSuccessfully;

                // Act
                var result = await postService.DeletePostAsync(postId);

                // Assert
                Assert.NotNull(result);
                Assert.True(result.IsSuccess);
                Assert.Equal(ResultStatus.Success, result.Status);
                Assert.Equal(successMessage, result.Message);

                var removedPost = await context.Posts.AnyAsync(p => p.Id == postId);
                Assert.False(removedPost);

                var finalCount = await context.Posts.CountAsync();
                Assert.Equal(initialCount - 1, finalCount);
            }
        }
    }
}
