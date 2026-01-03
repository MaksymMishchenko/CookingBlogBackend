using Microsoft.Extensions.DependencyInjection;
using PostApiService.Models.Common;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

namespace PostApiService.Tests.IntegrationTests.Controllers
{
    public class CategoryControllerTests : IClassFixture<CategoryFixture>
    {
        private readonly CategoryFixture _fixture;
        private readonly HttpClient _client;
        private readonly IServiceProvider _services;

        public CategoryControllerTests(CategoryFixture fixture)
        {
            _fixture = fixture;
            _client = fixture.Client!;
            _services = fixture.Services!;
        }

        [Fact]
        public async Task GetAllCategoriesAsync_ShouldReturnAllCategories_WithStatusCode200Ok()
        {
            // Arrange
            const int ExpectedTotalCategories = 6;
            var categories = TestDataHelper.GetCulinaryCategories();

            await SeedCategoriesAsync(categories);

            var url = Categories.Base;

            // Act            
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<CategoryDto>>>();

            response.EnsureSuccessStatusCode();

            // Assert
            Assert.NotNull(content);
            Assert.True(content.Success);

            Assert.NotNull(content.Data);
            Assert.Equal(ExpectedTotalCategories, content.Data.Count);

            var firstCategory = content.Data.OrderBy(c => c.Id).First();
            Assert.Equal(categories[0].Name, firstCategory.Name);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_ShouldReturnBadRequest_WhenCategoryIdIsInvalid()
        {
            // Arrange
            SetupMockUser();

            const int InvalidCategorytId = 0;
            var url = string.Format(Categories.GetById, InvalidCategorytId);

            // Act
            var response = await _client.GetAsync(url);

            // Assert           
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<CategoryDto>>>();

            Assert.NotNull(result);
            Assert.False(result.Success);

            Assert.Equal(Global.Validation.ValidationFailed, result.Message);

            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.ContainsKey("id"));
            Assert.Equal(Global.Validation.InvalidId, result.Errors["id"][0]);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_ShouldReturn200ОК_WithExpectedCategory()
        {
            // Arrange
            SetupMockUser();

            const int ExpectedCategoryId = 5;
            var categories = TestDataHelper.GetCulinaryCategories();
            await SeedCategoriesAsync(categories);

            var expectedCategory = categories.First(c => c.Id == ExpectedCategoryId);

            var url = string.Format(Categories.GetById, ExpectedCategoryId);

            // Act
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
            response.EnsureSuccessStatusCode();

            // Assert
            Assert.NotNull(content);
            Assert.True(content!.Success);
            Assert.NotNull(content.Data);

            Assert.Equal(expectedCategory.Id, content.Data.Id);
            Assert.Equal(expectedCategory!.Name, content.Data.Name);
        }

        [Fact]
        public async Task AddСategoryAsync_ShouldReturnBadRequest_WhenModelIsInvalid()
        {
            // Arrange
            SetupMockUser();

            var invalidCategory = new CreateCategoryDto { Name = null! };
            var content = HttpHelper.GetJsonHttpContent(invalidCategory);

            var url = Categories.Base;

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

        [Fact]
        public async Task AddCategoryAsync_ShouldReturnLocationHeader_AndDataShouldBeAccessible()
        {
            // Arrange
            SetupMockUser();

            var newCategory = new CreateCategoryDto { Name = "Seafood" };
            var content = HttpHelper.GetJsonHttpContent(newCategory);

            var url = Categories.Base;
            
            var response = await _client.PostAsync(url, content);
            
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            var locationHeader = response.Headers.Location?.ToString();
            Assert.NotNull(locationHeader);            
            
            var getResponse = await _client.GetAsync(locationHeader);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        }

        [Fact]
        public async Task AddCategoryAsync_ShouldAddCategory_Return201CreatedAtAction()
        {
            // Arrange
            SetupMockUser();

            var newCategory = new CreateCategoryDto { Name = "Street Food" };
            var content = HttpHelper.GetJsonHttpContent(newCategory);

            var url = Categories.Base;

            // Act
            var response = await _client.PostAsync(url, content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
            Assert.NotNull(result);

            var locationHeader = response.Headers.Location?.ToString();
            Assert.NotNull(locationHeader);

            Assert.True(result.Success);
            Assert.Equal(CategoryM.Success.CategoryAddedSuccessfully, result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(newCategory.Name, result.Data.Name);
        }

        [Fact]
        public async Task UpdateCategoryAsync_ShouldReturnBadRequest_WhenModelIsInvalid()
        {
            // Arrange
            SetupMockUser();

            const int ValidId = 3;
            var invalidCategory = new UpdateCategoryDto { Name = null! };
            var content = HttpHelper.GetJsonHttpContent(invalidCategory);

            var url = string.Format(Categories.GetById, ValidId);

            // Act
            var response = await _client.PutAsync(url, content);

            // Assert           
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal(Global.Validation.ValidationFailed, result.Message);

            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.ContainsKey("Name"));
        }

        [Fact]
        public async Task UpdateCategoryAsync_ShouldReturnBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            SetupMockUser();

            const int InvalidId = 0;
            var invalidCategory = new UpdateCategoryDto { Name = "Soap" };
            var content = HttpHelper.GetJsonHttpContent(invalidCategory);

            var url = string.Format(Categories.GetById, InvalidId);

            // Act
            var response = await _client.PutAsync(url, content);

            // Assert           
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

            Assert.NotNull(result);
            Assert.False(result.Success);

            Assert.Equal(Global.Validation.ValidationFailed, result.Message);

            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.ContainsKey("id"));
            Assert.Equal(Global.Validation.InvalidId, result.Errors["id"][0]);
        }

        [Fact]
        public async Task UpdateCategoryAsync_ShouldUpdateCategory_Return200Ok()
        {
            // Arrange
            SetupMockUser();

            const int ValidId = 2;
            var categories = TestDataHelper.GetCulinaryCategories();
            await SeedCategoriesAsync(categories);

            var updateDto = new UpdateCategoryDto { Name = "Soap" };
            var content = HttpHelper.GetJsonHttpContent(updateDto);

            var url = string.Format(Categories.GetById, ValidId);

            // Act
            var response = await _client.PutAsync(url, content);

            // Assert           
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(updateDto.Name, result.Data.Name);
            Assert.Equal(CategoryM.Success.CategoryUpdatedSuccessfully, result.Message);

            var category = await GetCategoryByIdAsync(ValidId);


            Assert.NotNull(category);
            Assert.Equal(updateDto.Name, category.Name);
        }

        [Fact]
        public async Task DeleteCategoryAsync_ShouldReturnBadRequest_WhenCategoryIdIsInvalid()
        {
            // Arrange
            SetupMockUser();

            var invalidCategoryId = -1;
            var url = string.Format(Categories.GetById, invalidCategoryId);

            // Act
            var response = await _client.DeleteAsync(url);

            // Assert            
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

            Assert.NotNull(result);
            Assert.False(result.Success);

            Assert.Equal(Global.Validation.ValidationFailed, result.Message);

            Assert.NotNull(result.Errors);
            Assert.Contains("id", result.Errors.Keys);
            Assert.Equal(Global.Validation.InvalidId, result.Errors["id"][0]);
        }        

        [Fact]
        public async Task DeleteCategoryAsync_ShouldReturn200Ok_IfCategoryIsDeletedSuccessfully()
        {
            // Arrange
            SetupMockUser();

            const int ValidCategoryId = 4;
            var categories = TestDataHelper.GetCulinaryCategories();
            await SeedCategoriesAsync(categories);

            var url = string.Format(Categories.GetById, ValidCategoryId);

            // Act
            var request = await _client.DeleteAsync(url);
            request.EnsureSuccessStatusCode();

            var result = await request.Content.ReadFromJsonAsync<ApiResponse<bool>>();

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.Success);
            Assert.Equal(string.Format
                (CategoryM.Success.CategoryDeletedSuccessfully), result.Message);

            var category = await GetCategoryByIdAsync(ValidCategoryId);
            Assert.Null(category);
        }

        private async Task SeedCategoriesAsync(IEnumerable<Category> categories)
        {
            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                await dbContext.Database.EnsureDeletedAsync();
                if (await dbContext.Database.EnsureCreatedAsync())
                {
                    await dbContext.Categories.AddRangeAsync(categories);
                    await dbContext.SaveChangesAsync();
                }
            }
        }       

        private async Task<Category?> GetCategoryByIdAsync(int id)
        {
            using var scope = _services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            return await dbContext.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            
        }        

        private void SetupMockUser()
        {
            var adminClaims = new[]
                {
                new Claim(ClaimTypes.NameIdentifier, "admin-id"),
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var adminIdentity = new ClaimsIdentity(adminClaims, "DynamicScheme");
            var adminPrincipal = new ClaimsPrincipal(adminIdentity);

            _fixture.SetCurrentUser(adminPrincipal);
        }
    }
}
