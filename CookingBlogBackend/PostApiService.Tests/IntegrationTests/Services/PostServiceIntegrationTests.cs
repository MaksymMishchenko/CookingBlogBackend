using PostApiService.Infrastructure.Common;
using PostApiService.Infrastructure.Services;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Response;
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
        public async Task GetPostsPagedAsync_InNormalMode_ShouldReturnPagedResultWithCorrectMapping()
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
                active.ForEach(p =>
                {
                    p.IsActive = true;
                    p.Id = 0;
                });

                var inactive = _fixture.GeneratePosts(InactiveCount, cats, 0);
                inactive.ForEach(p =>
                {
                    p.IsActive = false;
                    p.Id = 0;
                    p.Slug = $"inactive-{Guid.NewGuid()}";
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
                var result = await postService.GetPostsPagedAsync(pageNumber: ExpectedPageNumber, pageSize: ExpectedPageSize);

                // Assert                
                Assert.True(result.IsSuccess);

                var data = Assert.IsType<PagedResult<PostListDto>>(result.Value);

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
        public async Task GetPostsPagedAsync_SearchMode_ReturnsCorrectSearchDtosAndSnippets()
        {
            // Arrange
            const string SearchTerm = "pizza";
            const int ExpectedPageSize = 5;
            const int MatchCount = 3;

            var (context, postService, allSeededPosts, _) = await SetupAsync(cats =>
            {
                var matches = _fixture.GeneratePosts(MatchCount, cats, 0);
                matches.ForEach(p =>
                {
                    p.Title = $"{SearchTerm} title {Guid.NewGuid()}";
                    p.IsActive = true;
                    p.Id = 0;
                });

                var others = _fixture.GeneratePosts(5, cats, 0);
                others.ForEach(p =>
                {
                    p.Title = "Regular salad";
                    p.IsActive = true;
                    p.Id = 0;
                });

                return matches.Concat(others).ToList();
            });

            using (context)
            {
                // Act
                var result = await postService.GetPostsPagedAsync(search: SearchTerm, pageSize: ExpectedPageSize);

                // Assert
                Assert.True(result.IsSuccess);

                var data = Assert.IsType<PagedSearchResult<SearchPostListDto>>(result.Value);

                Assert.Equal(MatchCount, data.TotalCount);
                Assert.Equal(SearchTerm, data.Query);

                Assert.All(data.Items, item =>
                {
                    Assert.Contains(SearchTerm, item.Title.ToLower());
                    Assert.NotNull(item.SearchSnippet);
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
