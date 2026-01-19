using PostApiService.Models.Common;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
using System.Net;
using System.Net.Http.Json;

namespace PostApiService.Tests.IntegrationTests.Controllers
{
    [Collection("SharedDatabase")]
    public class CommentControllerTests
    {
        private readonly BaseTestFixture _fixture;
        private readonly HttpClient _client;
        private readonly IServiceProvider _services;

        public CommentControllerTests(BaseTestFixture fixture)
        {
            _fixture = fixture;
            _client = fixture.Client!;
            _services = fixture.Services!;
            _fixture.LoginAsContributor();
        }

        [Fact]
        public async Task OnAddComment_ShouldReturnBadRequest_WhenPostIdIsInvalid()
        {
            // Arrange                        
            var invalidPostId = 0;
            var comment = new CommentCreateDto { Content = "Valid content" };
            var content = HttpHelper.GetJsonHttpContent(comment);

            var url = string.Format(Comments.GetById, invalidPostId);

            // Act
            var response = await _client.PostAsync(url, content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal(Global.Validation.ValidationFailed, result.Message);
        }

        [Fact]
        public async Task OnAddComment_ShouldReturnBadRequest_WhenModelIsInvalid()
        {
            // Arrange                        
            var postId = 1;
            var invalidComment = new CommentCreateDto { Content = "" };
            var content = HttpHelper.GetJsonHttpContent(invalidComment);

            var url = string.Format(Comments.GetById, postId);

            // Act
            var response = await _client.PostAsync(url, content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal(Global.Validation.ValidationFailed, result.Message);
            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.Any());
        }

        [Theory]
        [InlineData(TestUserData.AdminKey)]
        [InlineData(TestUserData.ContributorKey)]
        public async Task OnAddCommentAsync_ShouldAddCommentToDatabaseAndReturn200OkResult(string userRole)
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _services.SeedDefaultUsersAsync();

            if (userRole == TestUserData.AdminKey)
                _fixture.LoginAsAdmin();
            else
                _fixture.LoginAsContributor();

            const string ExpectedCommentOutput = "Test comment content";
            const string HackCode = "Test comment content<script>Hack code</script>";

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(categories);

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var postId = posts.First().Id;
            var createDto = TestDataHelper.CreateCommentRequest(HackCode);
            var content = HttpHelper.GetJsonHttpContent(createDto);

            var url = string.Format(Comments.GetById, postId);

            // Act
            var response = await _client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            // Assert            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<CommentCreatedDto>>();

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(CommentM.Success.CommentAddedSuccessfully, result.Message);
            Assert.NotNull(result.Data);

            var data = result.Data!;
            Assert.Equal(ExpectedCommentOutput, data.Content);
            Assert.True(data.Id > 0);
            Assert.NotEqual(default, data.CreatedAt);
            Assert.NotNull(data.Author);
        }

        [Fact]
        public async Task OnUpdateComment_ShouldReturnBadRequest_WhenCommentIdIsInvalid()
        {
            // Arrange           
            var invalidCommentId = 0;
            var editModel = new CommentUpdateDto { Content = "Some valid content" };
            var content = HttpHelper.GetJsonHttpContent(editModel);
            var url = string.Format(Comments.GetById, invalidCommentId);

            // Act
            var response = await _client.PutAsync(url, content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal(Global.Validation.ValidationFailed, result.Message);
        }

        [Fact]
        public async Task OnUpdateComment_ShouldReturnBadRequest_WhenModelIsInvalid()
        {
            // Arrange            
            var commentId = 2;
            var invalidModel = new CommentUpdateDto { Content = "" };
            var content = HttpHelper.GetJsonHttpContent(invalidModel);
            var url = string.Format(Comments.GetById, commentId);

            // Act
            var response = await _client.PutAsync(url, content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal(Global.Validation.ValidationFailed, result.Message);
            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.Any());
        }

        [Fact]
        public async Task OnUpdateCommentAsync_ShouldUpdateCommentInDatabaseAndReturn200OkResult()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _services.SeedDefaultUsersAsync();

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(categories);

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var title = posts.First().Title;

            var expectedComment = posts.SelectMany(c => c.Comments).First();
            var realId = expectedComment.Id;

            var editedComment = new CommentUpdateDto { Content = "Updated comment content." };

            var content = HttpHelper.GetJsonHttpContent(editedComment);
            var url = string.Format(Comments.GetById, realId);

            // Act
            var response = await _client.PutAsync(url, content);

            // Assert
            response.EnsureSuccessStatusCode();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<CommentUpdatedDto>>();

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(CommentM.Success.CommentUpdatedSuccessfully, result.Message);
            Assert.NotNull(result.Data);

            var data = result.Data!;
            Assert.Equal(editedComment.Content, data.Content);
            Assert.True(data.Id > 0);
            Assert.NotEqual(default, data.CreatedAt);
            Assert.NotNull(data.Author);
        }

        [Fact]
        public async Task OnDeleteComment_ShouldReturnBadRequest_WhenCommentIdIsInvalid()
        {
            // Arrange                       
            var invalidCommentId = 0;
            var url = string.Format(Comments.GetById, invalidCommentId);

            // Act
            var response = await _client.DeleteAsync(url);

            // Assert            
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal(Global.Validation.ValidationFailed, result.Message);
        }

        [Fact]
        public async Task OnDeleteCommentAsync_ShouldRemoveCommentInDatabaseAndReturn200OkResult()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _services.SeedDefaultUsersAsync();

            var categories = TestDataHelper.GetCulinaryCategories();
            var posts = TestDataHelper.GetPostsWithComments(categories);

            await _fixture.Services!.SeedBlogDataAsync(posts, categories);

            var comment = posts.SelectMany(p => p.Comments).First();
            var realId = comment.Id;

            var url = string.Format(Comments.GetById, realId);

            // Act
            var response = await _client.DeleteAsync(url);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(CommentM.Success.CommentDeletedSuccessfully, result.Message);
        }
    }
}
