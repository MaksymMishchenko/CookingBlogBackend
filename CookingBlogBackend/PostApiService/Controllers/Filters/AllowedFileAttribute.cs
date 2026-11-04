using System.ComponentModel.DataAnnotations;

namespace PostApiService.Controllers.Filters
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class AllowedFileAttribute : ValidationAttribute
    {
        public bool Required { get; set; } = true;

        public int MaxFiles { get; set; } = int.MaxValue;

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
            if (value is IEnumerable<IFormFile> files)
            {
                var list = files.ToList();

                if (list.Count == 0)
                    return RequiredOrSuccess(validationContext);

                if (list.Count > MaxFiles)
                    return new ValidationResult(
                        $"{validationContext.DisplayName}: maximum {MaxFiles} files allowed.");

                foreach (var f in list)
                {
                    var error = ValidateFile(f, validationContext);
                    if (error != null) return error;
                }

                return ValidationResult.Success;
            }

            if (value is null)
                return RequiredOrSuccess(validationContext);

            if (value is not IFormFile file)
                return new ValidationResult(
                    string.Format(Global.Validation.Required, validationContext.DisplayName));

            return ValidateFile(file, validationContext);
        }

        private ValidationResult? RequiredOrSuccess(ValidationContext ctx) =>
            Required
                ? new ValidationResult(string.Format(Global.Validation.Required, ctx.DisplayName))
                : ValidationResult.Success;

        private ValidationResult? ValidateFile(IFormFile file, ValidationContext ctx)
        {
            if (file.Length == 0)
                return new ValidationResult(string.Format(Global.Validation.Required, ctx.DisplayName));

            if (file.Length > _maxFileSizeMb * 1024 * 1024)
                return new ValidationResult(string.Format(Global.Validation.FileTooLarge, _maxFileSizeMb));

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                return new ValidationResult(Global.Validation.InvalidFormat);

            if (!HasImageSignature(file))
                return new ValidationResult(Global.Validation.InvalidFormat);

            return ValidationResult.Success;
        }

        private static bool HasImageSignature(IFormFile file)
        {
            Span<byte> h = stackalloc byte[12];

            using var stream = file.OpenReadStream();
            var read = stream.ReadAtLeast(h, h.Length, throwOnEndOfStream: false);
            h = h[..read];

            return h.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }) ||
                   h.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }) ||
                   (h.Length >= 12 && h[..4].SequenceEqual("RIFF"u8) && h[8..12].SequenceEqual("WEBP"u8));
        }
    }
}
