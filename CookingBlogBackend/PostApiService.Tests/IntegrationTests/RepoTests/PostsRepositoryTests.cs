using Microsoft.Extensions.DependencyInjection;
using PostApiService.Repositories;

namespace PostApiService.Tests.IntegrationTests.RepoTests
{
    [Collection("SharedDatabase")]
    public class PostsRepositoryTests
    {
        private readonly ServiceTestFixture _fixture;

        public PostsRepositoryTests(ServiceTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Theory]
        [InlineData("SpecialQuery", true, "beverages", 1)]
        [InlineData(null, false, null, 1)]
        [InlineData(null, null, "desserts", 1)]
        [InlineData("NonExistent", null, null, 0)]
        public async Task GetPublicFilteredPosts_VarietyTests(
            string? search, bool? active, string? slug, int expectedCount)
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();

            await _fixture.Services!.SeedDefaultUsersAsync();

            var categories = TestDataHelper.GetCulinaryCategories();

            string[] authorIds = new[] { TestUserData.AdminId, TestUserData.ContributorId };
            var activePosts = TestDataHelper.GetPostsWithComments(5, categories, authorIds: authorIds, commentCount: 1);
            activePosts.ForEach(p => { p.IsActive = true; p.Id = 0; });

            activePosts[0].Title = "This is a SpecialQuery item";
            activePosts[0].Category = categories.First(c => c.Slug == "beverages");

            activePosts[1].Category = categories.First(c => c.Slug == "desserts");

            var inactivePosts = TestDataHelper.GetPostsWithComments(1, categories, authorIds: authorIds, commentCount: 0);
            inactivePosts.ForEach(p =>
            {
                p.IsActive = false;
                p.Id = 0;
                p.Slug = $"inactive-{Guid.NewGuid()}";
            });

            var allPosts = activePosts.Concat(inactivePosts).ToList();

            await _fixture.Services!.SeedBlogDataAsync(allPosts, categories);

            // Act       
            using var scope = _fixture.Services!.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IPostRepository>();

            var result = repo.GetPublicFilteredPosts(search, active, slug).ToList();

            // Assert
            Assert.Equal(expectedCount, result.Count);

            if (expectedCount > 0 && !string.IsNullOrEmpty(search))
            {
                Assert.Contains(result, p =>
                    p.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(search, StringComparison.OrdinalIgnoreCase)
                );
            }
        }

        [Fact]
        public async Task GetPublicFilteredPosts_ShouldSearchInTitleDescriptionAndContent()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();

            const string Query = "soups";
            var categories = TestDataHelper.GetCulinaryCategories();

            string[] authorIds = new[] { TestUserData.AdminId, TestUserData.ContributorId };
            var posts = TestDataHelper.GetPostsWithComments(3, categories, authorIds: authorIds, commentCount: 0);

            posts[0].Title = $"Best {Query} for winter";
            posts[0].IsActive = true;
            posts[0].Id = 0;

            posts[1].Description = $"This article is about {Query}";
            posts[1].IsActive = true;
            posts[1].Id = 0;

            posts[2].Content = $"You should try this {Query} recipe at home";
            posts[2].IsActive = true;
            posts[2].Id = 0;

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            // Act
            using var scope = _fixture.Services!.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IPostRepository>();

            var result = repo.GetPublicFilteredPosts(Query, null, null).ToList();

            // Assert
            Assert.Equal(3, result.Count);

            Assert.All(result, p =>
            {
                bool existsInAnyField =
                    p.Title.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                    (p.Description != null && p.Description.Contains(Query, StringComparison.OrdinalIgnoreCase)) ||
                    p.Content.Contains(Query, StringComparison.OrdinalIgnoreCase);

                Assert.True(existsInAnyField, $"Word '{Query}' not found in any field of post {p.Id}");
            });
        }

        [Fact]
        public async Task GetAdminFilteredAndSortedPosts_ShouldFilterAndSortCorrectly_WhenParametersProvided()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();

            var categories = TestDataHelper.GetCulinaryCategories();

            string targetAuthorId = TestUserData.ContributorId;
            string otherAuthorId = TestUserData.AdminId;
            
            var posts = TestDataHelper.GetPostsWithComments(4, categories, authorIds: new[] { targetAuthorId, otherAuthorId }, commentCount: 0);
           
            posts[0].Title = "B Post by Target Author";
            posts[0].IsActive = true;
            posts[0].CategoryId = 1;
            posts[0].Category = categories[0];
            posts[0].AuthorId = targetAuthorId;
            posts[0].CreatedAt = DateTime.UtcNow.AddDays(-2);
            posts[0].Id = 0;
            
            posts[1].Title = "A Post by Target Author";
            posts[1].IsActive = true;
            posts[1].CategoryId = 1;
            posts[1].Category = categories[0];
            posts[1].AuthorId = targetAuthorId;
            posts[1].CreatedAt = DateTime.UtcNow.AddDays(-1);
            posts[1].Id = 0;
            
            posts[2].Title = "Draft Post by Target Author";
            posts[2].IsActive = false;
            posts[2].CategoryId = 1;
            posts[2].Category = categories[0];
            posts[2].AuthorId = targetAuthorId;
            posts[2].CreatedAt = DateTime.UtcNow;
            posts[2].Id = 0;
            
            posts[3].Title = "Post by Other Author";
            posts[3].IsActive = true;
            posts[3].CategoryId = 1;
            posts[3].Category = categories[0];
            posts[3].AuthorId = otherAuthorId;
            posts[3].CreatedAt = DateTime.UtcNow;
            posts[3].Id = 0;

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            // Act
            using var scope = _fixture.Services!.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IPostRepository>();

            var result = repo.GetAdminFilteredAndSortedPosts(
                search: null,
                onlyActive: true,
                categoryId: 1,
                sortBy: "title",
                sortDirection: "asc",
                authorId: targetAuthorId
            ).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("A Post by Target Author", result[0].Title);
            Assert.Equal("B Post by Target Author", result[1].Title);
            
            Assert.All(result, p => Assert.Equal(targetAuthorId, p.AuthorId));
            Assert.All(result, p => Assert.True(p.IsActive));
        }

        [Fact]
        public async Task GetByIdWithAuthorsAsync_ShouldReturnPostWithAuthor_WhenPostExists()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();

            var categories = TestDataHelper.GetCulinaryCategories();
            string[] authorIds = new[] { TestUserData.AdminId };
            var posts = TestDataHelper.GetPostsWithComments(1, categories, authorIds: authorIds, commentCount: 0);

            posts[0].Title = "Post with Author Test";
            posts[0].IsActive = true;
            posts[0].Id = 0;

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            using var arrangeScope = _fixture.Services!.CreateScope();
            var arrangeRepo = arrangeScope.ServiceProvider.GetRequiredService<IPostRepository>();
            var createdPost = arrangeRepo.GetAdminFilteredAndSortedPosts(null, null, null, null, null, null).First();

            // Act       
            using var actScope = _fixture.Services!.CreateScope();
            var actRepo = actScope.ServiceProvider.GetRequiredService<IPostRepository>();
            var result = await actRepo.GetByIdWithAuthorsAsync(createdPost.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Post with Author Test", result.Title);
            Assert.NotNull(result.Author);
            Assert.Equal(TestUserData.AdminId, result.Author.Id);
        }
    }
}