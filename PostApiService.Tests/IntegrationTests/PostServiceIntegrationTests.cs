using Microsoft.EntityFrameworkCore;
using PostApiService.Models;
using PostApiService.Services;

namespace PostApiService.Tests.IntegrationTests
{
    public class PostServiceIntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly ApplicationDbContext _context;
        private readonly PostService _postService;

        public PostServiceIntegrationTests(IntegrationTestFixture fixture)
        {
            _context = fixture.Context;
            _postService = fixture.PostService;
        }

        [Fact]
        public async Task AddPostAsync_Should_Add_Post_And_SaveChanges()
        {
            // Arrange
            int postId = 1;

            var postToBeAdded = CreateTestPost(
                "Test Post",
                "Content of the test post.",
                "Test author",
                "http://example.com/image1.jpg",
                "Test post description",
                "test-post-three",
                "Test Post Meta Title 1",
                "Test Post Meta Description 1"
                );

            // Act
            await _postService.AddPostAsync(postToBeAdded);

            // Assert            
            var addedPost = await _context.Posts.FindAsync(postId);
            Assert.NotNull(addedPost);
            Assert.Equal(postToBeAdded.Title, addedPost.Title);
            Assert.Equal(postToBeAdded.Content, addedPost.Content);
            Assert.Equal(postToBeAdded.Author, addedPost.Author);
            Assert.Equal(postToBeAdded.ImageUrl, addedPost.ImageUrl);
            Assert.Equal(postToBeAdded.Description, addedPost.Description);
            Assert.Equal(postToBeAdded.Slug, addedPost.Slug);
            Assert.Equal(postToBeAdded.MetaTitle, addedPost.MetaTitle);
            Assert.Equal(postToBeAdded.MetaDescription, addedPost.MetaDescription);
        }

        [Fact]
        public async Task EditPostAsync_Should_Edit_Post_And_SaveChanges()
        {
            // Arrange
            int postId = 1;

            var postToBeUpdated = CreateTestPost(
                "Origin post title",
                "Origin post content",
                "Origin post author",
                "origine-image.jpg",
                "Origin description",
                "origin-slug-post",
                "Origin meta title",
                "Origin meta description"
                );

            await _context.Posts.AddAsync(postToBeUpdated);
            await _context.SaveChangesAsync();

            postToBeUpdated.Title = "Updated post title";
            postToBeUpdated.Content = "Updated post content";
            postToBeUpdated.MetaTitle = "Updated Test Post Meta Title";

            // Act
            await _postService.EditPostAsync(postToBeUpdated);

            // Assert
            var updatedPost = await _context.Posts.FindAsync(postId);
            Assert.NotNull(updatedPost);
            Assert.Equal(postToBeUpdated.PostId, updatedPost.PostId);
            Assert.Equal(postToBeUpdated.Title, updatedPost.Title);
            Assert.Equal(postToBeUpdated.Content, updatedPost.Content);
            Assert.Equal(postToBeUpdated.MetaTitle, updatedPost.MetaTitle);
        }

        [Fact]
        public async Task DeletePostAsync_Should_Remove_Post_If_Exists()
        {
            // Arrange
            int postId = 1;

            var postToBeDeleted = CreateTestPost(
                "Origin post title",
                "Origin post content",
                "Origin post author",
                "origine-image.jpg",
                "Origin description",
                "origin-slug-post",
                "Origin meta title",
                "Origin meta description"
                );

            await _context.Posts.AddAsync(postToBeDeleted);
            await _context.SaveChangesAsync();

            // Act
            await _postService.DeletePostAsync(postId);

            // Assert
            var removedPost = await _context.Posts.FindAsync(postId);
            Assert.Null(removedPost);
        }

        [Fact]
        public async Task GetAllPostsAsync_Should_Return_All_Posts()
        {
            // Arrange
            int totalCount = 2;

            var post1 = CreateTestPost(
                "Post title 1",
                "Post content 1",
                "Post author 1",
                "post-image1.jpg",
                "Post description 1",
                "post-slug-post 1",
                "Post meta title 1",
                "Post meta description 1"
                );

            var post2 = CreateTestPost(
                "Post title 2",
                "Post content 2",
                "Post author 2",
                "post-image2.jpg",
                "Post description 2",
                "post-slug-post 2",
                "Post meta title 2",
                "Post meta description 2"
                );

            await _context.Posts.AddRangeAsync(post1, post2);
            await _context.SaveChangesAsync();

            // Act
            var posts = await _postService.GetAllPostsAsync();

            // Assert
            Assert.NotNull(posts);
            Assert.Equal(totalCount, posts.Count);

            var firstPost = await _context.Posts.FirstOrDefaultAsync(p => p.Title == post1.Title);
            Assert.NotNull(firstPost);
            Assert.Equal(post1.Description, firstPost.Description);
            Assert.Equal(post1.Content, firstPost.Content);
            Assert.Equal(post1.Author, firstPost.Author);
            Assert.Equal(post1.MetaTitle, firstPost.MetaTitle);
            Assert.Equal(post1.MetaDescription, firstPost.MetaDescription);
            Assert.Equal(post1.Slug, firstPost.Slug);

            var secondPost = await _context.Posts.FirstOrDefaultAsync(p => p.Title == post2.Title);
            Assert.NotNull(secondPost);
            Assert.Equal(post2.Description, secondPost.Description);
            Assert.Equal(post2.Content, secondPost.Content);
            Assert.Equal(post2.Author, secondPost.Author);
            Assert.Equal(post2.MetaTitle, secondPost.MetaTitle);
            Assert.Equal(post2.MetaDescription, secondPost.MetaDescription);
            Assert.Equal(post2.Slug, secondPost.Slug);
        }

        [Fact]
        public async Task GetPostByIdAsync_Should_Return_Post_If_Found()
        {
            // Arrange
            int postId = 2;

            var post1 = CreateTestPost(
                 "Origin post title 1",
                 "Origin post content 1",
                 "Origin post author 1",
                 "origine-image1.jpg",
                 "Origin description 1",
                 "origin-slug-post 1",
                 "Origin meta title 1",
                 "Origin meta description 1"
                 );

            var post2 = CreateTestPost(
                "Origin post title 2",
                "Origin post content 2",
                "Origin post author 2",
                "origine-image2.jpg",
                "Origin description 2",
                "origin-slug-post 2",
                "Origin meta title 2",
                "Origin meta description 2"
                );

            await _context.Posts.AddRangeAsync(post1, post2);
            await _context.SaveChangesAsync();

            // Act
            var foundedPost = await _postService.GetPostByIdAsync(postId);

            // Assert
            Assert.NotNull(foundedPost);
            Assert.Equal(postId, foundedPost.PostId);
        }

        private Post CreateTestPost(
            string title,
            string content,
            string author,
            string imageUrl,
            string description,
            string slug,
            string metaTitle,
            string metaDescription
            )
        {
            return new Post
            {
                Title = title,
                Content = content,
                Author = author,
                ImageUrl = imageUrl,
                CreateAt = DateTime.UtcNow,
                Description = $"Description for {title}",
                Slug = slug,
                MetaTitle = $"Meta title for {title}",
                MetaDescription = $"Meta description for {title}"
            };
        }

        private async Task SeedPostAsync(Post post)
        {
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
        }
    }
}
