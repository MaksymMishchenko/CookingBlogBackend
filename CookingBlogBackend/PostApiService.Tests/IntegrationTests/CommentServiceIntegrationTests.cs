//using Castle.Core.Logging;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using PostApiService.Models;
//using PostApiService.Services;

//namespace PostApiService.Tests.IntegrationTests
//{
//    public class CommentServiceIntegrationTests : IClassFixture<IntegrationTestFixture>
//    {
//        private readonly IntegrationTestFixture _fixture;
//        public CommentServiceIntegrationTests(IntegrationTestFixture fixture)
//        {
//            _fixture = fixture;
//        }

//        [Fact]
//        public async Task AddCommentAsync_Should_Add_Comment_To_Specific_Post()
//        {
//            // Arrange
//            using var context = _fixture.CreateContext();
//            var postService = new PostService(context);
//            //var logger = new LoggerFactory().CreateLogger<CommentService>();
//            var commentService = new CommentService(context, null);

//            int postId = 1;
//            var post = CreateTestPost(
//                "Origin Post",
//                "Origin Content",
//                "Origin Author",
//                "Origin Description",
//                "origin-image.jpg",
//                "origin-post",
//                "Origin Post meta title",
//                "Origin Post meta description"
//                );

//            await postService.AddPostAsync(post);

//            var comment = new Comment
//            {
//                Content = "Test comment from Bob",
//                Author = "Bob",
//                CreatedAt = DateTime.Now
//            };

//            // Act
//            await commentService.AddCommentAsync(postId, comment);

//            // Assert
//            var addedComment = await context.Comments
//                .FirstOrDefaultAsync(c => c.Content == comment.Content
//                && c.Author == comment.Author);
//            Assert.NotNull(addedComment);
//            Assert.True(addedComment.PostId == postId);
//        }

//        [Fact]
//        public async Task EditCommentAsync_Should_Edit_Comment_If_Exist()
//        {
//            // Arrange
//            using var context = _fixture.CreateContext();
//            var postService = new PostService(context);
//            var commentService = new CommentService(context);

//            int postId = 1;
//            var post = CreateTestPost(
//                "Origin Post",
//                "Origin Content",
//                "Origin Author",
//                "Origin Description",
//                "origin-image.jpg",
//                "origin-post",
//                "Origin Post meta title",
//                "Origin Post meta description"
//                );

//            await postService.AddPostAsync(post);

//            var comment = new Comment
//            {
//                Content = "Origin comment content",
//                Author = "Michael",
//                CreatedAt = DateTime.Now
//            };

//            await commentService.AddCommentAsync(postId, comment);

//            comment.Content = "Updated comment content";

//            // Act
//            await commentService.EditCommentAsync(comment);

//            // Assert
//            var editedComment = await context.Comments
//                .FirstOrDefaultAsync(c => c.Content == comment.Content
//            && c.Author == comment.Author
//            );
//            Assert.NotNull(editedComment);
//            Assert.Equal("Updated comment content", editedComment.Content);
//            Assert.Equal(comment.Author, editedComment.Author);
//        }

//        [Fact]
//        public async Task DeleteCommentAsync_Should_Remove_Comment_If_Exists()
//        {
//            // Arrange
//            using var context = _fixture.CreateContext();
//            var postService = new PostService(context);
//            var commentService = new CommentService(context);

//            var postId = 1;
//            var post = CreateTestPost(
//                "Origin Post",
//                "Origin Content",
//                "Origin Author",
//                "Origin Description",
//                "origin-image.jpg",
//                "origin-post",
//                "Origin Post meta title",
//                "Origin Post meta description"
//                );

//            await postService.AddPostAsync(post);

//            var comment = new Comment
//            {
//                Content = "Comment to be deleted",
//                Author = "William",
//                CreatedAt = DateTime.Now
//            };

//            await commentService.AddCommentAsync(postId, comment);

//            // Act
//            await commentService.DeleteCommentAsync(comment.CommentId);

//            // Assert
//            var removedComment = await context.Comments.FindAsync(comment.CommentId);
//            Assert.Null(removedComment);
//        }

//        private Post CreateTestPost(string title,
//            string content,
//            string author,
//            string description,
//            string imageUrl,
//            string slug,
//            string metaTitle,
//            string metaDescription)
//        {
//            return new Post
//            {
//                Title = title,
//                Content = content,
//                Author = author,
//                CreateAt = DateTime.Now,
//                Description = description,
//                ImageUrl = imageUrl,
//                Slug = slug,
//                MetaTitle = metaTitle,
//                MetaDescription = metaDescription
//            };
//        }
//    }
//}

