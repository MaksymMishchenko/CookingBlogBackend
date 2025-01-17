using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PostApiService.Controllers;
using PostApiService.Interfaces;
using PostApiService.Models;

namespace PostApiService.Tests.UnitTests.Controllers
{
    public class CommentControllerTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task OnAddCommentAsync_PostIdLessThanOrEqualZero_ReturnsBadRequest(int invalidPostId)
        {
            // Arrange            
            var commentServiceMock = new Mock<ICommentService>();
            var loggerServiceMock = new Mock<ILogger<CommentsController>>();

            var controller = new CommentsController(commentServiceMock.Object, loggerServiceMock.Object);

            var newComment = new Comment
            {
                Author = "Bob",
                Content = "Lorem Ipsum is simply dummy text of the printing and typesetting industry."
            };

            // Act
            var result = await controller.AddCommentAsync(invalidPostId, newComment);

            //Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<CommentResponse>(badRequestResult.Value);

            Assert.False(response.Success);
            Assert.Equal("Post ID must be greater than zero.", response.Message);
        }
    }
}
