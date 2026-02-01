using PostApiService.Helper;
using PostApiService.Infrastructure.Common;
using PostApiService.Models.Dto.Requests;
using PostApiService.Repositories;
using PostApiService.Services;
using System.Linq.Expressions;

namespace PostApiService.Tests.UnitTests.Services
{
    public class CategoryServiceTests
    {
        private readonly IRepository<Category> _mockCategoryRepo;
        private readonly IPostRepository _mockPostRepo;
        private readonly CategoryService _categoryService;

        public CategoryServiceTests()
        {
            _mockPostRepo = Substitute.For<IPostRepository>();
            _mockCategoryRepo = Substitute.For<IRepository<Category>>();
            _categoryService = new CategoryService(_mockCategoryRepo, _mockPostRepo);
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

        [Fact]
        public async Task GetCategoryByIdAsync_ShouldReturnNotFound_IfNotFound()
        {
            // Arrange
            const int categoryId = 999;
            _mockCategoryRepo.GetByIdAsync(categoryId, Arg.Any<CancellationToken>()).Returns((Category)null!);

            // Act
            var result = await _categoryService.GetCategoryByIdAsync(categoryId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal(CategoryM.Errors.CategoryNotFound, result.Message);

            await _mockCategoryRepo.Received(1).GetByIdAsync(categoryId, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetCategoryByIdAsync_ShouldReturnSuccess_WhenCategoryExists()
        {
            // Arrange
            const int categoryId = 1;
            var category = new Category { Id = categoryId, Name = "Test Category" };

            _mockCategoryRepo.GetByIdAsync(categoryId, Arg.Any<CancellationToken>()).Returns(category);

            // Act
            var result = await _categoryService.GetCategoryByIdAsync(categoryId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.NotNull(result.Value);
            Assert.Equal(category.Name, result.Value.Name);
            Assert.Equal(category.Id, result.Value.Id);

            await _mockCategoryRepo.Received(1).GetByIdAsync(categoryId, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetCategoryByIdAsync_ShouldReturnNotFound_WhenCategoryDoesNotExist()
        {
            // Arrange
            const int categoryId = 99;
            _mockCategoryRepo.GetByIdAsync(categoryId, Arg.Any<CancellationToken>()).Returns((Category)null!);

            var expectedMessage = CategoryM.Errors.CategoryNotFound;

            // Act
            var result = await _categoryService.GetCategoryByIdAsync(categoryId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal(expectedMessage, result.Message);
            Assert.Null(result.Value);

            await _mockCategoryRepo.Received(1).GetByIdAsync(categoryId, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddCategoryAsync_ShouldStripHtmlFromName_BeforeSaving()
        {
            // Arrange
            var dto = new CreateCategoryDto { Name = "<b>Healthy</b> Food" };
            var expectedName = "Healthy Food";
            var expectedSlug = "healthy-food";

            _mockCategoryRepo.AnyAsync(Arg.Any<Expression<Func<Category, bool>>>(),
                Arg.Any<CancellationToken>()).Returns(false);

            // Act
            var result = await _categoryService.AddCategoryAsync(dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedName, result.Value!.Name);
            Assert.Equal(expectedSlug, result.Value.Slug);

            await _mockCategoryRepo.Received(1).AddAsync(Arg.Is<Category>(c => c.Name == expectedName),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddCategoryAsync_ShouldReturnConflict_WhenCategoryNameAlreadyExists()
        {
            // Arrange
            var dto = new CreateCategoryDto { Name = "Dessert" };
            var expectedSlug = StringHelper.GenerateSlug(dto.Name);
            _mockCategoryRepo.AnyAsync(Arg.Any<Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
                             .Returns(true);

            var expectedMessage = string.Format(CategoryM.Errors.CategoryOrSlugExists, dto.Name, expectedSlug);

            // Act
            var result = await _categoryService.AddCategoryAsync(dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Equal(expectedMessage, result.Message);

            await _mockCategoryRepo.Received(1).AnyAsync
                (Arg.Any<Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>());
            await _mockCategoryRepo.DidNotReceive().AddAsync(Arg.Any<Category>());
            await _mockCategoryRepo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddCategoryAsync_ShouldGenerateSlugFromName_WhenSlugInDtoIsEmpty()
        {
            // Arrange
            var dto = new CreateCategoryDto { Name = "Desserts", Slug = "" };
            var expectedSlug = "desserts";

            _mockCategoryRepo.AnyAsync(Arg.Any<Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
                             .Returns(false);

            // Act
            var result = await _categoryService.AddCategoryAsync(dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedSlug, result.Value!.Slug);

            await _mockCategoryRepo.Received(1).AddAsync(Arg.Is<Category>(c => c.Slug == expectedSlug), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddCategoryAsync_ShouldReturnSuccess_WhenCategoryIsCreated()
        {
            // Arrange
            var token = new CancellationToken(false);
            var dto = new CreateCategoryDto { Name = "Dessert" };
            _mockCategoryRepo.AnyAsync(Arg.Any<Expression<Func<Category, bool>>>(), token)
                             .Returns(false);

            _mockCategoryRepo.AddAsync(Arg.Any<Category>(), token)
                             .Returns(Task.CompletedTask);

            // Act
            var result = await _categoryService.AddCategoryAsync(dto, token);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.NotNull(result.Value);
            Assert.Equal(dto.Name, result.Value.Name);

            await _mockCategoryRepo.Received(1).AnyAsync
                (Arg.Any<Expression<Func<Category, bool>>>(), token);
            await _mockCategoryRepo.Received(1).AddAsync(Arg.Is<Category>(c => c.Name == dto.Name), token);
            await _mockCategoryRepo.Received(1).SaveChangesAsync(token);
        }

        [Fact]
        public async Task UpdateCategoryAsync_ShouldReturnNotFound_WhenCategoryDoesNotExist()
        {
            // Arrange
            const int categoryId = 1;
            var dto = new UpdateCategoryDto { Name = "Bakery" };
            _mockCategoryRepo.GetByIdAsync(categoryId, Arg.Any<CancellationToken>()).Returns((Category)null!);

            // Act
            var result = await _categoryService.UpdateCategoryAsync(categoryId, dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal(CategoryM.Errors.CategoryNotFound, result.Message);

            await _mockCategoryRepo.Received(1).GetByIdAsync
                (categoryId, Arg.Any<CancellationToken>());
            await _mockCategoryRepo.DidNotReceive().AnyAsync
                (Arg.Any<Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>());
            await _mockCategoryRepo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateCategoryAsync_ShouldReturnConflict_WhenNewNameAlreadyExistsForOtherCategory()
        {
            // Arrange
            const int categoryId = 1;
            var dto = new UpdateCategoryDto { Name = "Dessert" };
            var existingCategory = new Category { Id = categoryId, Name = "Beverages" };

            var finalSlug = StringHelper.GenerateSlug(dto.Name);

            _mockCategoryRepo.GetByIdAsync(categoryId, Arg.Any<CancellationToken>())
                .Returns(existingCategory);

            _mockCategoryRepo.AnyAsync(Arg.Any<Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
                             .Returns(true);

            var expectedMessage = string.Format(CategoryM.Errors.CategoryOrSlugExists, dto.Name, finalSlug);

            // Act
            var result = await _categoryService.UpdateCategoryAsync(categoryId, dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Conflict, result.Status);

            await _mockCategoryRepo.Received(1).GetByIdAsync
                (categoryId, Arg.Any<CancellationToken>());

            await _mockCategoryRepo.Received(1).AnyAsync
                (Arg.Any<Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>());

            await _mockCategoryRepo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateCategoryAsync_ShouldReturnConflict_WhenGeneratedSlugAlreadyExists()
        {
            // Arrange
            const int categoryId = 1;
            var dto = new UpdateCategoryDto { Name = "New Unique Name" };
            var existingCategory = new Category { Id = categoryId, Name = "Old Name", Slug = "old-slug" };

            _mockCategoryRepo.GetByIdAsync(categoryId, Arg.Any<CancellationToken>())
                .Returns(existingCategory);

            _mockCategoryRepo.AnyAsync(Arg.Any<Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            // Act
            var result = await _categoryService.UpdateCategoryAsync(categoryId, dto);

            // Assert
            Assert.Equal(ResultStatus.Conflict, result.Status);

            await _mockCategoryRepo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateCategoryAsync_ShouldReturnSuccess_WhenCategoryIsUpdated()
        {
            // Arrange
            const int categoryId = 1;
            var expectedName = "Beverages";
            var token = new CancellationToken(false);

            var dto = new UpdateCategoryDto { Name = "<i>Beverages</i>" };
            var existingCategory = new Category { Id = categoryId, Name = "Dessert" };

            _mockCategoryRepo.GetByIdAsync(categoryId, token).Returns(existingCategory);
            _mockCategoryRepo.AnyAsync(Arg.Any<Expression<Func<Category, bool>>>(), token)
                             .Returns(false);

            // Act
            var result = await _categoryService.UpdateCategoryAsync(categoryId, dto, token);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);
            Assert.Equal(expectedName, result.Value!.Name);
            Assert.Equal(dto.Name, result.Value!.Name);
            Assert.Equal(categoryId, result.Value.Id);

            var expectedSlug = StringHelper.GenerateSlug(dto.Name);
            Assert.Equal(expectedSlug, result.Value!.Slug);
            Assert.Equal(expectedSlug, existingCategory.Slug);

            await _mockCategoryRepo.Received(1).GetByIdAsync
                (categoryId, token);
            await _mockCategoryRepo.Received(1).AnyAsync
                (Arg.Any<Expression<Func<Category, bool>>>(), token);
            await _mockCategoryRepo.Received(1).SaveChangesAsync(token);
        }

        [Fact]
        public async Task DeleteCategoryAsync_ShouldReturnNotFound_WhenCategoryDoesNotExist()
        {
            // Arrange
            const int categoryId = 99;
            _mockCategoryRepo.GetByIdAsync(categoryId, Arg.Any<CancellationToken>()).Returns((Category)null!);

            // Act
            var result = await _categoryService.DeleteCategoryAsync(categoryId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal(CategoryM.Errors.CategoryNotFound, result.Message);

            await _mockCategoryRepo.Received(1).GetByIdAsync
                (categoryId, Arg.Any<CancellationToken>());
            await _mockPostRepo.DidNotReceive().AnyAsync
                (Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>());
            await _mockCategoryRepo.DidNotReceive().DeleteAsync
                (Arg.Any<Category>(), Arg.Any<CancellationToken>());
            await _mockCategoryRepo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteCategoryAsync_ShouldReturnConflict_WhenCategoryHasRelatedPosts()
        {
            // Arrange
            const int categoryId = 1;
            var category = new Category { Id = categoryId, Name = "Beverages" };

            _mockCategoryRepo.GetByIdAsync(categoryId, Arg.Any<CancellationToken>()).Returns(category);

            _mockPostRepo.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>())
                         .Returns(true);

            // Act
            var result = await _categoryService.DeleteCategoryAsync(categoryId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Conflict, result.Status);
            Assert.Equal(CategoryM.Errors.CannotDeleteCategoryWithPosts, result.Message);

            await _mockCategoryRepo.Received(1).GetByIdAsync
                (categoryId, Arg.Any<CancellationToken>());
            await _mockPostRepo.Received(1).AnyAsync
                (Arg.Any<Expression<Func<Post, bool>>>(), Arg.Any<CancellationToken>());
            await _mockCategoryRepo.DidNotReceive().DeleteAsync
                (Arg.Any<Category>(), Arg.Any<CancellationToken>());
            await _mockCategoryRepo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteCategoryAsync_ShouldReturnNoContent_WhenDeletedSuccessfully()
        {
            // Arrange
            var token = new CancellationToken(false);
            const int categoryId = 1;
            var category = new Category { Id = categoryId, Name = "Empty Category" };

            _mockCategoryRepo.GetByIdAsync(categoryId, token).Returns(category);

            _mockPostRepo.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>(), token)
                         .Returns(false);

            // Act
            var result = await _categoryService.DeleteCategoryAsync(categoryId, token);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ResultStatus.Success, result.Status);

            await _mockCategoryRepo.Received(1).GetByIdAsync
                (categoryId, token);
            await _mockPostRepo.Received(1).AnyAsync
                (Arg.Any<Expression<Func<Post, bool>>>(), token);
            await _mockCategoryRepo.Received(1).DeleteAsync
                (category, token);
            await _mockCategoryRepo.Received(1).SaveChangesAsync(token);
        }
    }
}
