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
            var fileMock = CreateFormFileWithMagicBytes(GetPngBytes(), "test.png");
            var context = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(fileMock, context);

            // Assert
            Assert.Equal(ValidationResult.Success, result);
        }

        [Fact]
        public void IsValid_NullAndRequiredFalse_ReturnsSuccess()
        {
            // Arrange
            var attribute = new AllowedFileAttribute { Required = false };
            var context = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(null, context);

            // Assert
            Assert.Equal(ValidationResult.Success, result);
        }

        [Fact]
        public void IsValid_Null_ReturnsRequiredError()
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
            var expectedMessage = string.Format(Global.Validation.Required, context.DisplayName);
            Assert.Equal(expectedMessage, result.ErrorMessage);
        }

        [Fact]
        public void IsValid_EmptyFileAndRequiredFalse_ReturnsError()
        {
            // Arrange
            var attribute = new AllowedFileAttribute { Required = false };
            var emptyFile = CreateFormFile(0, "empty.png");
            var context = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(emptyFile, context);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(ValidationResult.Success, result);
        }

        [Fact]
        public void IsValid_NotIFormFileValue_ReturnsError()
        {
            // Arrange
            var attribute = new AllowedFileAttribute();
            var context = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult("not a file", context);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(ValidationResult.Success, result);
        }

        [Fact]
        public void IsValid_FileTooLarge_ReturnsFileTooLargeError()
        {
            // Arrange
            const int maxFileSize = 5;
            var attribute = new AllowedFileAttribute(maxFileSizeMb: maxFileSize);

            var fileMock = CreateFormFile(6 * 1024 * 1024, "large.png");
            var context = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(fileMock, context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(string.Format(Global.Validation.FileTooLarge, maxFileSize), result.ErrorMessage);
        }

        [Fact]
        public void IsValid_FileTooLargeAndRequiredFalse_StillReturnsFileTooLargeError()
        {
            // Arrange
            const int maxFileSize = 1;
            var attribute = new AllowedFileAttribute(maxFileSizeMb: maxFileSize) { Required = false };
            var fileMock = CreateFormFile(2 * 1024 * 1024, "large.png");
            var context = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(fileMock, context);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.ErrorMessage);
            Assert.Equal(string.Format(Global.Validation.FileTooLarge, maxFileSize), result.ErrorMessage);
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
            var fileMock = CreateFormFileWithMagicBytes(GetWebpBytes(), "photo.webp");
            var context = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(fileMock, context);

            // Assert
            Assert.Equal(ValidationResult.Success, result);
        }

        [Fact]
        public void IsValid_UppercaseExtension_ReturnsSuccess()
        {
            // Arrange
            var attribute = new AllowedFileAttribute();
            var fileMock = CreateFormFileWithMagicBytes(GetPngBytes(), "PHOTO.PNG");
            var context = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(fileMock, context);

            // Assert
            Assert.Equal(ValidationResult.Success, result);
        }        

        [Fact]
        public void IsValid_SpoofedFileExtension_ReturnsInvalidFormatError()
        {
            // Arrange
            var attribute = new AllowedFileAttribute();
            var spoofedFile = CreateFormFile(1024, "malware.png");
            var context = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(spoofedFile, context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(Global.Validation.InvalidFormat, result.ErrorMessage);
        }

        [Fact]
        public void IsValid_ValidJpecMagicBytes_ReturnsSuccess()
        {
            // Arrange
            var attribute = new AllowedFileAttribute();
            var fileMock = CreateFormFileWithMagicBytes(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 }, "picture.jpg");
            var context = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(fileMock, context);

            // Assert
            Assert.Equal(ValidationResult.Success, result);
        }        

        private byte[] GetPngBytes() => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];
        private byte[] GetWebpBytes() => "RIFFXXXXWEBP"u8.ToArray();

        private IFormFile CreateFormFile(long length, string fileName)
        {
            var stream = new MemoryStream(new byte[length]);
            return new FormFile(stream, 0, length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/octet-stream"
            };
        }

        private IFormFile CreateFormFileWithMagicBytes(byte[] magicBytes, string fileName)
        {
            var stream = new MemoryStream(magicBytes);
            return new FormFile(stream, 0, magicBytes.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/octet-stream"
            };
        }
    }
}