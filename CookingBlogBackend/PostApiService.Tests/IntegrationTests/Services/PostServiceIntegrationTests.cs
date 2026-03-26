using PostApiService.Infrastructure.Common;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Tests.IntegrationTests.Services
{
    [Collection("SharedDatabase")]
    public class PostServiceIntegrationTests
    {
        private readonly ServiceTestFixture _fixture;

        public PostServiceIntegrationTests(ServiceTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GetPostsPagedAsync_InNormalMode_ShouldReturnPagedResult()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();

            const int ExpectedPageNumber = 1;
            const int ExpectedPageSize = 10;
            const int ActiveCount = 15;
            const int InactiveCount = 5;
            const int ExpectedCommentCountPerPost = 2;

            var categories = TestDataHelper.GetCulinaryCategories();
            var active = TestDataHelper.GetPostsWithComments(ActiveCount, categories, commentCount: ExpectedCommentCountPerPost);

            active.ForEach(p => { p.IsActive = true; p.Id = 0; });

            var inactive = TestDataHelper.GetPostsWithComments(InactiveCount, categories, commentCount: 0);
            inactive.ForEach(p =>
            {
                p.IsActive = false;
                p.Id = 0;
                p.Slug = $"inactive-{Guid.NewGuid()}";
            });

            var allPosts = active.Concat(inactive).ToList();

            await _fixture.Services!.SeedBlogDataAsync(allPosts, categories);

            var expectedActivePosts = allPosts
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((ExpectedPageNumber - 1) * ExpectedPageSize)
                    .Take(ExpectedPageSize)
                    .ToList();

            var queryDto = new PostQueryDto(
                    SearchTerm: null,
                    CategorySlug: null,
                    PageNumber: ExpectedPageNumber,
                    PageSize: ExpectedPageSize
            );

            var (service, _, _) = _fixture.GetScopedService<IPostService>();

            // Act            
            var result = await service.GetPostsPagedAsync(queryDto);

            // Assert
            Assert.True(result.IsSuccess);

            var data = Assert.IsType<PagedResult<PostListDto>>(result.Value);

            Assert.Equal(ActiveCount, data.TotalCount);
            Assert.Null(data.AppliedFilters!.Search);
            Assert.Null(data.AppliedFilters.CategoryName!);
            Assert.Equal(expectedActivePosts.Count, data.Items.Count());

            Assert.All(data.Items.Select((item, index) => new { item, index }), x =>
            {
                var expected = expectedActivePosts[x.index];

                Assert.Equal(expected.Id, x.item.Id);
                Assert.Equal(expected.Title, x.item.Title);
                Assert.Equal(expected.Category.Name, x.item.Category);
            });
        }

        [Fact]
        public async Task GetPostsPagedAsync_SearchMode_ShouldReturnCorrectSearchDtosAndSnippets()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();

            const string SearchTerm = "pizza";
            const int ExpectedPageNumber = 1;
            const int ExpectedPageSize = 5;
            const int MatchCount = 3;

            var categories = TestDataHelper.GetCulinaryCategories();

            var matches = TestDataHelper.GetPostsWithComments(MatchCount, categories, commentCount: 0);
            matches.ForEach(p =>
            {
                p.Title = $"{SearchTerm} title {Guid.NewGuid()}";
                p.Content = $"This is a long content about {SearchTerm} to generate a snippet.";
                p.IsActive = true;
                p.Id = 0;
            });

            var others = TestDataHelper.GetPostsWithComments(5, categories, commentCount: 0);
            others.ForEach(p =>
            {
                p.Title = $"Regular healthy salad {Guid.NewGuid()}";
                p.Content = "Just some greens and oil.";
                p.IsActive = true;
                p.Id = 0;
                p.Slug = $"regular-salad-{Guid.NewGuid()}";
            });

            var allPosts = matches.Concat(others).ToList();
            await _fixture.Services!.SeedBlogDataAsync(allPosts, categories);

            var queryDto = new PostQueryDto(
                SearchTerm: SearchTerm,
                CategorySlug: null,
                PageNumber: ExpectedPageNumber,
                PageSize: ExpectedPageSize
            );

            var (service, _, _) = _fixture.GetScopedService<IPostService>();

            // Act
            var result = await service.GetPostsPagedAsync(queryDto);

            // Assert
            Assert.True(result.IsSuccess);

            var data = Assert.IsType<PagedSearchResult<SearchPostListDto>>(result.Value);

            Assert.Equal(MatchCount, data.TotalCount);
            Assert.Equal(SearchTerm, data.AppliedFilters!.Search);
            Assert.Null(data.AppliedFilters.CategoryName);

            Assert.All(data.Items, item =>
            {
                Assert.Contains(SearchTerm, item.Title.ToLower());
                Assert.NotEmpty(item.SearchSnippet!);
                Assert.Contains(SearchTerm, item.SearchSnippet!.ToLower());
            });
        }

        [Fact]
        public async Task GetAdminPostsPagedAsync_SearchMode_ShouldReturnCorrectAdminDtos()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();

            var (service, _, webContext) = _fixture.GetScopedService<IPostService>();
            webContext.UserId = TestUserData.AdminId;
            webContext.UserName = TestUserData.AdminUserName;
            webContext.IsAdmin = true;

            const string SearchTerm = "burger";
            const int ExpectedPageNumber = 1;
            const int ExpectedPageSize = 5;
            const int MatchCount = 3;
            var ct = CancellationToken.None;

            var categories = TestDataHelper.GetCulinaryCategories();

            var matches = TestDataHelper.GetPostsWithComments(MatchCount, categories, commentCount: 0);
            matches.ForEach(p =>
            {
                p.Title = $"{SearchTerm} variant {Guid.NewGuid()}";
                p.IsActive = true;
                p.Id = 0;
                p.Slug = $"burger-match-{Guid.NewGuid()}";
            });

            var others = TestDataHelper.GetPostsWithComments(5, categories, commentCount: 0);
            others.ForEach(p =>
            {
                p.Title = $"Generic soup recipe {Guid.NewGuid()}";
                p.IsActive = true;
                p.Id = 0;
                p.Slug = $"soup-recipe-{Guid.NewGuid()}";
            });

            var allPosts = matches.Concat(others).ToList();

            await _fixture.Services!.SeedBlogDataAsync(allPosts, categories);

            var queryDto = new PostAdminQueryDto(
                SearchTerm: SearchTerm,
                CategorySlug: null,
                PageNumber: ExpectedPageNumber,
                PageSize: ExpectedPageSize,
                OnlyActive: null
            );

            // Act
            var result = await service.GetAdminPostsPagedAsync(queryDto, ct);

            // Assert            
            Assert.True(result.IsSuccess);

            var data = Assert.IsType<PagedResult<AdminPostListDto>>(result.Value);

            Assert.Equal(SearchTerm, data.AppliedFilters!.Search);
            Assert.Null(data.AppliedFilters.CategoryName);
            Assert.Equal(MatchCount, data.TotalCount);

            Assert.All(data.Items, item =>
            {
                Assert.Contains(SearchTerm, item.Title.ToLower());
                Assert.True(item.Id > 0);
                Assert.NotNull(item.CategoryName);
            });
        }

        [Fact]
        public async Task GetAdminPostsPagedAsync_FilterByInactive_ReturnsOnlyInactivePosts()
        {   // Arrange        
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();

            var (service, _, webContext) = _fixture.GetScopedService<IPostService>();
            webContext.UserId = TestUserData.AdminId;
            webContext.UserName = TestUserData.AdminUserName;
            webContext.IsAdmin = true;

            const int InactiveMatchCount = 2;
            const int ActiveCount = 5;
            var categories = TestDataHelper.GetCulinaryCategories();

            var activePosts = TestDataHelper.GetPostsWithComments(ActiveCount, categories, commentCount: 0);
            activePosts.ForEach(p =>
            {
                p.IsActive = true;
                p.Id = 0;
                p.Slug = $"active-{Guid.NewGuid()}";
            });

            var inactivePosts = TestDataHelper.GetPostsWithComments(InactiveMatchCount, categories, commentCount: 0);
            inactivePosts.ForEach(p =>
            {
                p.IsActive = false;
                p.Id = 0;
                p.Slug = $"inactive-{Guid.NewGuid()}";
            });

            var allPosts = activePosts.Concat(inactivePosts).ToList();
            await _fixture.Services!.SeedBlogDataAsync(allPosts, categories);

            var queryDto = new PostAdminQueryDto(
                SearchTerm: null,
                CategorySlug: null,
                PageNumber: 1,
                PageSize: 10,
                OnlyActive: false
            );

            // Act
            var result = await service.GetAdminPostsPagedAsync(queryDto);

            // Assert
            Assert.True(result.IsSuccess);

            var data = Assert.IsType<PagedResult<AdminPostListDto>>(result.Value);

            Assert.Equal(InactiveMatchCount, data.TotalCount);

            Assert.All(data.Items, item =>
            {
                Assert.False(item.IsActive);
            });

            Assert.Null(data.AppliedFilters!.Search);
            Assert.Null(data.AppliedFilters.CategoryName);
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnSuccess_IfPostExistsInDb()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(25, categories, commentCount: 5);
            posts.ForEach(p => { p.Id = 0; p.Slug = Guid.NewGuid().ToString(); });

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var targetPost = posts.First();

            var (service, _, _) = _fixture.GetScopedService<IPostService>();

            // Act
            var result = await service.GetPostByIdAsync(targetPost.Id);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);

            var data = Assert.IsType<PostAdminDetailsDto>(result.Value);
            Assert.NotNull(data);

            Assert.Equal(targetPost.Title, data.Title);
            Assert.Equal(targetPost.Author, data.Author);
            Assert.Equal(targetPost.Slug, data.Slug);
        }

        [Fact]
        public async Task GetPostBySlugAsync_ShouldReturnSuccess_IfPostExistsInDbAndIsActive()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();

            const int ExpectedCommentCount = 5;
            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(3, categories, commentCount: ExpectedCommentCount);

            posts.ForEach(p =>
            {
                p.Id = 0;
                p.IsActive = true;
                p.Slug = $"slug-{Guid.NewGuid()}";
            });

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var targetPost = posts.First();
            var requestDto = new PostRequestBySlug
            {
                Category = targetPost.Category.Slug,
                Slug = targetPost.Slug
            };

            var (service, _, _) = _fixture.GetScopedService<IPostService>();

            // Act
            var result = await service.GetPostBySlugAsync(requestDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);

            var data = Assert.IsType<PostDetailsDto>(result.Value);
            Assert.NotNull(data);

            Assert.Equal(targetPost.Title, data.Title);
            Assert.Equal(targetPost.Author, data.Author);
            Assert.Equal(targetPost.Slug, data.Slug);
            Assert.Equal(targetPost.Category.Slug, data.CategorySlug);
            Assert.Equal(targetPost.Category.Name, data.Category);
            Assert.Equal(ExpectedCommentCount, data.CommentCount);
        }

        [Fact]
        public async Task AddPostAsync_ShouldReturnSuccess_WhenPostAddedSuccessfully()
        {
            // Arrange           
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();

            var (service, dbContextBefore, webContext) = _fixture.GetScopedService<IPostService>();
            webContext.UserId = TestUserData.AdminId;
            webContext.UserName = TestUserData.AdminUserName;
            webContext.IsAdmin = true;

            var categories = TestDataHelper.GetCulinaryCategories();
            await _fixture.Services!.SeedBlogDataAsync(new List<Post>(), categories);

            var existingCategoryId = categories.First().Id;

            var newPost = TestDataHelper.GetSinglePostWithCategoryId(existingCategoryId);
            var postDto = TestDataHelper.ToPostCreateDto(newPost);

            var initialCount = await dbContextBefore.Posts.CountAsync();

            // Act
            var result = await service.AddPostAsync(postDto);

            // Assert                
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);

            var data = Assert.IsType<PostAdminDetailsDto>(result.Value);
            Assert.NotNull(data);
            Assert.Equal(postDto.Title, data.Title);
            Assert.Equal(existingCategoryId, data.CategoryId);

            var (_, dbContextAfter, _) = _fixture.GetScopedService<IPostService>();

            var addedPostInDb = await dbContextAfter.Posts
                .FirstOrDefaultAsync(p => p.Id == data.Id);

            Assert.NotNull(addedPostInDb);
            Assert.True(data.Id > 0);
            Assert.Equal(postDto.Content, addedPostInDb.Content);
            Assert.Equal(postDto.Slug, addedPostInDb.Slug);

            var finalCount = await dbContextAfter.Posts.CountAsync();
            Assert.Equal(initialCount + 1, finalCount);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldUpdateExistingPostSuccessfully()
        {
            // Arrange           
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();

            var (service, dbContextBefore, webContext) = _fixture.GetScopedService<IPostService>();
            webContext.UserId = TestUserData.AdminId;
            webContext.UserName = TestUserData.AdminUserName;
            webContext.IsAdmin = true;

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(1, categories, commentCount: 0);
            posts.ForEach(p => { p.Id = 0; p.Slug = $"original-{Guid.NewGuid()}"; });

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var targetPost = posts.First();
            var updatedTitle = "New Receipt Title";

            var updateDto = TestDataHelper.ToPostUpdateDto(targetPost, updatedTitle);

            // Act                                     
            var result = await service.UpdatePostAsync(targetPost.Id, updateDto);

            // Assert                
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);

            var (_, dbContextAfter, _) = _fixture.GetScopedService<IPostService>();

            var postInDb = await dbContextAfter.Posts
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == targetPost.Id);

            Assert.NotNull(postInDb);
            Assert.Equal(updateDto.Title, postInDb.Title);
            Assert.Equal(updateDto.Content, postInDb.Content);
            Assert.Equal(PostM.Success.PostUpdatedSuccessfully, result.Message);

            Assert.NotNull(postInDb.UpdatedAt);
            Assert.True(postInDb.UpdatedAt > DateTime.UtcNow.AddMinutes(-1));

            var data = Assert.IsType<PostAdminDetailsDto>(result.Value);
            DateAssert.EqualWithPrecision(postInDb.UpdatedAt, data.UpdatedAt);

            var totalCount = await dbContextAfter.Posts.CountAsync();
            Assert.Equal(posts.Count, totalCount);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldRemovePostSuccessfully()
        {
            // Arrange           
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();

            var (service, dbContextBefore, webContext) = _fixture.GetScopedService<IPostService>();
            webContext.UserId = TestUserData.AdminId;
            webContext.UserName = TestUserData.AdminUserName;
            webContext.IsAdmin = true;

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(3, categories, commentCount: 0);
            posts.ForEach(p => { p.Id = 0; p.Slug = $"delete-target-{Guid.NewGuid()}"; });

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var targetPost = posts.First();
            var initialCount = await dbContextBefore.Posts.CountAsync();

            // Act
            var result = await service.DeletePostAsync(targetPost.Id);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.Equal(PostM.Success.PostDeletedSuccessfully, result.Message);

            var (_, dbContextAfter, _) = _fixture.GetScopedService<IPostService>();

            var postExists = await dbContextAfter.Posts.AnyAsync(p => p.Id == targetPost.Id);
            Assert.False(postExists);

            var finalCount = await dbContextAfter.Posts.CountAsync();
            Assert.Equal(initialCount - 1, finalCount);
        }

        [Fact]
        public async Task DeletePostAsync_ShouldAlsoRemoveAssociatedComments()
        {
            // Arrange           
            await _fixture.ResetDatabaseAsync();
            await _fixture.Services!.SeedDefaultUsersAsync();

            var (service, dbContext, webContext) = _fixture.GetScopedService<IPostService>();
            webContext.UserId = TestUserData.AdminId;
            webContext.UserName = TestUserData.AdminUserName;
            webContext.IsAdmin = true;

            const int CommentCount = 5;
            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(1, categories, commentCount: CommentCount);
            posts.ForEach(p => { p.Id = 0; p.Slug = $"cascade-delete-{Guid.NewGuid()}"; });

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var targetPost = posts.First();

            // Act
            await service.DeletePostAsync(targetPost.Id);

            // Assert
            var (_, dbContextAfter, _) = _fixture.GetScopedService<IPostService>();

            var orphanCommentsExist = await dbContextAfter.Comments.AnyAsync(c => c.PostId == targetPost.Id);
            Assert.False(orphanCommentsExist);

            var postExists = await dbContextAfter.Posts.AnyAsync(p => p.Id == targetPost.Id);
            Assert.False(postExists);
        }
    }
}
