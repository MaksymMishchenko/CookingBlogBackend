using Microsoft.AspNetCore.Http;
using PostApiService.Controllers.Filters;
using PostApiService.Models.Dto.Requests;
using System.ComponentModel.DataAnnotations;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace PostApiService.Tests.UnitTests.Filters
{
    public class AllowedFileAttributeTests
    {
        [Fact]
        public void IsValid_ValidFile_ReturnsSuccess()
        {
            // Arrange
            var attribute = new AllowedFileAttribute(maxFileSizeMb: 5, ".jpg", ".png");
            var fileMock = CreateFormFile(1024 * 1024, "test.png");
            var context = new ValidationContext(new object());

            // Act           
            var result = attribute.GetValidationResult(fileMock, context);

            // Assert
            Assert.Equal(ValidationResult.Success, result);
        }

        [Fact]
        public void IsValid_NullOrNotIFormFile_ReturnsRequiredError()
        {
            // Arrange
            var attribute = new AllowedFileAttribute();
            var dto = new PostCreateDto();
            
            var context = new ValidationContext(dto, serviceProvider: null, items: null)
            {
                MemberName = nameof(PostCreateDto.PreviewImage)
            };

            // Act
            var result = attribute.GetValidationResult(null, context);

            // Assert
            Assert.NotNull(result);            
            var expectedMessage = string.Format(Global.Validation.Required, "PreviewImage");
            Assert.Equal(expectedMessage, result.ErrorMessage);
        }

        [Fact]
        public void IsValid_FileTooLarge_ReturnsFileTooLargeError()
        {
            // Arrange
            var attribute = new AllowedFileAttribute(maxFileSizeMb: 1);
            var fileMock = CreateFormFile(2 * 1024 * 1024, "large.png");
            var context = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(fileMock, context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(Global.Validation.FileTooLarge, result.ErrorMessage);
        }

        [Fact]
        public void IsValid_InvalidExtension_ReturnsInvalidFormatError()
        {
            // Arrange
            var attribute = new AllowedFileAttribute(maxFileSizeMb: 5, ".jpg", ".png");
            var fileMock = CreateFormFile(1024, "document.txt");
            var context = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(fileMock, context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(Global.Validation.InvalidFormat, result.ErrorMessage);
        }

        [Fact]
        public void IsValid_DefaultExtensionsWhenNotProvided_AllowsWebpAndJpg()
        {
            // Arrange
            var attribute = new AllowedFileAttribute();
            var fileMock = CreateFormFile(1024, "photo.webp");
            var context = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(fileMock, context);

            // Assert
            Assert.Equal(ValidationResult.Success, result);
        }

        private IFormFile CreateFormFile(long length, string fileName)
        {
            var stream = new MemoryStream(new byte[length]);
            return new FormFile(stream, 0, length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/octet-stream"
            };
        }
    }
}
