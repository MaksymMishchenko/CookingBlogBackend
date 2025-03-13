namespace PostApiService.Exceptions
{
    public static class TokenErrorMessages
    {
        public const string SecretKeyNullOrEmpty = "Secret key cannot be null or empty.";
        public const string TokenExpirationInvalid = "Token expiration time must be greater than zero.";
        public const string ClaimsNull = "Claims cannot be null.";
        public const string TokenGenerationFailed = "An error occurred while generating the token.";
        public const string GenerationFailed = "An unexpected error occurred.";
    }
}
