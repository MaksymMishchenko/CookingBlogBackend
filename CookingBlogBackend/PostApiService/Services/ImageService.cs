using PostApiService.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace PostApiService.Services
{
    /// <summary>
    /// TODO: [Refactor to Cloudinary](https://github.com/MaksymMishchenko/CookingBlogBackend/issues/96)
    /// Currently saves images locally to wwwroot/images/posts. 
    /// Must be refactored to use Cloudinary or external object storage to prevent data loss on ephemeral environments (e.g., Render).
    /// </summary>
    public class ImageService : BaseService, IImageService
    {
        private readonly IWebHostEnvironment _env;

        private const int MinImageDimension = 250;

        public ImageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        private async Task ProcessAndSaveImageAsync(IFormFile file, string filePath)
        {
            using (var image = await Image.LoadAsync(file.OpenReadStream()))
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(250, 250),
                    Mode = ResizeMode.Crop,
                    Position = AnchorPositionMode.Center
                }));

                var encoder = new WebpEncoder { Quality = 80 };
                await image.SaveAsync(filePath, encoder);
            }
        }

        private async Task<Result> ValidateImageDimensionsAsync(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            var imageInfo = await Image.IdentifyAsync(stream);

            if (imageInfo == null || imageInfo.Width < MinImageDimension || imageInfo.Height < MinImageDimension)
            {
                Log.Warning(Images.InvalidSizeAttempt, $"{imageInfo?.Width}x{imageInfo?.Height}", file.FileName, WebContext?.IpAddress ?? "Unknown");
                return Result.Invalid(ImageM.Errors.ImageTooSmall, ImageM.Errors.ImageTooSmallCode);
            }

            return Result.Success();
        }

        /// <summary>
        /// Validates, resizes to 250x250px (Crop), optimizes, and saves a recipe preview image as WebP.
        /// </summary>
        public async Task<Result<string>> SavePostPreviewAsync(IFormFile file, string slug)
        {
            var dimensionResult = await ValidateImageDimensionsAsync(file);
            if (!dimensionResult.IsSuccess)
            {
                return Invalid<string>(dimensionResult.Message!, dimensionResult.ErrorCode);
            }

            var uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 6);
            var safeSlug = string.IsNullOrWhiteSpace(slug) ? "recipe-preview" : slug.ToLowerInvariant();
            var fileName = $"{safeSlug}-{uniqueSuffix}.webp";

            var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "posts");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, fileName);

            await ProcessAndSaveImageAsync(file, filePath);

            Log.Information(Images.SavedSuccessfully, fileName, slug);

            return Success<string>($"/images/posts/{fileName}");
        }
    }
}
