using PostApiService.Models.Dto.Requests;
using PostApiService.Repositories;
using PostApiService.Services;
using System.Linq.Expressions;
using System.Net;

namespace PostApiService.Tests.UnitTests.Services
{
    public class CategoryServiceTests
    {
        private readonly IRepository<Category> _mockCategoryRepo;
        private readonly IRepository<Post> _mockPostRepo;
        private readonly CategoryService _categoryService;

        public CategoryServiceTests()
        {
            _mockPostRepo = Substitute.For<IRepository<Post>>();
            _mockCategoryRepo = Substitute.For<IRepository<Category>>();
            _categoryService = new CategoryService(_mockCategoryRepo, _mockPostRepo);
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnSuccessWithTrue_IfCategoryExists()
        {
            // Arrange
            const int CategoryId = 1;
            _mockCategoryRepo.AnyAsync(Arg.Any<Expression<Func<Category, bool>>>())
                           .Returns(true);

            // Act
            var result = await _categoryService.ExistsAsync(CategoryId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value);
        }

        [Fact]
        public async Task GetCategoryById_ShouldReturnNotFound_IfNotFound()
        {
            // Arrange
            const int categoryId = 999;
            _mockCategoryRepo.GetByIdAsync(categoryId).Returns((Category)null!);

            // Act
            var result = await _categoryService.GetCategoryByIdAsync(categoryId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            Assert.Equal(CategoryM.Errors.CategoryNotFound, result.ErrorMessage);
        }

        [Fact]
        public async Task GetAllCategoriesAsync_ShouldReturnListWithAllCategories()
        {
            // Arrange
            var categories = TestDataHelper.GetCulinaryCategories();
            _mockCategoryRepo.GetAllAsync().Returns(categories);

            // Act
            var result = await _categoryService.GetAllCategoriesAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.NotNull(result.Value);
            Assert.Equal(categories.Count, result.Value.Count);

            Assert.Equal(categories[0].Name, result.Value[0].Name);
            Assert.Equal(categories[0].Id, result.Value[0].Id);
        }

        [Fact]
        public async Task GetAllCategoriesAsync_ShouldReturnEmptyList_WhenNoCategoriesExist()
        {
            // Arrange
            _mockCategoryRepo.GetAllAsync().Returns(new List<Category>());

            // Act
            var result = await _categoryService.GetAllCategoriesAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value!);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_ShouldReturnSuccess_WhenCategoryExists()
        {
            // Arrange
            const int categoryId = 1;
            var category = new Category { Id = categoryId, Name = "Test Category" };

            _mockCategoryRepo.GetByIdAsync(categoryId).Returns(category);

            // Act
            var result = await _categoryService.GetCategoryByIdAsync(categoryId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.NotNull(result.Value);
            Assert.Equal(category.Name, result.Value.Name);
            Assert.Equal(category.Id, result.Value.Id);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_ShouldReturnNotFound_WhenCategoryDoesNotExist()
        {
            // Arrange
            const int categoryId = 99;
            _mockCategoryRepo.GetByIdAsync(categoryId).Returns((Category)null!);

            var expectedMessage = string.Format(CategoryM.Errors.CategoryNotFound, categoryId);

            // Act
            var result = await _categoryService.GetCategoryByIdAsync(categoryId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            Assert.Equal(expectedMessage, result.ErrorMessage);
            Assert.Null(result.Value);
        }

        [Fact]
        public async Task AddCategoryAsync_ShouldReturnConflict_WhenCategoryNameAlreadyExists()
        {
            // Arrange
            var dto = new CreateCategoryDto { Name = "Dessert" };
            _mockCategoryRepo.AnyAsync(Arg.Any<Expression<Func<Category, bool>>>())
                             .Returns(true);

            var expectedMessage = string.Format(CategoryM.Errors.CategoryAlreadyExists, dto.Name);

            // Act
            var result = await _categoryService.AddCategoryAsync(dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
            Assert.Equal(expectedMessage, result.ErrorMessage);
            await _mockCategoryRepo.DidNotReceive().AddAsync(Arg.Any<Category>());
        }

        [Fact]
        public async Task AddCategoryAsync_ShouldReturnSuccess_WhenCategoryIsCreated()
        {
            // Arrange
            var dto = new CreateCategoryDto { Name = "Dessert" };
            _mockCategoryRepo.AnyAsync(Arg.Any<Expression<Func<Category, bool>>>())
                             .Returns(false);

            _mockCategoryRepo.AddAsync(Arg.Any<Category>())
                             .Returns(x => x.Arg<Category>());

            // Act
            var result = await _categoryService.AddCategoryAsync(dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.NotNull(result.Value);
            Assert.Equal(dto.Name, result.Value.Name);
            await _mockCategoryRepo.Received(1).AddAsync(Arg.Is<Category>(c => c.Name == dto.Name));
        }

        [Fact]
        public async Task UpdateCategoryAsync_ShouldReturnNotFound_WhenCategoryDoesNotExist()
        {
            // Arrange
            const int categoryId = 1;
            var dto = new UpdateCategoryDto { Name = "Bakery" };
            _mockCategoryRepo.GetByIdAsync(categoryId).Returns((Category)null!);

            // Act
            var result = await _categoryService.UpdateCategoryAsync(categoryId, dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            Assert.Equal(CategoryM.Errors.CategoryNotFound, result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateCategoryAsync_ShouldReturnConflict_WhenNewNameAlreadyExistsForOtherCategory()
        {
            // Arrange
            const int categoryId = 1;
            var dto = new UpdateCategoryDto { Name = "Dessert" };
            var existingCategory = new Category { Id = categoryId, Name = "Beverages" };

            _mockCategoryRepo.GetByIdAsync(categoryId).Returns(existingCategory);

            _mockCategoryRepo.AnyAsync(Arg.Any<Expression<Func<Category, bool>>>())
                             .Returns(true);

            var expectedMessage = string.Format(CategoryM.Errors.CategoryAlreadyExists, dto.Name);

            // Act
            var result = await _categoryService.UpdateCategoryAsync(categoryId, dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
            Assert.Equal(expectedMessage, result.ErrorMessage);
            await _mockCategoryRepo.DidNotReceive().UpdateAsync(Arg.Any<Category>());
        }

        [Fact]
        public async Task UpdateCategoryAsync_ShouldReturnSuccess_WhenCategoryIsUpdated()
        {
            // Arrange
            const int categoryId = 1;
            var dto = new UpdateCategoryDto { Name = "Beverages" };
            var existingCategory = new Category { Id = categoryId, Name = "Dessert" };

            _mockCategoryRepo.GetByIdAsync(categoryId).Returns(existingCategory);
            _mockCategoryRepo.AnyAsync(Arg.Any<Expression<Func<Category, bool>>>())
                             .Returns(false);

            // Act
            var result = await _categoryService.UpdateCategoryAsync(categoryId, dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal(dto.Name, result.Value!.Name);
            Assert.Equal(categoryId, result.Value.Id);

            await _mockCategoryRepo.Received(1).UpdateAsync(Arg.Is<Category>(c => c.Name == dto.Name));
        }

        [Fact]
        public async Task DeleteCategoryAsync_ShouldReturnNotFound_WhenCategoryDoesNotExist()
        {
            // Arrange
            const int categoryId = 99;
            _mockCategoryRepo.GetByIdAsync(categoryId).Returns((Category)null!);

            // Act
            var result = await _categoryService.DeleteCategoryAsync(categoryId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            Assert.Equal(CategoryM.Errors.CategoryNotFound, result.ErrorMessage);

            await _mockPostRepo.DidNotReceiveWithAnyArgs().AnyAsync(default!);
        }

        [Fact]
        public async Task DeleteCategoryAsync_ShouldReturnConflict_WhenCategoryHasRelatedPosts()
        {
            // Arrange
            const int categoryId = 1;
            var category = new Category { Id = categoryId, Name = "Beverages" };

            _mockCategoryRepo.GetByIdAsync(categoryId).Returns(category);

            _mockPostRepo.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>())
                         .Returns(true);

            // Act
            var result = await _categoryService.DeleteCategoryAsync(categoryId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
            Assert.Equal(CategoryM.Errors.CannotDeleteCategoryWithPosts, result.ErrorMessage);

            await _mockCategoryRepo.DidNotReceive().DeleteAsync(Arg.Any<Category>());
        }

        [Fact]
        public async Task DeleteCategoryAsync_ShouldReturnNoContent_WhenDeletedSuccessfully()
        {
            // Arrange
            const int categoryId = 1;
            var category = new Category { Id = categoryId, Name = "Empty Category" };

            _mockCategoryRepo.GetByIdAsync(categoryId).Returns(category);

            _mockPostRepo.AnyAsync(Arg.Any<Expression<Func<Post, bool>>>())
                         .Returns(false);

            // Act
            var result = await _categoryService.DeleteCategoryAsync(categoryId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);

            await _mockCategoryRepo.Received(1).DeleteAsync(category);
        }

    }
}
