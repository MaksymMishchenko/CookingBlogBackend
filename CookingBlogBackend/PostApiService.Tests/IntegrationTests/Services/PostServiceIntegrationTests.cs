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

        private record TestSetup(
        ApplicationDbContext Context,
        PostService Service,
        List<Post> Posts,
        List<Category> Categories);

        private TestSetup CreateTestSetup(ApplicationDbContext context, List<Post> posts, List<Category> categories)
        {
            var repo = new Repository<Post>(context);
            var catService = new CategoryService(new Repository<Category>(context), repo);
            var service = new PostService(repo, catService, new SnippetGeneratorService());

            return new TestSetup(context, service, posts, categories);
        }

        private async Task<TestSetup> SetupAsync(Func<List<Category>, List<Post>> dataGenerator)
        {
            var context = _fixture.CreateUniqueContext();
            var categories = TestDataHelper.GetCulinaryCategories();

            await _fixture.SeedCategoryAsync(context, categories);

            var postsToSeed = dataGenerator(categories);

            await _fixture.SeedDatabaseAsync(context, postsToSeed);
            return CreateTestSetup(context, postsToSeed, categories);
        }

        [Fact]
        public async Task GetPostsWithTotalPostCountAsync_ReturnsCorrectPageWithPostCountAndCommentCount()
        {
            // Arrange            
            const int ExpectedPageNumber = 1;
            const int ExpectedPageSize = 10;
            const int ExpectedTotalPostCount = 25;
            const int ExpectedCommentCountPerPost = 5;

            var (context, postService, seededPosts, _) = await SetupAsync(cats =>
                _fixture.GeneratePosts(ExpectedTotalPostCount, cats, ExpectedCommentCountPerPost));

            using (context)
            {
                var expectedPostsOnPage = seededPosts
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((ExpectedPageNumber - 1) * ExpectedPageSize)
                    .Take(ExpectedPageSize)
                    .ToList();

                // Act
                var result = await postService.GetPostsWithTotalPostCountAsync(ExpectedPageNumber, ExpectedPageSize);

                // Assert                
                Assert.True(result.IsSuccess);
                Assert.Equal(ResultStatus.Success, result.Status);

                var data = result.Value!;
                Assert.Equal(ExpectedTotalPostCount, data.TotalCount);
                Assert.Equal(ExpectedPageSize, data.PageSize);

                Assert.Equal(expectedPostsOnPage.First().Id, data.Items.First().Id);
                Assert.Equal(expectedPostsOnPage.Last().Id, data.Items.Last().Id);

                Assert.All(data.Items.Select((item, index) => new { item, index }), x =>
                {
                    var expectedPost = expectedPostsOnPage[x.index];
                    TestDataHelper.AssertPostListDtoMapping(expectedPost, x.item, ExpectedCommentCountPerPost);
                });
            }
        }

        [Fact]
        public async Task SearchPosts_ShouldFindQuery_InTitleOrDescriptionOrContent()
        {
            // Arrange            
            const string Query = "Chili";

            var (_, postService, seededPosts, _) = await SetupAsync(categories =>
                _fixture.GeneratePostsWithKeywords(categories));

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
                Assert.Contains(item.Id, expectedIds);
            });
        }

        [Fact]
        public async Task SearchPostsWithTotalCountAsync_ShouldReturnPagesSearchedPosts_WithTotalCount()
        {
            // Arrange            
            const int PageNumber = 1;
            const int PageSize = 10;
            const int PostsCountToSeed = 10;
            const string Query = "Chili";

            var (_, postService, seededPosts, _) = await SetupAsync(cats =>
                _fixture.GeneratePostsWithKeywords(Query, cats, PostsCountToSeed));

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
            var (context, postService, seededPosts, _) = await SetupAsync(categories =>
                _fixture.GeneratePosts(25, categories, 5));

            var targetPost = seededPosts.First();

            using (context)
            {
                // Act
                var result = await postService.GetPostByIdAsync(targetPost.Id);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.Equal(ResultStatus.Success, result.Status);

                var data = result.Value;
                Assert.NotNull(data);

                Assert.Equal(targetPost.Title, data.Title);
                Assert.Equal(targetPost.Author, data.Author);
                Assert.Equal(targetPost.Slug, data.Slug);
            }
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnSuccess_WhenPostAddedSuccessfully()
        {
            // Arrange           
            var (context, postService, _, categories) = await SetupAsync(cats => new List<Post>());

            var existingCategoryId = categories.First().Id;

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
                Assert.Equal(existingCategoryId, data.CategoryId);

                var addedPostInDb = await context.Posts
                    .FirstOrDefaultAsync(p => p.Title == postDto.Title);

                Assert.NotNull(addedPostInDb);
                Assert.Equal(postDto.Content, addedPostInDb.Content);

                var finalCount = await context.Posts.CountAsync();
                Assert.Equal(initialCount + 1, finalCount);
            }
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldUpdateExistingPostSuccessfully()
        {
            // Arrange                        
            var (context, postService, seededPosts, _) = await SetupAsync(cats =>
                _fixture.GeneratePosts(1, cats, 0));

            var targetPost = seededPosts.First();
            var updatedTitle = "New Receipt Title";

            var updateDto = TestDataHelper.ToPostUpdateDto(targetPost, updatedTitle);

            using (context)
            {
                // Act                              
                var result = await postService.UpdatePostAsync(targetPost.Id, updateDto);

                // Assert                
                Assert.True(result.IsSuccess);
                Assert.Equal(ResultStatus.Success, result.Status);

                var postInDb = await context.Posts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == targetPost.Id);

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
            var (context, postService, seededPosts, _) = await SetupAsync(cats =>
                _fixture.GeneratePosts(3, cats, 0));

            var targetPost = seededPosts.First();
            var initialCount = seededPosts.Count;
            var successMessage = PostM.Success.PostDeletedSuccessfully;

            using (context)
            {
                // Act
                var result = await postService.DeletePostAsync(targetPost.Id);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.Equal(ResultStatus.Success, result.Status);
                Assert.Equal(successMessage, result.Message);

                var postExists = await context.Posts.AnyAsync(p => p.Id == targetPost.Id);
                Assert.False(postExists);

                var finalCount = await context.Posts.CountAsync();
                Assert.Equal(initialCount - 1, finalCount);
            }
        }
    }
}
