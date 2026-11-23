namespace PostApiService.Infrastructure.Constants
{
    public static class StorageConstants
    {
        public static class Limits
        {
            public const int MaxImageSizeMb = 5;
            public const long MaxImageSizeBytes = MaxImageSizeMb * 1024 * 1024;
            public const long MaxImageRequestSizeBytes = MaxImageSizeBytes + (2 * 1024 * 1024);
            public const long MaxGlobalRequestSizeBytes = 15 * 1024 * 1024;
        }

        public static class Folders
        {
            public const string Posts = "posts";
            public const string PostContent = "posts/content";
            public const string DefaultPreviewSlug = "recipe-preview";
            public const string ContentImagePrefix = "img";
        }

        public static class ImageProcessing
        {
            public const int PreviewWidth = 640;
            public const int PreviewHeight = 480;
            public const string WebpFormat = "webp";
            public const int PublicIdSuffixLength = 6;
            public const string CloudinaryUploadSegment = "/upload/";
        }
    }
}
