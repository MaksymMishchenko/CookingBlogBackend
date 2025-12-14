using MockQueryable;
using NSubstitute;
using PostApiService.Interfaces;
using PostApiService.Models;
using PostApiService.Repositories;
using PostApiService.Services;
using System.Linq.Expressions;

namespace PostApiService.Tests.UnitTests
{
    public class PostServiceTests
    {
        private readonly IRepository<Post> _mockRepository;
        private readonly ISnippetGeneratorService _mockSnippetGenerator;

        public PostServiceTests()
        {
            _mockRepository = Substitute.For<IRepository<Post>>();
            _mockSnippetGenerator = Substitute.For<ISnippetGeneratorService>();
        }

        [Fact]
        public async Task GetPostsWithTotalPostCountAsync_ShouldReturn_PagedPostsWithTotalPostsAndCommentsCount()
        {
            // Arrange            
            const int PageNumber = 1;
            const int PageSize = 10;
            const int ExpectedTotalPostCount = 25;
            const int ExpectedCommentCountPerPost = 11;

            var testPosts = TestDataHelper.GetPostsWithComments
                (count: ExpectedTotalPostCount, commentCount: ExpectedCommentCountPerPost);
            var mockQueryable = testPosts.AsQueryable().BuildMock();

            _mockRepository.AsQueryable().Returns(mockQueryable);

            _mockRepository.GetTotalCountAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(ExpectedTotalPostCount));

            var service = new PostService(_mockRepository, _mockSnippetGenerator);

            // Act
            var result = await service.GetPostsWithTotalPostCountAsync(PageNumber, PageSize);

            // Assert
            Assert.Equal(PageSize, result.Posts.Count);
            Assert.Equal(ExpectedTotalPostCount, result.TotalPostCount);

            Assert.All(result.Posts, (postDto, index) =>
            {
                int expectedIndex = (PageNumber - 1) * PageSize + index;
                var expectedPost = testPosts[expectedIndex];

                TestDataHelper.AssertPostListDtoMapping
                (expectedPost, postDto, ExpectedCommentCountPerPost);
            });
        }

        [Fact]
        public async Task GetPostsWithTotalPostCountAsync_ShouldReturnPaginatedPostsWithTotalPostsAndCommentsCount()
        {
            // Arrange
            const int ExpectedTotalPostCount = 23;
            const int ExpectedCommentCountPerPost = 7;
            const int PageNumber = 2;
            const int PageSize = 10;

            var testPosts = TestDataHelper.GetPostsWithComments
                (count: ExpectedTotalPostCount, commentCount: ExpectedCommentCountPerPost);
            var mockQueryable = testPosts.AsQueryable().BuildMock();

            _mockRepository.AsQueryable().Returns(mockQueryable);
            _mockRepository.GetTotalCountAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(ExpectedTotalPostCount));

            var service = new PostService(_mockRepository, _mockSnippetGenerator);

            // Act
            var result = await service.GetPostsWithTotalPostCountAsync(PageNumber, PageSize);

            // Assert
            Assert.Equal(PageSize, result.Posts.Count);
            Assert.Equal(ExpectedTotalPostCount, result.TotalPostCount);

