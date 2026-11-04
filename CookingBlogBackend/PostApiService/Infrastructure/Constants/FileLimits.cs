namespace PostApiService.Infrastructure.Constants
{
    public static class FileLimits
    {
        public const int MaxPostImageSizeMb = 5;
        public const int MaxContentImagesCount = 10;
        public const int MaxPostRequestBytes = 10 * 1024 * 1024;
        public const string ContentImagesFolder = "posts/content";
    }
}
