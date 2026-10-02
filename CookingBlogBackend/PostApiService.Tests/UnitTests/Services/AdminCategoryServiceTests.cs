using PostApiService.Helper;
using PostApiService.Infrastructure.Common;
using PostApiService.Infrastructure.Services;
using PostApiService.Models.Dto.Requests;
using PostApiService.Repositories;
using PostApiService.Services;
using System.Linq.Expressions;

namespace PostApiService.Tests.UnitTests.Services
{
    public class AdminCategoryServiceTests
    {
        private readonly ICategoryRepository _mockCategoryRepo;
        private readonly IPostRepository _mockPostRepo;
        private readonly IWebContext _mockWebContext;
        private readonly AdminCategoryService _categoryService;

        public AdminCategoryServiceTests()
        {
            _mockPostRepo = Substitute.For<IPostRepository>();
            _mockWebContext = Substitute.For<IWebContext>();
            _mockCategoryRepo = Substitute.For<ICategoryRepository>();
            _categoryService = new AdminCategoryService(_mockCategoryRepo, _mockWebContext, _mockPostRepo);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_ShouldReturnSuccess_WhenCategoryExists()
        {
            // Arrange
            const int categoryId = 1;
            var category = new Category { Id = categoryId, Name = "Test Category" };

            _mockWebContext.UserId.Returns("admin-id");
            _mockWebContext.IsAdmin.Returns(true);

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

            _mockWebContext.UserId.Returns("admin-id");
            _mockWebContext.IsAdmin.Returns(true);

            _mockCategoryRepo.GetByIdAsync(categoryId, Arg.Any<CancellationToken>()).Returns((Category)null!);

            // Act
            var result = await _categoryService.GetCategoryByIdAsync(categoryId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal(CategoryM.Errors.CategoryNotFound, result.Message);
            Assert.Null(result.Value);

            await _mockCategoryRepo.Received(1).GetByIdAsync(categoryId, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetCategoryByIdAsync_ShouldReturnUnauthorized_WhenUserIsNotLoggedIn()
        {
            // Arrange
            const int categoryId = 1;
            _mockWebContext.UserId.Returns((string?)null);

            // Act
            var result = await _categoryService.GetCategoryByIdAsync(categoryId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Unauthorized, result.Status);

            await _mockCategoryRepo.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetCategoryByIdAsync_ShouldReturnForbidden_WhenUserIsNotAdmin()
        {
            // Arrange
            const int categoryId = 1;
            _mockWebContext.UserId.Returns("user-id");
            _mockWebContext.IsAdmin.Returns(false);

            // Act
            var result = await _categoryService.GetCategoryByIdAsync(categoryId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Forbidden, result.Status);

            await _mockCategoryRepo.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddCategoryAsync_ShouldStripHtmlFromName_BeforeSaving()
        {
            // Arrange
            var dto = new CreateCategoryDto { Name = "<b>Healthy</b> Food" };
            var expectedName = "Healthy Food";
            var expectedSlug = "healthy-food";

            _mockWebContext.UserId.Returns("admin-id");
            _mockWebContext.IsAdmin.Returns(true);

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

            _mockWebContext.UserId.Returns("admin-id");
            _mockWebContext.IsAdmin.Returns(true);

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

            _mockWebContext.UserId.Returns("admin-id");
            _mockWebContext.IsAdmin.Returns(true);

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

            _mockWebContext.UserId.Returns("admin-id");
            _mockWebContext.IsAdmin.Returns(true);

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
        public async Task AddCategoryAsync_ShouldReturnUnauthorized_WhenUserIsNotLoggedIn()
        {
            // Arrange
            var dto = new CreateCategoryDto { Name = "Dessert" };
            _mockWebContext.UserId.Returns((string?)null);

            // Act
            var result = await _categoryService.AddCategoryAsync(dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Unauthorized, result.Status);

            await _mockCategoryRepo.DidNotReceive().AnyAsync(Arg.Any<Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddCategoryAsync_ShouldReturnForbidden_WhenUserIsNotAdmin()
        {
            // Arrange
            var dto = new CreateCategoryDto { Name = "Dessert" };
            _mockWebContext.UserId.Returns("user-id");
            _mockWebContext.IsAdmin.Returns(false);

            // Act
            var result = await _categoryService.AddCategoryAsync(dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Forbidden, result.Status);

            await _mockCategoryRepo.DidNotReceive().AnyAsync(Arg.Any<Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateCategoryAsync_ShouldReturnNotFound_WhenCategoryDoesNotExist()
        {
            // Arrange
            const int categoryId = 1;
            var dto = new UpdateCategoryDto { Name = "Bakery" };

            _mockWebContext.UserId.Returns("admin-id");
            _mockWebContext.IsAdmin.Returns(true);

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

            _mockWebContext.UserId.Returns("admin-id");
            _mockWebContext.IsAdmin.Returns(true);

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

            _mockWebContext.UserId.Returns("admin-id");
            _mockWebContext.IsAdmin.Returns(true);

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

            _mockWebContext.UserId.Returns("admin-id");
            _mockWebContext.IsAdmin.Returns(true);

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
        public async Task UpdateCategoryAsync_ShouldReturnUnauthorized_WhenUserIsNotLoggedIn()
        {
            // Arrange
            const int categoryId = 1;
            var dto = new UpdateCategoryDto { Name = "Bakery" };

            _mockWebContext.UserId.Returns((string?)null);

            // Act
            var result = await _categoryService.UpdateCategoryAsync(categoryId, dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Unauthorized, result.Status);

            await _mockCategoryRepo.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateCategoryAsync_ShouldReturnForbidden_WhenUserIsNotAdmin()
        {
            // Arrange
            const int categoryId = 1;
            var dto = new UpdateCategoryDto { Name = "Bakery" };

            _mockWebContext.UserId.Returns("user-id");
            _mockWebContext.IsAdmin.Returns(false);

            // Act
            var result = await _categoryService.UpdateCategoryAsync(categoryId, dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Forbidden, result.Status);

            await _mockCategoryRepo.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteCategoryAsync_ShouldReturnNotFound_WhenCategoryDoesNotExist()
        {
            // Arrange
            const int categoryId = 99;

            _mockWebContext.UserId.Returns("admin-id");
            _mockWebContext.IsAdmin.Returns(true);

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

            _mockWebContext.UserId.Returns("admin-id");
            _mockWebContext.IsAdmin.Returns(true);

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
        public async Task DeleteCategoryAsync_ShouldReturnSuccess_WhenDeletedSuccessfully()
        {
            // Arrange
            var token = new CancellationToken(false);
            const int categoryId = 1;
            var category = new Category { Id = categoryId, Name = "Empty Category" };

            _mockWebContext.UserId.Returns("admin-id");
            _mockWebContext.IsAdmin.Returns(true);

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

        [Fact]
        public async Task DeleteCategoryAsync_ShouldReturnUnauthorized_WhenUserIsNotLoggedIn()
        {
            // Arrange
            const int categoryId = 1;
            _mockWebContext.UserId.Returns((string?)null);

            // Act
            var result = await _categoryService.DeleteCategoryAsync(categoryId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Unauthorized, result.Status);

            await _mockCategoryRepo.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteCategoryAsync_ShouldReturnForbidden_WhenUserIsNotAdmin()
        {
            // Arrange
            const int categoryId = 1;
            _mockWebContext.UserId.Returns("user-id");
            _mockWebContext.IsAdmin.Returns(false);

            // Act
            var result = await _categoryService.DeleteCategoryAsync(categoryId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.Forbidden, result.Status);

            await _mockCategoryRepo.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }
    }
}
