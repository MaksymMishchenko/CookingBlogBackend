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

        private PostService CreatePostService()
        {
            var context = _fixture.CreateContext();
            var repo = new Repository<Post>(context);
            return new PostService(repo);
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnListOfPosts_WithComments()
        {
            // Arrange
            var postService = CreatePostService();
            using var context = _fixture.CreateContext();

            // Act
            var listOfPosts = await postService.GetAllPostsAsync(1, 10, 1, 10, includeComments: true);

            // Assert            
            Assert.NotEmpty(listOfPosts);
            Assert.Single(listOfPosts);

            var returnedPost = listOfPosts[0];
            var post = await context.Posts
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == 1);
            Assert.NotNull(post);

            var comments = await context.Comments
                .Where(c => c.PostId == post.Id)
                .ToListAsync();
            Assert.NotNull(post);

            Assert.Equal(post.Id, returnedPost.Id);
            Assert.Equal(post.Title, returnedPost.Title);
            Assert.Equal(post.Description, returnedPost.Description);

            Assert.NotNull(returnedPost.Comments);
            Assert.NotEmpty(returnedPost.Comments);
            Assert.Equal(comments.Count, returnedPost.Comments.Count);

            Assert.All(returnedPost.Comments, comment =>
            {
                Assert.Equal(post.Id, comment.PostId);
            });

            Assert.Equal(comments[0].Content, returnedPost.Comments[0].Content);
        }

        [Fact]
        public async Task GetAllPostsAsync_ShouldReturnListOfPosts_WithoutComments()
        {
            // Arrange
            var postService = CreatePostService();
            using var context = _fixture.CreateContext();

            // Act
            var listOfPosts = await postService.GetAllPostsAsync(1, 10, 1, 10, includeComments: false);

            // Assert
            Assert.NotEmpty(listOfPosts);

            var post = await context.Posts
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == 1);

            Assert.NotNull(post);

            var postList = listOfPosts[0];

            Assert.Single(listOfPosts);
            Assert.Equal(post.Id, postList.Id);
            Assert.Equal(post.Title, postList.Title);
            Assert.Equal(post.Description, postList.Description);
            Assert.All(listOfPosts, post => Assert.NotNull(post.Comments));
            Assert.All(listOfPosts, post => Assert.Empty(post.Comments));
        }

        [Fact]
        public async Task GetPostByIdAsync_ShouldReturnSpecificPost()
        {
            // Arrange
            var postService = CreatePostService();
            using var context = _fixture.CreateContext();

            var postId = 1;

            // Act
            var existingPost = await postService.GetPostByIdAsync(postId, includeComments: false);

            // Assert            
            Assert.NotNull(existingPost);

            var post = await context.Posts
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == 1);
            Assert.NotNull(post);

            Assert.Equal(post.Id, existingPost.Id);
            Assert.Equal(post.Title, existingPost.Title);
            Assert.Equal(post.Description, existingPost.Description);
            Assert.NotNull(existingPost.Comments);
            Assert.Empty(existingPost.Comments);
        }

        [Fact]
        public async Task AddPostAsync_ShouldAddNewPostSuccessfully()
        {
            // Arrange
            var postService = CreatePostService();
            using var context = _fixture.CreateContext();

            var newPost = TestDataHelper.GetPostWithComments(generateComments: false);
            var initialCount = await context.Posts.CountAsync();

            // Act
            await postService.AddPostAsync(newPost);

            // Assert
            var addedPost = await context.Posts
                .FirstOrDefaultAsync(p => p.Title == newPost.Title && p.Author == newPost.Author);
            Assert.NotNull(addedPost);
            Assert.Equal(newPost.Title, addedPost.Title);
            Assert.Equal(newPost.Author, addedPost.Author);
            Assert.Equal(newPost.Content, addedPost.Content);
            Assert.NotEqual(DateTime.MinValue, addedPost.CreateAt);
            Assert.True(addedPost.CreateAt <= DateTime.UtcNow.ToLocalTime());

            var postCount = await context.Posts.CountAsync();
            Assert.Equal(initialCount + 1, postCount);
            Assert.NotNull(addedPost.Comments);
            Assert.Empty(addedPost.Comments);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldUpdatedExistingPostSuccessfully()
        {
            // Arrange
            var postService = CreatePostService();
            using var context = _fixture.CreateContext();

            var postId = 1;
            var existingPost = await context.Posts.FindAsync(postId);
            Assert.NotNull(existingPost);

            existingPost.Title = "Updated title";
            existingPost.Content = "Updated content";
            existingPost.MetaTitle = "Updated meta title";

            // Act
            await postService.UpdatePostAsync(existingPost);

            // Assert
            var updatedPost = await context.Posts.FindAsync(postId);
            Assert.NotNull(updatedPost);
            Assert.Equal(existingPost.Title, updatedPost.Title);
            Assert.Equal(existingPost.Content, updatedPost.Content);
            Assert.NotEqual(DateTime.MinValue, updatedPost.CreateAt);
            Assert.True(updatedPost.CreateAt <= DateTime.UtcNow.ToLocalTime());
        }

        [Fact]
        public async Task DeletePostAsync_ShouldRemovePostSuccessfully()
        {
            // Arrange
            var postService = CreatePostService();
            using var context = _fixture.CreateContext();

            var initialCount = await context.Posts.CountAsync();
            var postId = 1;

            // Act
            await postService.DeletePostAsync(postId);

            // Assert
            var removedPost = await context.Posts
                .AnyAsync(p => p.Id == postId);
            Assert.False(removedPost);
        }        
    }
}
