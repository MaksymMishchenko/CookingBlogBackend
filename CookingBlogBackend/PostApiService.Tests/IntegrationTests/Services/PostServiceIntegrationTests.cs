using PostApiService.Infrastructure.Common;
using PostApiService.Infrastructure.Services;
using PostApiService.Interfaces;
using PostApiService.Repositories;
using PostApiService.Services;

namespace PostApiService.Tests.IntegrationTests.Services
{
    public class PostServiceIntegrationTests : IClassFixture<InMemoryDatabaseFixture>
    {
        private readonly InMemoryDatabaseFixture _fixture;
        private readonly IdentityUser _testUser;

        public PostServiceIntegrationTests(InMemoryDatabaseFixture fixture)
        {
            _fixture = fixture;

            _testUser = new IdentityUser
            {
                Id = "testContId",
                UserName = "TestBob",
                Email = "bob@test.com"
            };
        }

        private record TestSetup(
            ApplicationDbContext Context,
            PostService Service,
            List<Post> Posts,
            List<Category> Categories);

        private TestSetup CreateTestSetup(ApplicationDbContext context, List<Post> posts, List<Category> categories)
        {
            var repo = new PostRepository(context);
            var webContextMock = Substitute.For<IWebContext>();
            var sanitizeServiceMock = Substitute.For<IHtmlSanitizationService>();
            var catService = new CategoryService(new Repository<Category>(context), repo);

            webContextMock.UserId.Returns(_testUser.Id);
            sanitizeServiceMock.SanitizePost(Arg.Any<string>()).Returns(x => x.Arg<string>());
            webContextMock.UserName.Returns(_testUser.UserName);

            var service = new PostService(repo, webContextMock, sanitizeServiceMock, catService, new SnippetGeneratorService());

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
        public async Task GetPostsPagedAsync_ReturnsOnlyActivePostsWithCorrectCounts()
        {
            // Arrange            
            const int ExpectedPageNumber = 1;
            const int ExpectedPageSize = 10;
            const int ActiveCount = 15;
            const int InactiveCount = 5;
            const int ExpectedCommentCountPerPost = 2;

            var (context, postService, allSeededPosts, _) = await SetupAsync(cats =>
            {
                var active = _fixture.GeneratePosts(ActiveCount, cats, ExpectedCommentCountPerPost);
                active.ForEach(p => p.IsActive = true);

                var inactive = _fixture.GeneratePosts(InactiveCount, cats, 0);

                int lastActiveId = active.Max(p => p.Id);
                inactive.ForEach(p =>
                {
                    p.IsActive = false;
                    p.Id = ++lastActiveId;
                    p.Slug = $"inactive-{p.Slug}-{p.Id}";
                });

                return active.Concat(inactive).ToList();
            });

            using (context)
            {
                var expectedActivePosts = allSeededPosts
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((ExpectedPageNumber - 1) * ExpectedPageSize)
                    .Take(ExpectedPageSize)
                    .ToList();

                // Act
                var result = await postService.GetPostsPagedAsync(ExpectedPageNumber, ExpectedPageSize);

                // Assert                
                Assert.True(result.IsSuccess);
                var data = result.Value!;

                Assert.Equal(ActiveCount, data.TotalCount);
                Assert.Equal(expectedActivePosts.Count, data.Items.Count());

                Assert.All(data.Items.Select((item, index) => new { item, index }), x =>
                {
                    var expectedPost = expectedActivePosts[x.index];

                    TestDataHelper.AssertPostListDtoMapping(expectedPost, x.item, ExpectedCommentCountPerPost);

                    var original = allSeededPosts.First(p => p.Id == x.item.Id);
                    Assert.True(original.IsActive);
                });
            }
        }

        [Fact]
        public async Task GetAdminPostsPagedAsync_ShouldReturnBothActiveAndInactivePosts_WhenFilterIsNull()
        {
            // Arrange            
            const int ExpectedPageNumber = 1;
            const int ExpectedPageSize = 10;
            const int ActiveCount = 15;
            const int InactiveCount = 5;
            const int TotalExpectedCount = ActiveCount + InactiveCount;

            var (context, postService, allSeededPosts, _) = await SetupAsync(cats =>
            {
                var active = _fixture.GeneratePosts(ActiveCount, cats, 2);
                active.ForEach(p => {
                    p.IsActive = true;
                    p.Id = 0;
                });

                var inactive = _fixture.GeneratePosts(InactiveCount, cats, 0);                
                inactive.ForEach(p => {
                    p.IsActive = false;
                    p.Id = 0;
                    p.Slug = $"inactive-{Guid.NewGuid()}";
                });

                return active.Concat(inactive).ToList();
            });

            using (context)
            {                
                var expectedPosts = allSeededPosts
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((ExpectedPageNumber - 1) * ExpectedPageSize)
                    .Take(ExpectedPageSize)
                    .ToList();

                // Act
                var result = await postService.GetAdminPostsPagedAsync(
                    isActive: null,
                    pageNumber: ExpectedPageNumber,
                    pageSize: ExpectedPageSize);

                // Assert                
                Assert.True(result.IsSuccess);
                var data = result.Value!;
                
                Assert.Equal(TotalExpectedCount, data.TotalCount);                
                Assert.Equal(expectedPosts.Count, data.Items.Count());
                
                Assert.All(data.Items.Select((item, index) => new { item, index }), x =>
                {
                    var expectedPost = expectedPosts[x.index];
                    Assert.Equal(expectedPost.Id, x.item.Id);
                    Assert.Equal(expectedPost.IsActive, x.item.IsActive);
                });
            }
        }

        [Fact]
        public async Task SearchActivePostsPagedAsync_ShouldFindOnlyActivePosts_InTitleOrDescriptionOrContent()
        {
            // Arrange            
            const string Query = "Chili";

            var (context, postService, allSeededPosts, _) = await SetupAsync(categories =>
            {
                var posts = _fixture.GeneratePostsWithKeywords(categories);

                int halfCount = posts.Count / 2;
                var activePart = posts.Take(halfCount).ToList();
                var inactivePart = posts.Skip(halfCount).ToList();

                activePart.ForEach(p => p.IsActive = true);

                inactivePart.ForEach(p =>
                {
                    p.IsActive = false;
                    p.Title += $" {Query} (Hidden)";
                    p.Slug = $"hidden-{p.Slug}";
                });

                return activePart.Concat(inactivePart).ToList();
            });

            var expectedActiveMatchedIds = allSeededPosts
                .Where(p => p.IsActive && (
                            p.Title.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                            p.Description.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                            p.Content.Contains(Query, StringComparison.OrdinalIgnoreCase)))
                .Select(p => p.Id)
                .OrderBy(id => id)
                .ToList();

            // Act
            var result = await postService.SearchActivePostsPagedAsync(Query);

            // Assert           
            Assert.True(result.IsSuccess);
            var data = result.Value!;

            var actualIds = data.Items.Select(r => r.Id).OrderBy(id => id).ToList();

            Assert.Equal(expectedActiveMatchedIds, actualIds);
            Assert.Equal(expectedActiveMatchedIds.Count, data.TotalSearchCount);

            Assert.All(data.Items, item =>
            {
                var original = allSeededPosts.First(p => p.Id == item.Id);
                Assert.True(original.IsActive);
            });
        }

        [Fact]
        public async Task GetActivePostsByCategoryPagedAsync_ShouldReturnOnlyFilteredPosts_AndCorrectTotalCount()
        {
            // Arrange
            const string targetSlug = "breakfast";
            const int pageNumber = 1;
            const int pageSize = 10;
            const int postsInTargetCategory = 5;
            const int postsInOtherCategory = 3;

            var (context, postService, _, allCategories) = await SetupAsync(cats =>
            {
                var targetCat = cats.First(c => c.Slug == targetSlug);
                var otherCat = cats.First(c => c.Slug != targetSlug);

                var targetPosts = TestDataHelper.GetPostsWithComments(
                    postsInTargetCategory, cats, forcedCategory: targetCat);

                var otherPosts = TestDataHelper.GetPostsWithComments(
                    postsInOtherCategory, cats, forcedCategory: otherCat);

                return targetPosts.Concat(otherPosts).ToList();
            });

            using (context)
            {
                // Act
                var result = await postService.GetActivePostsByCategoryPagedAsync(targetSlug, pageNumber, pageSize);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.Equal(ResultStatus.Success, result.Status);

                var data = result.Value!;

                Assert.Equal(postsInTargetCategory, data.TotalCount);
                Assert.Equal(postsInTargetCategory, data.Items.Count());

                Assert.All(data.Items, item =>
                {
                    Assert.Equal(targetSlug, item.CategorySlug);
                });

                var sortedIds = data.Items.Select(i => i.Id).ToList();
                var expectedIds = data.Items.OrderByDescending(i => i.CreatedAt).Select(i => i.Id).ToList();
                Assert.Equal(expectedIds, sortedIds);
            }
        }

        [Fact]
        public async Task GetActivePostsByCategoryPagedAsync_ShouldReturnOnlyActivePosts_ForSpecificCategory()
        {
            // Arrange
            const string targetCategorySlug = "desserts";
            const int ExpectedActiveInCat = 3;
            const int ExpectedComments = 2;
            const int InactiveInCat = 2;

            var (context, postService, allSeededPosts, categories) = await SetupAsync(cats =>
            {
                var targetCat = cats.First(c => c.Slug == targetCategorySlug);
                var otherCat = cats.First(c => c.Slug != targetCategorySlug);

                var activeInCat = _fixture.GeneratePosts(ExpectedActiveInCat, [targetCat], ExpectedComments);
                activeInCat.ForEach(p => p.IsActive = true);

                var inactiveInCat = _fixture.GeneratePosts(InactiveInCat, [targetCat], 0);
                int lastId = activeInCat.Max(p => p.Id);
                inactiveInCat.ForEach(p =>
                {
                    p.IsActive = false;
                    p.Id = ++lastId;
                    p.Slug = $"inactive-{p.Slug}-{p.Id}";
                });

                var activeOtherCat = _fixture.GeneratePosts(2, new[] { otherCat }, 0);
                activeOtherCat.ForEach(p =>
                {
                    p.IsActive = true;
                    p.Id = ++lastId;
                    p.Slug = $"other-{p.Slug}-{p.Id}";
                });

                return activeInCat.Concat(inactiveInCat).Concat(activeOtherCat).ToList();
            });

            using (context)
            {
                // Act
                var result = await postService.GetActivePostsByCategoryPagedAsync(targetCategorySlug, 1, 10);

                // Assert
                Assert.True(result.IsSuccess);
                var data = result.Value!;

                Assert.Equal(ExpectedActiveInCat, data.TotalCount);
                Assert.Equal(ExpectedActiveInCat, data.Items.Count());

                Assert.All(data.Items, item =>
                {
                    Assert.Equal(targetCategorySlug, item.CategorySlug);

                    var original = allSeededPosts.First(p => p.Id == item.Id);
                    Assert.True(original.IsActive);
                    TestDataHelper.AssertPostListDtoMapping(original, item, ExpectedComments);
                });
            }
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
        public async Task GetActivePostBySlugAsync_ShouldReturnSuccess_IfPostExistsInDbAndIsActive()
        {
            // Arrange                        
            const int ExpectedCommentCount = 5;

            var (context, postService, seededPosts, _) = await SetupAsync(categories =>
            {
                var posts = _fixture.GeneratePosts(3, categories, ExpectedCommentCount);
                posts.ForEach(p => p.IsActive = true);
                return posts;
            });

            var targetPost = seededPosts.First();

            var requestDto = TestDataHelper.CreatePostRequest(targetPost.Category.Slug, targetPost.Slug);

            using (context)
            {
                // Act
                var result = await postService.GetActivePostBySlugAsync(requestDto);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.Equal(ResultStatus.Success, result.Status);

                var data = result.Value;
                Assert.NotNull(data);

                Assert.Equal(targetPost.Title, data.Title);
                Assert.Equal(targetPost.Author, data.Author);
                Assert.Equal(targetPost.Slug, data.Slug);
                Assert.Equal(targetPost.Category.Slug, data.CategorySlug);
                Assert.Equal(targetPost.Category.Name, data.Category);

                Assert.Equal(ExpectedCommentCount, data.CommentCount);
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
                Assert.True(data.Id > 0);
                Assert.Equal(data.Id, addedPostInDb.Id);
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

                Assert.NotNull(postInDb.UpdatedAt);
                Assert.True(postInDb.UpdatedAt > DateTime.UtcNow.AddMinutes(-1));
                Assert.Equal(postInDb.UpdatedAt, result.Value!.UpdatedAt);

                var totalCount = await context.Posts.CountAsync();
                Assert.Equal(seededPosts.Count, totalCount);
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

            using (context)
            {
                // Act
                var result = await postService.DeletePostAsync(targetPost.Id);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.Equal(ResultStatus.Success, result.Status);
                Assert.Equal(PostM.Success.PostDeletedSuccessfully, result.Message);

                var postExists = await context.Posts.AnyAsync(p => p.Id == targetPost.Id);
                Assert.False(postExists);

                var finalCount = await context.Posts.CountAsync();
                Assert.Equal(initialCount - 1, finalCount);
            }
        }

        [Fact]
        public async Task DeletePostAsync_ShouldAlsoRemoveAssociatedComments()
        {
            // Arrange           
            var (context, postService, seededPosts, _) = await SetupAsync(cats =>
                _fixture.GeneratePosts(1, cats, commentCount: 5));

            var targetPost = seededPosts.First();

            using (context)
            {
                // Act
                await postService.DeletePostAsync(targetPost.Id);

                // Assert               
                var orphanCommentsExist = await context.Comments.AnyAsync(c => c.PostId == targetPost.Id);
                Assert.False(orphanCommentsExist);
            }
        }
    }
}
