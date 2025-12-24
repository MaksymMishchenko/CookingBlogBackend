namespace PostApiService.Infrastructure.Constants
{
    public static class ValidationErrorMessages
    {       
        public const string Required = "{0} is required.";
        public const string LengthRange = "{0} must be between {2} and {1} characters.";
        public const string MaxLength = "{0} cannot exceed {1} characters.";
       
        public const string InvalidUrl = "Invalid URL format.";
        public const string SlugFormat = "Slug must only contain lowercase letters, numbers, and hyphens.";
        public const string InvalidCategory = "Please select a valid category.";
    }
}
