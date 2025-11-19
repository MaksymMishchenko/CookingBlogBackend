using Microsoft.EntityFrameworkCore;
using PostApiService.Models;
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

        private (PostService Service, List<Post> SeededPosts) CreatePostServiceAndSeedUniqueDb(out ApplicationDbContext context)
        {
            context = _fixture.CreateUniqueContext();

            var postsToSeed = _fixture.GeneratePosts();

            _fixture.SeedDatabaseAsync(context, postsToSeed).Wait();

            var repo = new Repository<Post>(context);
            var service = new PostService(repo);

            return (service, postsToSeed);
        }

        [Fact]
        public async Task GetPostsWithTotalAsync_ReturnsCorrectPageAndCount_WithContentCheck()
        {
            // Arrange
            ApplicationDbContext context;
            var (postService, seededPosts) = CreatePostServiceAndSeedUniqueDb(out context);
            using (context)
            {
                const int expectedTotalCount = 25;
                int expectedPageNumber = 1;
                int expectedPageSize = 10;

                var expectedPostsOnPage = seededPosts
                    .OrderBy(p => p.Id)
                    .Skip((expectedPageNumber - 1) * expectedPageSize)
                    .Take(expectedPageSize)
                    .ToList();

                // Act
                var result = await postService.GetPostsWithTotalAsync
                    (expectedPageNumber, expectedPageSize, includeComments: true);

                // Assert
                Assert.Equal(expectedTotalCount, result.TotalCount);
                Assert.Equal(expectedPageSize, result.Posts.Count);
                Assert.Equal(1, result.Posts.First().Id);
                Assert.Equal(10, result.Posts.Last().Id);

                Assert.True(
                    expectedPostsOnPage.Select(p => p.Id)
                        .SequenceEqual(result.Posts.Select(p => p.Id)),
                    "The order or set of Post IDs on the page does not match the expected.");

                Assert.True(
                    expectedPostsOnPage.Select(p => p.Title)
                        .SequenceEqual(result.Posts.Select(p => p.Title)),
                    "The Post Titles do not match in order.");

                Assert.True(
                    expectedPostsOnPage.Select(p => p.Content)
                        .SequenceEqual(result.Posts.Select(p => p.Content)),
                    "The Post Content does not match in order.");

                for (int i = 0; i < expectedPostsOnPage.Count; i++)
                {
                    var expectedPost = expectedPostsOnPage[i];
                    var actualPost = result.Posts[i];

                    Assert.Equal(expectedPost.Id, actualPost.Id);

                    Assert.Equal(expectedPost.Comments.Count, actualPost.Comments.Count);
                }
            }
        }

        [Fact]
        public async Task GetPostsWithTotalAsync_ShouldReturnLastIncompletePageRange()
        {
            // Arrange
            ApplicationDbContext context;
            var (postService, seededPosts) = CreatePostServiceAndSeedUniqueDb(out context);
            using (context)
            {
                const int expectedTotalCount = 25;
                const int pageSize = 10;
                const int pageNumber = 3;
                const int expectedPostsOnLastPage = 5;

                var expectedPostsOnPage = seededPosts
                    .OrderBy(p => p.Id)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Act
                var result = await postService.GetPostsWithTotalAsync(pageNumber, pageSize);

                // Assert
                Assert.Equal(expectedTotalCount, result.TotalCount);
                Assert.Equal(expectedPostsOnLastPage, result.Posts.Count);
                Assert.Equal(21, result.Posts.First().Id);
                Assert.Equal(expectedTotalCount, result.Posts.Last().Id);

                Assert.True(
                    expectedPostsOnPage.Select(p => p.Title)
                        .SequenceEqual(result.Posts.Select(p => p.Title)),
                    "The Post Titles do not match in order on the last incomplete page.");

                Assert.True(
                    expectedPostsOnPage.Select(p => p.Content)
                        .SequenceEqual(result.Posts.Select(p => p.Content)),
                    "The Post Content does not match in order on the last incomplete page.");

                Assert.All(result.Posts, actualPost =>
                {
                    Assert.NotEmpty(actualPost.Comments);
                    Assert.All(actualPost.Comments, c => Assert.Equal(actualPost.Id, c.PostId));
                });

                for (int i = 0; i < expectedPostsOnPage.Count; i++)
                {
                    var expectedPost = expectedPostsOnPage[i];
                    var actualPost = result.Posts[i];

                    Assert.Equal(expectedPost.Id, actualPost.Id);

                    Assert.Equal(expectedPost.Comments.Count, actualPost.Comments.Count);
                }
            }
        }

        [Fact]
        public async Task GetPostsWithTotalAsync_ShouldReturnPostsWithEmptyComments()
        {
            // Arrange
            ApplicationDbContext context;
            var (postService, seededPosts) = CreatePostServiceAndSeedUniqueDb(out context);

            const int PageNumber = 1;
            const int PageSize = 5;
            const int ExpectedTotalPosts = 25;

            // Act            
            var (posts, totalCount) = await postService.GetPostsWithTotalAsync(
                pageNumber: PageNumber,
                pageSize: PageSize,
                includeComments: false);

            // Assert            
            Assert.Equal(PageSize, posts.Count);

            Assert.Equal(ExpectedTotalPosts, totalCount);

            Assert.All(posts, post =>
            {
                Assert.NotNull(post.Comments);

                Assert.Empty(post.Comments);
            });
        }

        [Fact]
        public async Task GetPostsWithTotalAsync_ShouldPaginateAndSortCommentsCorrectly()
        {
            // Arrange
            ApplicationDbContext context;
            var (postService, seededPosts) = CreatePostServiceAndSeedUniqueDb(out context);

            const int PostPageSize = 3;

            const int CommentsPerPage = 2;
            const int CommentPageNum = 2;
            const int ExpectedPostsCount = 25;

            const int ExpectedCommentCount = CommentsPerPage;

            var expectedPostsOnPage = seededPosts
                    .OrderBy(p => p.Id)
                    .Skip((1 - 1) * PostPageSize)
                    .Take(PostPageSize)
                    .ToList();

            // Act
            var (posts, totalCount) = await postService.GetPostsWithTotalAsync(
                pageNumber: 1,
                pageSize: PostPageSize,
                commentPageNumber: CommentPageNum,
                commentsPerPage: CommentsPerPage,
                includeComments: true);

            // Assert            
            Assert.Equal(PostPageSize, posts.Count);
            Assert.Equal(ExpectedPostsCount, totalCount);

            Assert.True(
                expectedPostsOnPage.Select(p => p.Title).SequenceEqual(posts.Select(p => p.Title)),
                "The Post Titles do not match in order after comment pagination.");
            Assert.True(
                expectedPostsOnPage.Select(p => p.Content).SequenceEqual(posts.Select(p => p.Content)),
                "The Post Content does not match in order after comment pagination.");

            Assert.All(posts, post =>
            {
                Assert.Equal(ExpectedCommentCount, post.Comments.Count);

                var isSorted = post.Comments.SequenceEqual(post.Comments.OrderBy(c => c.CreatedAt));
                Assert.True(isSorted);

                Assert.True(post.Comments.Min(c => c.CreatedAt) > DateTime.MinValue);
            });
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
