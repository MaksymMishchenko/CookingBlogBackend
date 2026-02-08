using PostApiService.Controllers;
using PostApiService.Infrastructure.Common;
using PostApiService.Interfaces;
using PostApiService.Models.Common;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Tests.UnitTests.Controllers
{
    public class CategoryControllerTests
    {
        private readonly ICategoryService _mockCategoryService;
        private readonly CategoryController _categoryController;

        public CategoryControllerTests()
        {
            _mockCategoryService = Substitute.For<ICategoryService>();
            _categoryController = new CategoryController(_mockCategoryService);
        }

        [Fact]
        public async Task GetAllCategoriesAsync_ShouldReturn200Ok()
        {
            // Arrange
            var token = new CancellationToken(false);
            var categoriesFromService = new List<CategoryDto> { new CategoryDto(1, "Test", "test-slug") };
            var expectedResult = Result<List<CategoryDto>>.Success(categoriesFromService);

            _mockCategoryService.GetAllCategoriesAsync(token)
                .Returns(expectedResult);

            // Act
            var result = await _categoryController.GetAllCategoriesAsync(token);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResult = Assert.IsType<ApiResponse<List<CategoryDto>>>(okResult.Value);

            Assert.Equal(categoriesFromService, actualResult.Data);

            await _mockCategoryService.Received(1).GetAllCategoriesAsync(token);
        }
    }
}
