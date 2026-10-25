using System.ComponentModel.DataAnnotations;

namespace PostApiService.Controllers.Filters
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class AllowedFileAttribute : ValidationAttribute
    {
        public bool Required { get; set; } = true;
        private readonly int _maxFileSizeMb;
        private readonly string[] _allowedExtensions;

        public AllowedFileAttribute(int maxFileSizeMb = 5, params string[]? allowedExtensions)
        {
            _maxFileSizeMb = maxFileSizeMb;
            _allowedExtensions = allowedExtensions is { Length: > 0 }
                ? allowedExtensions
                : [".jpg", ".jpeg", ".png", ".webp"];
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null)
            {
                return Required
                    ? new ValidationResult(string.Format(Global.Validation.Required, validationContext.DisplayName))
                    : ValidationResult.Success;
            }

            if (value is not IFormFile file || file.Length == 0)
            {
                return new ValidationResult(string.Format(Global.Validation.Required,
                    validationContext.DisplayName));
            }
            
            if (file.Length > _maxFileSizeMb * 1024 * 1024)
            {
                return new ValidationResult(string.Format(Global.Validation.FileTooLarge, _maxFileSizeMb));
            }
            
            if (_allowedExtensions.Length > 0)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(extension))
                {
                    return new ValidationResult(Global.Validation.InvalidFormat);
                }
            }

            return ValidationResult.Success;
        }
    }
}