            Assert.All(result.Posts, (postDto, index) =>
            {
                int expectedIndex = (PageNumber - 1) * PageSize + index;
                var expectedPost = testPosts[expectedIndex];

                TestDataHelper.AssertPostListDtoMapping(expectedPost, postDto, ExpectedCommentCountPerPost);
            });
        }

        [Fact]
        public async Task GetPostsWithTotalPostCountAsync_ShouldReturnLastPartialPage()
        {
            // Arrange
            const int PageNumber = 3;
            const int PageSize = 10;
            const int ExpectedTotalPostCount = 25;
            const int ExpectedCommentCountPerPost = 5;
            const int ExpectedCountOnPage = 5;

            var testPosts = TestDataHelper.GetPostsWithComments
                (count: ExpectedTotalPostCount, commentCount: ExpectedCommentCountPerPost, generateIds: true);
            var mockQueryable = testPosts.AsQueryable().BuildMock();

            _mockRepository.AsQueryable().Returns(mockQueryable);
            _mockRepository.GetTotalCountAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(ExpectedTotalPostCount));

            var service = new PostService(_mockRepository, _mockSnippetGenerator);

            // Act
            var result = await service.GetPostsWithTotalPostCountAsync(PageNumber, PageSize);

            // Assert            
            Assert.Equal(ExpectedCountOnPage, result.Posts.Count);
            Assert.Equal(ExpectedTotalPostCount, result.TotalPostCount);

            Assert.All(result.Posts, (postDto, index) =>
            {
                int expectedIndex = (PageNumber - 1) * PageSize + index;
                var expectedPost = testPosts[expectedIndex];

                TestDataHelper.AssertPostListDtoMapping(expectedPost, postDto, ExpectedCommentCountPerPost);
            });
        }

        [Fact]
        public async Task GetPostsWithTotalPostCountAsync_ShouldReturnEmptyList_WhenPageNumberIsTooLarge()
        {
            // Arrange
            const int PageNumber = 5;
            const int PageSize = 10;
            const int ExpectedTotalPostCount = 25;
            const int ExpectedCommentCountPerPost = 1;

            var testPosts = TestDataHelper.GetPostsWithComments
                (count: ExpectedTotalPostCount, commentCount: ExpectedCommentCountPerPost, generateIds: true);
            var mockQueryable = testPosts.AsQueryable().BuildMock();

            _mockRepository.AsQueryable().Returns(mockQueryable);
            _mockRepository.GetTotalCountAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(ExpectedTotalPostCount));

            var service = new PostService(_mockRepository, _mockSnippetGenerator);

            // Act
            var result = await service.GetPostsWithTotalPostCountAsync(PageNumber, PageSize);

            // Assert            
            Assert.Empty(result.Posts);
            Assert.Equal(ExpectedTotalPostCount, result.TotalPostCount);
        }

        [Fact]
        public async Task GetPostsWithTotalPostCountAsync_ShouldReturnFullPage_WhenTotalCountEqualsPageSize()
        {
            // Arrange
            const int PageNumber = 1;
            const int PageSize = 15;
            const int ExpectedTotalPostCount = 15;
            const int ExpectedCommentCountPerPost = 5;

            var testPosts = TestDataHelper.GetPostsWithComments
                (count: ExpectedTotalPostCount, commentCount: ExpectedCommentCountPerPost, generateIds: true);
            var mockQueryable = testPosts.AsQueryable().BuildMock();

            _mockRepository.AsQueryable().Returns(mockQueryable);
            _mockRepository.GetTotalCountAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(ExpectedTotalPostCount));

            var service = new PostService(_mockRepository, _mockSnippetGenerator);

            // Act
            var result = await service.GetPostsWithTotalPostCountAsync(PageNumber, PageSize);

            // Assert           
            Assert.Equal(ExpectedTotalPostCount, result.Posts.Count);
            Assert.Equal(ExpectedTotalPostCount, result.TotalPostCount);

            Assert.All(result.Posts, (postDto, index) =>
            {
                var expectedPost = testPosts[index];
                TestDataHelper.AssertPostListDtoMapping(expectedPost, postDto, ExpectedCommentCountPerPost);
            });
        }

        [Fact]
        public async Task SearchPostsWithTotalCountAsync_ShouldReturn_PagedSearchPosts_WithTotalCount()
        {
            // Arrange
            const string Query = "Chili";
            const string Expected_Title = "Ultimate Classic Chili Cheeseburger Recipe";
            const int PageNumber = 2;
            const int PageSize = 2;

            const string ExpectedSnippet = "Tips for brioche buns, sharp cheddar, and...";
            _mockSnippetGenerator
                .CreateSnippet(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
                .Returns(ExpectedSnippet);

            var allTestPosts = TestDataHelper.GetSearchedPost();

            var filteredPosts = allTestPosts
                .Where(p => p.Title.Contains(Query) || p.Description.Contains(Query) || p.Content.Contains(Query))
                .ToList();

            var mockQueryable = filteredPosts.AsQueryable().BuildMock();
            _mockRepository.GetFilteredQueryable(Arg.Any<Expression<Func<Post, bool>>>()).Returns(mockQueryable);

            var service = new PostService(_mockRepository, _mockSnippetGenerator);

            // Act
            var result = await service.SearchPostsWithTotalCountAsync(Query, PageNumber, PageSize);

            // Assert            
            Assert.Equal(3, result.SearchTotalPosts);

            Assert.Single(result.SearchPostList);

            Assert.Equal(1, result.SearchPostList.First().Id);
            Assert.Equal(Expected_Title, result.SearchPostList.First().Title);

            Assert.Equal(ExpectedSnippet, result.SearchPostList.First().SearchSnippet);

            var expectedPostModel = allTestPosts.First(p => p.Id == 1);
            var actualDto = result.SearchPostList.First();

            TestDataHelper.AssertSearchPostsWithTotalCountAsync(expectedPostModel, actualDto);

            _mockRepository.Received(1).GetFilteredQueryable(Arg.Any<Expression<Func<Post, bool>>>());
        }

        [Fact]
        public async Task SearchPostsWithTotalCountAsync_ShouldReturn_EmptyPostsList_WithZeroTotalCount()
        {
            // Arrange
            const string Query = "Not Found Query";
            const int ExpectedPostsCount = 0;
            const int PageNumber = 2;
            const int PageSize = 2;

            var allTestPosts = TestDataHelper.GetSearchedPost();

            var filteredPosts = allTestPosts
                .Where(p => p.Title.Contains(Query) || p.Description.Contains(Query) || p.Content.Contains(Query))
                .ToList();

            var mockQueryable = filteredPosts.AsQueryable().BuildMock();
            _mockRepository.GetFilteredQueryable(Arg.Any<Expression<Func<Post, bool>>>()).Returns(mockQueryable);

            var service = new PostService(_mockRepository, _mockSnippetGenerator);

            // Act
            var result = await service.SearchPostsWithTotalCountAsync(Query, PageNumber, PageSize);

            // Assert
            Assert.NotNull(result.SearchPostList);
            Assert.Empty(result.SearchPostList);
            Assert.Equal(ExpectedPostsCount, result.SearchTotalPosts);

            _mockRepository.Received(1).GetFilteredQueryable(Arg.Any<Expression<Func<Post, bool>>>());
        }

        [Fact]
        public async Task SearchPostsWithTotalCountAsync_ShouldReturn_EmptyList_WhenPageNumberIsOutOfRange()
        {
            // Arrange
            const string Query = "Chili";
            const int PageNumber = 10;
            const int PageSize = 2;

            var allTestPosts = TestDataHelper.GetSearchedPost();

            const string ExpectedSnippet = "Tips for brioche buns, sharp cheddar, and...";
            _mockSnippetGenerator
                .CreateSnippet(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
                .Returns(ExpectedSnippet);

            var filteredPosts = allTestPosts
                .Where(p => p.Title.Contains(Query) || p.Description.Contains(Query) || p.Content.Contains(Query))
                .ToList();

            var mockQueryable = filteredPosts.AsQueryable().BuildMock();
            _mockRepository.GetFilteredQueryable(Arg.Any<Expression<Func<Post, bool>>>()).Returns(mockQueryable);

            var service = new PostService(_mockRepository, _mockSnippetGenerator);

            // Act
            var result = await service.SearchPostsWithTotalCountAsync(Query, PageNumber, PageSize);

            // Assert
            Assert.Equal(3, result.SearchTotalPosts);
            Assert.Empty(result.SearchPostList);

            _mockRepository.Received(1).GetFilteredQueryable(Arg.Any<Expression<Func<Post, bool>>>());
        }

        [Fact]
        public async Task SearchPostsWithTotalCountAsync_ShouldReturn_FirstPage_WithDefaultParameters()
        {
            // Arrange
            const string Query = "Chili";

            var allTestPosts = TestDataHelper.GetSearchedPost();

            const string ExpectedSnippet = "Tips for brioche buns, sharp cheddar, and...";
            _mockSnippetGenerator
                .CreateSnippet(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
                .Returns(ExpectedSnippet);

            var filteredPosts = allTestPosts
                .Where(p => p.Title.Contains(Query) || p.Description.Contains(Query) || p.Content.Contains(Query))
                .ToList();

            var mockQueryable = filteredPosts.AsQueryable().BuildMock();
            _mockRepository.GetFilteredQueryable(Arg.Any<Expression<Func<Post, bool>>>()).Returns(mockQueryable);

            var service = new PostService(_mockRepository, _mockSnippetGenerator);

            var expectedSortedPosts = filteredPosts
                .OrderByDescending(p => p.CreateAt)
                .ToList();

            // Act
            var result = await service.SearchPostsWithTotalCountAsync(Query);

            // Assert
            Assert.Equal(3, result.SearchTotalPosts);
            Assert.Equal(3, result.SearchPostList.Count);
            Assert.Equal(2, result.SearchPostList.First().Id);

            Assert.All(result.SearchPostList, (searchPostDto, index) =>
            {
                var expectedPost = expectedSortedPosts[index];
                TestDataHelper.AssertSearchPostsWithTotalCountAsync(expectedPost, searchPostDto);

                Assert.Equal(ExpectedSnippet, searchPostDto.SearchSnippet);
            });

            _mockRepository.Received(1).GetFilteredQueryable(Arg.Any<Expression<Func<Post, bool>>>());

            _mockSnippetGenerator.Received(3).CreateSnippet(
                Arg.Any<string>(),
                Arg.Is(Query),
                Arg.Is(100)
            );
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnPost_WithComments()
        {
            // Arrange
            var postId = 2;
            var testPosts = TestDataHelper.GetPostsWithComments(count: 5, commentCount: 3, generateIds: true);
            var mockQueryable = testPosts.AsQueryable().BuildMock();

            _mockRepository.AsQueryable().Returns(mockQueryable);

            var service = new PostService(_mockRepository, _mockSnippetGenerator);

            // Act
            var post = await service.GetPostByIdAsync(postId, includeComments: true);

            // Assert
            Assert.NotNull(post);
            Assert.NotNull(post.Comments);
            Assert.NotEmpty(post.Comments);
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnPost_WithoutComments()
        {
            // Arrange
            var postId = 2;
            var testPosts = TestDataHelper.GetPostsWithComments(count: 5, generateComments: false, generateIds: true);
            var mockQueryable = testPosts.AsQueryable().BuildMock();

            _mockRepository.AsQueryable().Returns(mockQueryable);

            var service = new PostService(_mockRepository, _mockSnippetGenerator);

            // Act
            var post = await service.GetPostByIdAsync(postId, includeComments: false);

            // Assert
            Assert.NotNull(post);
            Assert.NotNull(post.Comments);
            Assert.Empty(post.Comments);
        }

        [Fact]
        public async Task AddPostAsync_ShouldAddPostSuccessfully_WhenPostIsValid()
        {
            // Arrange           
            var newPost = TestDataHelper.GetSinglePost();

            _mockRepository.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(false);

            _mockRepository.AddAsync(newPost, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(newPost));

            var postService = new PostService(_mockRepository, _mockSnippetGenerator);

            // Act
            var result = await postService.AddPostAsync(newPost);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newPost.Title, result.Title);
            Assert.Equal(newPost.Content, result.Content);

            await _mockRepository.Received(1)
                .AnyAsync(Arg.Is<Expression<Func<Post, bool>>>(p =>
                    p.Compile()(newPost)), Arg.Any<CancellationToken>());

            await _mockRepository.Received(1)
                .AddAsync(newPost, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostAsync_WhenPostIsUpdatedSuccessfully()
        {
            // Arrange            
            var originalPost = TestDataHelper.GetSinglePost();
            int postId = originalPost.Id;

            var inputPostData = new Post
            {
                Id = postId,
                Title = "New Title",
                Description = originalPost.Description,
                Content = originalPost.Content,
                Author = originalPost.Author,
                ImageUrl = originalPost.ImageUrl,
                MetaTitle = originalPost.MetaTitle,
                MetaDescription = originalPost.MetaDescription,
                Slug = "new-slug-value"
            };

            _mockRepository.GetByIdAsync(postId)
                .Returns(Task.FromResult(originalPost));

            _mockRepository.UpdateAsync(Arg.Any<Post>())
                .Returns(Task.CompletedTask);

            var service = new PostService(_mockRepository, _mockSnippetGenerator);

            // Act
            var resultPost = await service.UpdatePostAsync(postId, inputPostData);

            // Assert

            Assert.NotNull(resultPost);
            Assert.Equal(inputPostData.Title, resultPost.Title);
            Assert.Equal(inputPostData.Slug, resultPost.Slug);

            await _mockRepository.Received(1)
                .GetByIdAsync(originalPost.Id);

            await _mockRepository.Received(1)
                .UpdateAsync(Arg.Is<Post>(p =>
                    p.Title == "New Title" &&
            p.Slug == "new-slug-value" &&
            p.Content == originalPost.Content));
        }

        [Fact]
        public async Task DeletePostAsync_ShouldDeletePost_WhenSaveChangesSucceeds()
        {
            // Arrange            
            var post = TestDataHelper.GetSinglePost();

            var mockRepository = Substitute.For<IRepository<Post>>();

            mockRepository.GetByIdAsync(post.Id)
                .Returns(Task.FromResult(post));

            mockRepository.DeleteAsync(Arg.Any<Post>())
                .Returns(Task.CompletedTask);

            var service = new PostService(mockRepository, _mockSnippetGenerator);

            // Act
            await service.DeletePostAsync(post.Id);

            // Assert
            await mockRepository.Received(1)
                .GetByIdAsync(post.Id);

            await mockRepository.Received(1)
                .DeleteAsync(Arg.Is<Post>(p =>
                    p.Id == post.Id));
        }
    }
}