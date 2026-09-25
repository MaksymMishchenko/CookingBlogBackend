using PostApiService.Infrastructure.Common;
using PostApiService.Repositories;
using PostApiService.Services;
using System.Linq.Expressions;

namespace PostApiService.Tests.UnitTests.Services
{
    public class PublicCategoryServiceTests
    {
        private readonly ICategoryRepository _mockCategoryRepo;
        private readonly PublicCategoryService _categoryService;

        public PublicCategoryServiceTests()
        {
            _mockCategoryRepo = Substitute.For<ICategoryRepository>();
            _categoryService = new PublicCategoryService(_mockCategoryRepo);
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnSuccessWithTrue_IfCategoryExists()
        {
            // Arrange
            const int CategoryId = 1;
            _mockCategoryRepo.AnyAsync(Arg.Any<Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
                           .Returns(true);

            // Act
            var result = await _categoryService.ExistsAsync(CategoryId);

            // Assert
            Assert.True(result);

            await _mockCategoryRepo.Received(1).AnyAsync
                (Arg.Any<Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExistsBySlugAsync_ShouldReturnSuccessWithTrue_IfCategoryExists()
        {
            // Arrange
            const string Slug = "existent-slug";
            _mockCategoryRepo.AnyAsync(Arg.Any<Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
                           .Returns(true);

            // Act
            var result = await _categoryService.ExistsBySlugAsync(Slug);

            // Assert
            Assert.True(result);

            await _mockCategoryRepo.Received(1).AnyAsync
                (Arg.Any<Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExistsBySlugAsync_ShouldReturnFalse_IfCategoryDoesNotExist()
        {
            // Arrange
            const string Slug = "non-existent-slug";
            _mockCategoryRepo.AnyAsync(Arg.Any<Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
                           .Returns(false);

            // Act
            var result = await _categoryService.ExistsBySlugAsync(Slug);

            // Assert
            Assert.False(result);

            await _mockCategoryRepo.Received(1).AnyAsync
                (Arg.Any<Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetAllCategoriesAsync_ShouldReturnListWithAllCategories()
        {
            // Arrange
            var categories = TestDataHelper.GetCulinaryCategories();
            _mockCategoryRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(categories);

            // Act
            var result = await _categoryService.GetAllCategoriesAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.NotNull(result.Value);
            Assert.Equal(categories.Count, result.Value.Count);

            Assert.Equal(categories[0].Name, result.Value[0].Name);
            Assert.Equal(categories[0].Id, result.Value[0].Id);

            await _mockCategoryRepo.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetAllCategoriesAsync_ShouldReturnEmptyList_WhenNoCategoriesExist()
        {
            // Arrange
            _mockCategoryRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Category>());

            // Act
            var result = await _categoryService.GetAllCategoriesAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.Empty(result.Value!);

            await _mockCategoryRepo.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
        }
    }
}
