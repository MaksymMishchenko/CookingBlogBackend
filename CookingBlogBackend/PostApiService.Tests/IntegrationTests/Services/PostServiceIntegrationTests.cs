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
            var snippet = new SnippetGeneratorService();
            var service = new PostService(repo, snippet);

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
            var snippet = new SnippetGeneratorService();
            var service = new PostService(repo, snippet);

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
            var snippet = new SnippetGeneratorService();
            var service = new PostService(repo, snippet);

            return (service, postsToSeed);
        }

        [Fact]
        public async Task GetPostsWithTotalPostCountAsync_ReturnsCorrectPageWithPostCountAndCommentCount_WithContentCheck()
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
                    .OrderBy(p => p.Id)
                    .Skip((ExpectedPageNumber - 1) * ExpectedPageSize)
                    .Take(ExpectedPageSize)
                    .ToList();

                // Act
                var result = await postService.GetPostsWithTotalPostCountAsync
                    (ExpectedPageNumber, ExpectedPageSize);

                // Assert
                Assert.Equal(ExpectedTotalPostCount, result.TotalPostCount);
                Assert.Equal(ExpectedPageSize, result.Posts.Count);
                Assert.Equal(1, result.Posts.First().Id);
                Assert.Equal(10, result.Posts.Last().Id);

                Assert.True(
                    expectedPostsOnPage.Select(p => p.Id)
                        .SequenceEqual(result.Posts.Select(p => p.Id)),
                    "The order or set of Post IDs on the page does not match the expected.");

                Assert.All(result.Posts, (actualDto, index) =>
                {
                    var expectedPost = expectedPostsOnPage[index];

                    TestDataHelper.AssertPostListDtoMapping
                    (expectedPost, actualDto, ExpectedCommentCountPerPost);
                });
            }
        }

        [Fact]
        public async Task GetPostsWithTotalPostCountAsync_ShouldReturnLastIncompletePageRange()
        {
            // Arrange
            ApplicationDbContext context;
            const int PageSize = 10;
            const int PageNumber = 3;
            const int ExpectedTotalPostCount = 25;
            const int ExpectedCommentCountPerPost = 5;
            const int ExpectedPostsOnLastPage = 5;

            var (postService, seededPosts) = CreatePostServiceAndSeedUniqueDb
                (out context, ExpectedTotalPostCount, ExpectedPostsOnLastPage);
            using (context)
            {
                var expectedPostsOnPage = seededPosts
                    .OrderBy(p => p.Id)
                    .Skip((PageNumber - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                // Act
                var result = await postService.GetPostsWithTotalPostCountAsync(PageNumber, PageSize);

                // Assert
                Assert.Equal(ExpectedTotalPostCount, result.TotalPostCount);
                Assert.Equal(ExpectedPostsOnLastPage, result.Posts.Count);
                Assert.Equal(21, result.Posts.First().Id);
                Assert.Equal(ExpectedTotalPostCount, result.Posts.Last().Id);

                Assert.All(result.Posts, (actualDto, index) =>
                {
                    var expectedPost = expectedPostsOnPage[index];
                    TestDataHelper.AssertPostListDtoMapping(expectedPost, actualDto, ExpectedCommentCountPerPost);
                });
            }
        }

        [Fact]
        public async Task GetPostsWithTotalPostCountAsync_ShouldReturnEmptyListWhenTotalCountIsZero()
        {
            // Arrange
            ApplicationDbContext context;
            const int PageNumber = 1;
            const int PageSize = 5;
            const int ExpectedTotalPosts = 0;
            const int ExpectedCommentCount = 0;

            var (postService, seededPosts) = CreatePostServiceAndSeedUniqueDb
                (out context, ExpectedTotalPosts, ExpectedCommentCount);

            using (context)
            {
                // Act            
                var (posts, totalPostCount) = await postService.GetPostsWithTotalPostCountAsync(PageNumber, PageSize);

                // Assert                            
                Assert.Empty(posts);
                Assert.Equal(ExpectedTotalPosts, totalPostCount);
            }
        }

        [Fact]
        public async Task SearchPosts_ShouldFindQuery_InTitleOrDescriptionOrContent()
        {
            // Arrange
            ApplicationDbContext context;
            var query = "Chili";

            var (postService, seededPosts) = CreatePostServiceAndSeedUniqueDbForSearch
                (out context);

            // Act
            var (result, totalCount) = await postService.SearchPostsWithTotalCountAsync(query);

            // Assert
            Assert.Equal(3, totalCount);
            Assert.Equal(3, result.Count);
            Assert.All(result, r =>
            {
                bool foundInTitle = r.Title.Contains(query, StringComparison.OrdinalIgnoreCase);
                bool foundInSnippet = r.SearchSnippet.Contains(query, StringComparison.OrdinalIgnoreCase);

                if (r.Id == 2)
                {
                    Assert.Empty(r.SearchSnippet);
                }
                else
                {
                    Assert.True(foundInTitle || foundInSnippet);
                }
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

            using (context)
            {
                // Act            
                var (posts, totalPostCount) = await postService.SearchPostsWithTotalCountAsync(Query, PageNumber, PageSize);

                // Assert                            
                Assert.NotEmpty(posts);
                Assert.Equal(PostsCountToSeed, posts.Count);
            }
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnSpecificPost()
        {
            // Arrange
            ApplicationDbContext context;
            var (postService, seededPosts) = CreatePostServiceAndSeedUniqueDb(out context);
            using (context)
            {
                var postId = 1;

                // Act
                var existingPost = await postService.GetPostByIdAsync(postId, includeComments: false);

                // Assert
                Assert.NotNull(existingPost);

                var post = await context.Posts
                    .FirstOrDefaultAsync(p => p.Id == 1);

                Assert.NotNull(post);
                Assert.Equal(post.Title, existingPost.Title);
                Assert.Empty(existingPost.Comments);
            }
        }

        [Fact]
        public async Task AddPostAsync_ShouldAddNewPostSuccessfully()
        {
            // Arrange
            ApplicationDbContext context;
            var (postService, seededPosts) = CreatePostServiceAndSeedUniqueDb(out context);
            using (context)
            {
                var newPost = new Post
                {
                    Title = "Lorem ipsum dolor sit amet",
                    Content = "Lorem Ipsum is simply dummy text of the printing and typesetting industry.",
                    Author = "Test author",
                    Description = "Test description",
                    MetaTitle = "Test meta title",
                    MetaDescription = "Test meta description",
                    ImageUrl = "http://example.com/img/img.jpg",
                    Slug = "post-slug",
                };
                var initialCount = await context.Posts.CountAsync();

                // Act
                await postService.AddPostAsync(newPost);

                // Assert
                var addedPost = await context.Posts
                    .FirstOrDefaultAsync(p => p.Title == newPost.Title);
                Assert.NotNull(addedPost);

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

                existingPost.Title = "Updated title";
                existingPost.Content = "Updated content";

                // Act                
                var updatedPost = await postService.UpdatePostAsync(postId, existingPost);

                // Assert                
                Assert.NotNull(updatedPost);
                Assert.Equal(existingPost.Title, updatedPost.Title);
                Assert.Equal(existingPost.Content, updatedPost.Content);
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

                // Act
                await postService.DeletePostAsync(postId);

                // Assert
                var removedPost = await context.Posts.AnyAsync(p => p.Id == postId);
                Assert.False(removedPost);

                var finalCount = await context.Posts.CountAsync();
                Assert.Equal(initialCount - 1, finalCount);
            }
        }
    }
}
