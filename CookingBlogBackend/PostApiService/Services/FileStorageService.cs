using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using PostApiService.Interfaces;

namespace PostApiService.Services
{
    public class FileStorageService : BaseService, IFileStorageService
    {
        private readonly ICloudinaryWrapper _cloudinaryWrapper;

        public FileStorageService(ICloudinaryWrapper cloudinaryWrapper)
        {
            _cloudinaryWrapper = cloudinaryWrapper;
        }

        /// <summary>
        /// Uploads the recipe preview image to Cloudinary, applying resizing, cropping, and WebP optimization on the fly.
        /// </summary>
        public async Task<Result<string>> SavePostPreviewAsync(IFormFile file, string slug)
        {
            var uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 6);
            var safeSlug = string.IsNullOrWhiteSpace(slug) ? "recipe-preview" : slug.ToLowerInvariant();
            var publicId = $"{safeSlug}-{uniqueSuffix}";

            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                PublicId = publicId,
                Folder = "posts",
                Transformation = new Transformation()
                    .Width(250)
                    .Height(250)
                    .Crop("fill")
                    .Gravity("center")
                    .FetchFormat("webp")
                    .Quality("80")
            };
            
            var uploadResult = await _cloudinaryWrapper.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                Log.Error(Images.UploadFailed, uploadResult.Error.Message);
                return Error<string>(ImageM.Errors.UploadFailed, ImageM.Errors.UploadFailedCode);
            }

            Log.Information(Images.SavedSuccessfully, publicId, slug);

            return Success<string>(uploadResult.SecureUrl.ToString());
        }
    }
}