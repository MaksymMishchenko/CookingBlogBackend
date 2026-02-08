using PostApiService.Controllers;
using PostApiService.Infrastructure.Common;
using PostApiService.Interfaces;
using PostApiService.Models.Common;
using PostApiService.Models.Dto.Requests;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Tests.UnitTests.Controllers
{
    public class AdminCategoriesControllerTests
    {
        private readonly ICategoryService _mockCategoryService;
        private readonly AdminCategoriesController _categoryController;

        public AdminCategoriesControllerTests()
        {
            _mockCategoryService = Substitute.For<ICategoryService>();
            _categoryController = new AdminCategoriesController(_mockCategoryService);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_ShouldReturnStatusCode200Ok()
        {
            // Arrange
            var token = new CancellationToken(false);
            const int CategoryId = 3;
            var categoryFromService = new CategoryDto(1, "Test", "test-slug");
            var expectedResult = Result<CategoryDto>.Success(categoryFromService);

            _mockCategoryService.GetCategoryByIdAsync(CategoryId, token)
                .Returns(expectedResult);

            // Act
            var result = await _categoryController.GetCategoryByIdAsync(CategoryId, token);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            var actualResult = Assert.IsType<ApiResponse<CategoryDto>>(okResult.Value);
            Assert.Equal(categoryFromService, actualResult.Data);

            await _mockCategoryService.Received(1).GetCategoryByIdAsync(CategoryId, token);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_ShouldReturnErrorWithStatusCode404NotFound()
        {
            // Arrange                       
            const int Non_Existent_Category = 7;
            var expectedResult = Result<CategoryDto>.NotFound(CategoryM.Errors.CategoryNotFound);

            _mockCategoryService.GetCategoryByIdAsync(Non_Existent_Category, Arg.Any<CancellationToken>())
                .Returns(expectedResult);

            // Act
            var result = await _categoryController.GetCategoryByIdAsync(Non_Existent_Category);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var actualResult = Assert.IsType<ApiResponse>(notFoundResult.Value);
            Assert.Equal(CategoryM.Errors.CategoryNotFound, actualResult.Message);

            await _mockCategoryService.Received(1).GetCategoryByIdAsync
                (Non_Existent_Category, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddCategoryAsync_ShouldCreateCategory_WithStatusCode201Created()
        {
            // Arrange
            var token = new CancellationToken(false);
            var createDto = new CreateCategoryDto { Name = "New Category", Slug = "new-category-slug" };
            var responseDto = new CategoryDto(1, createDto.Name, createDto.Slug);
            var expectedResult = Result<CategoryDto>.Success(responseDto);

            _mockCategoryService.AddCategoryAsync(createDto, token)
                .Returns(expectedResult);

            // Act
            var result = await _categoryController.AddСategoryAsync(createDto, token);

            // Assert
            var createdAtAction = Assert.IsType<CreatedAtActionResult>(result);
            var actualResult = Assert.IsType<ApiResponse<CategoryDto>>(createdAtAction.Value);
            Assert.Equal(responseDto, actualResult.Data);

            Assert.Equal(responseDto.Id, createdAtAction.RouteValues!["id"]);

            await _mockCategoryService.Received(1).AddCategoryAsync
                (createDto, token);
        }

        [Fact]
        public async Task AddCategoryAsync_ShouldReturnError_WithStatusCode409Conflict()
        {
            // Arrange
            var createDto = new CreateCategoryDto { Name = "Existing Category" };
            var errorMessage = string.Format(CategoryM.Errors.CategoryOrSlugExists, createDto.Name, createDto.Slug);

            var expectedResult = Result<CategoryDto>.Conflict(errorMessage);

            _mockCategoryService.AddCategoryAsync(createDto, Arg.Any<CancellationToken>())
                .Returns(expectedResult);

            // Act
            var result = await _categoryController.AddСategoryAsync(createDto);

            // Assert           
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse>(conflictResult.Value);

            Assert.Equal(errorMessage, apiResponse.Message);

            await _mockCategoryService.Received(1).AddCategoryAsync(createDto, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateCategoryAsync_ShouldReturnUpdatedCategory_WithStatusCode200Ok()
        {
            // Arrange
            var token = new CancellationToken(false);
            const int categoryId = 1;
            var updateDto = new UpdateCategoryDto { Name = "Updated Name" };
            var responseDto = new CategoryDto(categoryId, "Updated Name", "test-slug");
            var expectedResult = Result<CategoryDto>.Success(responseDto);

            _mockCategoryService.UpdateCategoryAsync(categoryId, updateDto, token)
                .Returns(expectedResult);

            // Act
            var result = await _categoryController.UpdateCategoryAsync(categoryId, updateDto, token);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<CategoryDto>>(okResult.Value);
            Assert.Equal(responseDto, apiResponse.Data);

            await _mockCategoryService.Received(1).UpdateCategoryAsync(categoryId, updateDto, token);
        }

        [Fact]
        public async Task UpdateCategoryAsync_ShouldReturnError_WithStatusCode404NotFound()
        {
            // Arrange
            const int categoryId = 99;
            var updateDto = new UpdateCategoryDto { Name = "New Name" };
            var expectedResult = Result<CategoryDto>.NotFound(CategoryM.Errors.CategoryNotFound);

            _mockCategoryService.UpdateCategoryAsync(categoryId, updateDto, Arg.Any<CancellationToken>())
                .Returns(expectedResult);

            // Act
            var result = await _categoryController.UpdateCategoryAsync(categoryId, updateDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse>(notFoundResult.Value);
            Assert.Equal(CategoryM.Errors.CategoryNotFound, apiResponse.Message);

            await _mockCategoryService.Received(1)
                .UpdateCategoryAsync(categoryId, updateDto, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateCategoryAsync_ShouldReturnError_WithStatusCode409Conflict()
        {
            // Arrange
            const int categoryId = 1;
            var updateDto = new UpdateCategoryDto { Name = "Existing Category" };
            var errorMessage = string.Format(CategoryM.Errors.CategoryOrSlugExists, updateDto.Name, updateDto.Slug);
            var expectedResult = Result<CategoryDto>.Conflict(errorMessage);

            _mockCategoryService.UpdateCategoryAsync(categoryId, updateDto, Arg.Any<CancellationToken>())
                .Returns(expectedResult);

            // Act
            var result = await _categoryController.UpdateCategoryAsync(categoryId, updateDto);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse>(conflictResult.Value);
            Assert.Equal(errorMessage, apiResponse.Message);

            await _mockCategoryService.Received(1)
                .UpdateCategoryAsync(categoryId, updateDto, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteCategoryAsync_ShouldReturnOk_WithStatusCode200Ok_WhenDeletedSuccessfully()
        {
            // Arrange
            var token = new CancellationToken(false);
            const int categoryId = 1;
            var successMessage = CategoryM.Success.CategoryDeletedSuccessfully;
            var expectedResult = Result.Success(successMessage);

            _mockCategoryService.DeleteCategoryAsync(categoryId, token)
                .Returns(expectedResult);

            // Act
            var result = await _categoryController.DeleteCategoryAsync(categoryId, token);

            // Assert            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse>(okResult.Value);
            Assert.Equal(successMessage, apiResponse.Message);

            await _mockCategoryService.Received(1).DeleteCategoryAsync(categoryId, token);
        }

        [Fact]
        public async Task DeleteCategoryAsync_ShouldReturnError_WithStatusCode404NotFound_WhenCategoryDoesNotExist()
        {
            // Arrange
            const int categoryId = 99;
            var expectedResult = Result<bool>.NotFound(CategoryM.Errors.CategoryNotFound);

            _mockCategoryService.DeleteCategoryAsync(categoryId, Arg.Any<CancellationToken>())
                .Returns(expectedResult);

            // Act
            var result = await _categoryController.DeleteCategoryAsync(categoryId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse>(notFoundResult.Value);
            Assert.Equal(CategoryM.Errors.CategoryNotFound, apiResponse.Message);

            await _mockCategoryService.Received(1).DeleteCategoryAsync(categoryId, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteCategoryAsync_ShouldReturnError_WithStatusCode409Conflict_WhenCategoryHasPosts()
        {
            // Arrange
            const int categoryId = 1;
            var errorMessage = CategoryM.Errors.CannotDeleteCategoryWithPosts;
            var expectedResult = Result<bool>.Conflict(errorMessage);

            _mockCategoryService.DeleteCategoryAsync(categoryId, Arg.Any<CancellationToken>())
                .Returns(expectedResult);

            // Act
            var result = await _categoryController.DeleteCategoryAsync(categoryId);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse>(conflictResult.Value);
            Assert.Equal(errorMessage, apiResponse.Message);

            await _mockCategoryService.Received(1).DeleteCategoryAsync(categoryId, Arg.Any<CancellationToken>());
        }
    }
}
