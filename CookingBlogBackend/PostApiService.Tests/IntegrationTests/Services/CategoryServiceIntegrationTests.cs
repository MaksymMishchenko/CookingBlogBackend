using PostApiService.Infrastructure.Common;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Requests;
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

            var (service, dbContext, _) = _fixture.GetScopedService<ICategoryService>();
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

            var (service, dbContext, _) = _fixture.GetScopedService<ICategoryService>();
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

            var (service, dbContext, _) = _fixture.GetScopedService<ICategoryService>();

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

        [Fact]
        public async Task GetCategoryById_ShouldReturnExpectedCategory()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();

            var categories = TestDataHelper.GetCulinaryCategories();
            await _fixture.Services!.SeedCategoriesAsync(categories);

            var (service, dbContext, _) = _fixture.GetScopedService<ICategoryService>();

            var categoryToFind = await dbContext.Categories
                .OrderByDescending(o => o.Id)
                .FirstAsync();

            // Act
            var result = await service.GetCategoryByIdAsync(categoryToFind.Id);

            //Assert
            var data = Assert.IsType<Result<CategoryDto>>(result);

            Assert.NotNull(data.Value);
            Assert.True(data.IsSuccess);
            Assert.Equal(ResultStatus.Success, data.Status);
            Assert.Equal(categoryToFind.Id, result.Value!.Id);
            Assert.Equal(categoryToFind.Name, result.Value!.Name);
        }

        [Fact]
        public async Task AddCategoryAsync_ShouldSaveCategoryInDatabase_WhenDataIsValid()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            _fixture.LoginAsAdmin();

            var dto = new CreateCategoryDto { Name = "Soups" };
            var (service, dbContext, _) = _fixture.GetScopedService<ICategoryService>();

            // Act
            var result = await service.AddCategoryAsync(dto);

            // Assert
            var data = Assert.IsType<Result<CategoryDto>>(result);

            Assert.True(data.IsSuccess);
            Assert.Equal(ResultStatus.Success, data.Status);
            Assert.NotNull(data.Value);
            Assert.Equal(dto.Name, data.Value.Name);
            Assert.True(data.Value.Id > 0);

            var (_, dbContextAssert, _) = _fixture.GetScopedService<ICategoryService>();
            var categoryInDb = await dbContextAssert.Categories.FirstOrDefaultAsync(c => c.Name == dto.Name);

            Assert.NotNull(categoryInDb);
            Assert.Equal(dto.Name, categoryInDb.Name);
        }

        [Fact]
        public async Task UpdateCategoryAsync_ShouldUpdateExistingCategoryInDatabase()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            _fixture.LoginAsAdmin();

            var categories = TestDataHelper.GetCulinaryCategories();
            await _fixture.Services!.SeedCategoriesAsync(categories);

            var (service, dbContext, _) = _fixture.GetScopedService<ICategoryService>();
            var categoryToUpdate = await dbContext.Categories.FirstAsync();

            string oldName = categoryToUpdate.Name;
            const string NewName = "Updated Culinary";
            var dto = new UpdateCategoryDto { Name = NewName };

            // Act
            var result = await service.UpdateCategoryAsync(categoryToUpdate.Id, dto);

            // Assert
            var data = Assert.IsType<Result<CategoryDto>>(result);

            Assert.True(data.IsSuccess);
            Assert.NotNull(data.Value);
            Assert.Equal(NewName, data.Value.Name);
            Assert.Equal(categoryToUpdate.Id, data.Value.Id);

            var (_, dbContextAssert, _) = _fixture.GetScopedService<ICategoryService>();
            var categoryInDb = await dbContextAssert.Categories.FindAsync(categoryToUpdate.Id);

            Assert.NotNull(categoryInDb);
            Assert.Equal(NewName, categoryInDb.Name);
            Assert.NotEqual(oldName, categoryInDb.Name);
        }

        [Fact]
        public async Task DeleteCategoryAsync_ShouldRemoveCategoryFromDatabase_WhenCategoryHasNoPosts()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            _fixture.LoginAsAdmin();

            var categories = TestDataHelper.GetCulinaryCategories();
            await _fixture.Services!.SeedCategoriesAsync(categories);

            var (service, dbContextArrange, _) = _fixture.GetScopedService<ICategoryService>();
            var categoryToDelete = await dbContextArrange.Categories.FirstAsync();
            int initialCount = await dbContextArrange.Categories.CountAsync();

            // Act
            var result = await service.DeleteCategoryAsync(categoryToDelete.Id);

            // Assert
            var data = Assert.IsType<Result>(result);

            Assert.True(data.IsSuccess);
            Assert.Equal(ResultStatus.Success, data.Status);

            var (_, dbContextAssert, _) = _fixture.GetScopedService<ICategoryService>();

            var categoryInDb = await dbContextAssert.Categories.FindAsync(categoryToDelete.Id);
            int finalCount = await dbContextAssert.Categories.CountAsync();

            Assert.Null(categoryInDb);
            Assert.Equal(initialCount - 1, finalCount);
        }
    }
}
