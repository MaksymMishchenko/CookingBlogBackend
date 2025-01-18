using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PostApiService.Models;

namespace PostApiService.Tests.IntegrationTests.Controllers
{
    public class CommentControllerIntegrationTests : IClassFixture<WebApplicationFactoryFixture>
    {
        private WebApplicationFactoryFixture _factory;

        public CommentControllerIntegrationTests(WebApplicationFactoryFixture factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task OnAddCommentAsync_ShouldAddCommentToDatabaseAndReturn200OkResult()
        {
            // Arrange
            var newComment = new Comment
            {
                Content = "Lorem ipsum dolor sit amet.",
                Author = "Jane",
                PostId = 1
            };

            var content = HttpHelper.GetJsonHttpContent(newComment);

            // Act
            var response = await _factory.Client.PostAsync(HttpHelper.Urls.AddComment, content);

            // Assert
            response.EnsureSuccessStatusCode();

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var addedComment = await dbContext.Comments
                    .FirstOrDefaultAsync(c => c.Content == newComment.Content &&
                                              c.Author == newComment.Author &&
                                              c.PostId == newComment.PostId);

                Assert.NotNull(addedComment);
                Assert.Equal(newComment.Content, addedComment.Content);
                Assert.Equal(newComment.Author, addedComment.Author);
                Assert.Equal(newComment.PostId, addedComment.PostId);
            }
        }

        [Fact]
        public async Task OnUpdateCommentAsync_ShouldUpdateCommentInDatabaseAndReturn200OkResult()
        {
            // Arrange
            var commentToBeEdited = new EditCommentModel
            {
                Content = "Updated comment content."
            };

            var content = HttpHelper.GetJsonHttpContent(commentToBeEdited);

            // Act
            var response = await _factory.Client.PutAsync(HttpHelper.Urls.UpdateComment, content);

            // Assert
            response.EnsureSuccessStatusCode();

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var editedComment = await dbContext.Comments
                    .FirstOrDefaultAsync(c => c.Content == commentToBeEdited.Content);

                Assert.NotNull(editedComment);
                Assert.Equal(commentToBeEdited.Content, editedComment.Content);
            }
        }
    }
}
