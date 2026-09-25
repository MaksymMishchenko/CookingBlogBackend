using PostApiService.Infrastructure.Common;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Tests.IntegrationTests.Services
{
    [Collection("SharedDatabase")]
    public class CategoryServiceIntegrationTests
    {
        private readonly ServiceTestFixture _fixture;

        public CategoryServiceIntegrationTests(ServiceTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ExistAsync_ShouldReturnTrue_IfCategoryExists()
        {
            // Arrange            
            await _fixture.ResetDatabaseAsync();

            var categories = TestDataHelper.GetCulinaryCategories();
            await _fixture.Services!.SeedCategoriesAsync(categories);

            var (service, dbContext, _) = _fixture.GetScopedService<IPublicCategoryService>();
            var categoryFromDb = await dbContext.Categories.FirstAsync();

            // Act              
            var result = await service.ExistsAsync(categoryFromDb.Id);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExistBySlugAsync_ShouldReturnTrue_IfCategoryExists()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();

            var categories = TestDataHelper.GetCulinaryCategories();
            await _fixture.Services!.SeedCategoriesAsync(categories);

            var (service, dbContext, _) = _fixture.GetScopedService<IPublicCategoryService>();
            var categoryFromDb = await dbContext.Categories.FirstAsync();

            // Act              
            var result = await service.ExistsBySlugAsync(categoryFromDb.Slug);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetAllCategoriesAsync_ShouldReturnListOfCategories()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();

            var categories = TestDataHelper.GetCulinaryCategories();
            await _fixture.Services!.SeedCategoriesAsync(categories);

            var (service, dbContext, _) = _fixture.GetScopedService<IPublicCategoryService>();

            // Act
            var result = await service.GetAllCategoriesAsync();

            var data = Assert.IsType<Result<List<CategoryDto>>>(result);

            //Assert
            Assert.NotNull(data.Value);
            Assert.True(data.IsSuccess);
            Assert.Equal(ResultStatus.Success, data.Status);
            Assert.Equal(categories.Count, data.Value!.Count);

            Assert.All(result.Value!, (actualDto, index) =>
            {
                var expectedCategory = categories[index];

                TestDataHelper.AssertCategoryAsync
                (expectedCategory, actualDto);
            });
        }
    }
}
