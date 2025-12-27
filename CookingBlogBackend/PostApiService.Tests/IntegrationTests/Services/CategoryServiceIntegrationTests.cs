using PostApiService.Models.Dto.Requests;
using PostApiService.Repositories;
using PostApiService.Services;
using System.Net;

namespace PostApiService.Tests.IntegrationTests.Services
{
    public class CategoryServiceIntegrationTests : IClassFixture<InMemoryDatabaseFixture>
    {
        private readonly InMemoryDatabaseFixture _fixture;

        public CategoryServiceIntegrationTests(InMemoryDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        private (CategoryService Service, List<Category> SeededCategories) CreateCategoryServiceAndSeedUniqueDb
            (out ApplicationDbContext context)
        {
            context = _fixture.CreateUniqueContext();

            var categoriesToSeed = TestDataHelper.GetCulinaryCategories();
            _fixture.SeedCategoryAsync(context, categoriesToSeed).Wait();

            var postRepo = new Repository<Post>(context);
            var repo = new Repository<Category>(context);
            var service = new CategoryService(repo, postRepo);

            return (service, categoriesToSeed);
        }

        [Fact]
        public async Task ExistAsync_ShouldReturnTrue_IfCategoryExists()
        {
            // Arrange
            ApplicationDbContext context;
            var (categoryService, seededCategories) = CreateCategoryServiceAndSeedUniqueDb(out context);

            using (context)
            {
                var categoryToFind = seededCategories.First();

                // Act              
                var result = await categoryService.ExistsAsync(categoryToFind.Id);

                // Assert
                Assert.NotNull(result);
                Assert.True(result.IsSuccess);
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                Assert.True(result.Value);
            }
        }

        [Fact]
        public async Task GetAllCategoriesAsync_ShouldReturnListOfCategories()
        {
            // Arrange
            ApplicationDbContext context;
            var (categoryService, seededCategories) = CreateCategoryServiceAndSeedUniqueDb(out context);

            using (context)
            {
                // Act
                var result = await categoryService.GetAllCategoriesAsync();

                //Assert
                Assert.NotNull(result);
                Assert.True(result.IsSuccess);
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                Assert.Equal(seededCategories.Count, result.Value!.Count);

                Assert.All(result.Value!, (actualDto, index) =>
                {
                    var expectedCategory = seededCategories[index];

                    TestDataHelper.AssertCategoryAsync
                    (expectedCategory, actualDto);
                });
            }
        }

        [Fact]
        public async Task GetCategoryById_ShouldReturnExpectedCategory()
        {
            // Arrange
            ApplicationDbContext context;
            var (categoryService, seededCategories) = CreateCategoryServiceAndSeedUniqueDb(out context);

            using (context)
            {
                var categoryToFind = seededCategories.Last();

                // Act
                var result = await categoryService.GetCategoryByIdAsync(categoryToFind.Id);

                //Assert
                Assert.NotNull(result);
                Assert.True(result.IsSuccess);
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                Assert.Equal(categoryToFind.Id, result.Value!.Id);
                Assert.Equal(categoryToFind.Name, result.Value!.Name);
            }
        }

        [Fact]
        public async Task AddCategoryAsync_ShouldSaveCategoryInDatabase_WhenDataIsValid()
        {
            // Arrange
            ApplicationDbContext context;
            var (categoryService, _) = CreateCategoryServiceAndSeedUniqueDb(out context);

            using (context)
            {
                var dto = new CreateCategoryDto { Name = "Soups" };

                // Act
                var result = await categoryService.AddCategoryAsync(dto);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                Assert.NotNull(result.Value);
                
                var categoryInDb = context.Categories.FirstOrDefault(c => c.Name == dto.Name);
                Assert.NotNull(categoryInDb);
                Assert.Equal(dto.Name, categoryInDb.Name);
            }
        }

        [Fact]
        public async Task UpdateCategoryAsync_ShouldUpdateExistingCategoryInDatabase()
        {
            // Arrange
            ApplicationDbContext context;
            var (categoryService, seededCategories) = CreateCategoryServiceAndSeedUniqueDb(out context);

            using (context)
            {
                var categoryToUpdate = seededCategories.First();
                var dto = new UpdateCategoryDto { Name = "Updated Culinary Name" };

                // Act
                var result = await categoryService.UpdateCategoryAsync(categoryToUpdate.Id, dto);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
               
                var updatedEntity = context.Categories.Find(categoryToUpdate.Id);
                Assert.NotNull(updatedEntity);
                Assert.Equal(dto.Name, updatedEntity.Name);
                Assert.Equal(categoryToUpdate.Id, updatedEntity.Id);
            }
        }

        [Fact]
        public async Task DeleteCategoryAsync_ShouldRemoveCategoryFromDatabase_WhenCategoryHasNoPosts()
        {
            // Arrange
            ApplicationDbContext context;
            var (categoryService, seededCategories) = CreateCategoryServiceAndSeedUniqueDb(out context);

            using (context)
            {                
                var categoryToDelete = seededCategories.Last();

                // Act
                var result = await categoryService.DeleteCategoryAsync(categoryToDelete.Id);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
                
                var categoryInDb = context.Categories.Find(categoryToDelete.Id);
                Assert.Null(categoryInDb);
            }
        }
    }
}
