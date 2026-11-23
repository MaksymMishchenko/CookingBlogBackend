using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using PostApiService.Infrastructure.Constants;
using PostApiService.Infrastructure.Services;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Response;
using PostApiService.Repositories;
using System.Text.RegularExpressions;

namespace PostApiService.Services
{
    public class FileStorageService : AdminBaseService, IFileStorageService
    {
        private readonly ICloudinaryWrapper _cloudinaryWrapper;
        private readonly IPostImageRepository _postImageRepository;

        public FileStorageService(
            ICloudinaryWrapper cloudinaryWrapper,
            IPostImageRepository postImageRepository,
            IWebContext webContext) : base(webContext)
        {
            _cloudinaryWrapper = cloudinaryWrapper;
            _postImageRepository = postImageRepository;
        }

        /// <summary>
        /// Uploads the recipe preview image to Cloudinary, applying resizing, cropping, and WebP optimization on the fly.
        /// </summary>
        public async Task<Result<string>> SavePostPreviewAsync(IFormFile file, string slug)
        {
            var uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, StorageConstants.ImageProcessing.PublicIdSuffixLength);
            var safeSlug = string.IsNullOrWhiteSpace(slug) ? StorageConstants.Folders.DefaultPreviewSlug : slug.ToLowerInvariant();
            var publicId = $"{safeSlug}-{uniqueSuffix}";

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                PublicId = publicId,
                Folder = StorageConstants.Folders.Posts,
                Transformation = new Transformation()
                    .Width(StorageConstants.ImageProcessing.PreviewWidth)
                    .Height(StorageConstants.ImageProcessing.PreviewHeight)
                    .FetchFormat(StorageConstants.ImageProcessing.WebpFormat)
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

        public async Task<Result<PostImageUploadResponseDto>> UploadContentImageAsync(
            IFormFile file,
            string? alt,
            CancellationToken ct = default)
        {
            var accessError = ValidateAdminOrContributor<PostImageUploadResponseDto>();
            if (accessError != null) return accessError;

            var uniqueToken = Guid.NewGuid().ToString("N")[..12];
            var publicId = $"{StorageConstants.Folders.PostContent}/{StorageConstants.Folders.ContentImagePrefix}-{uniqueToken}";

            ImageUploadResult? uploadResult = null;
            int maxRetries = 3;
            int currentRetry = 0;

            while (currentRetry < maxRetries)
            {
                try
                {
                    await using var stream = file.OpenReadStream();
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(file.FileName, stream),
                        PublicId = publicId,
                        Transformation = new Transformation().FetchFormat(StorageConstants.ImageProcessing.WebpFormat)
                    };

                    uploadResult = await _cloudinaryWrapper.UploadAsync(uploadParams);

                    if (uploadResult?.Error == null)
                    {
                        break;
                    }

                    Log.Warning(Images.UploadAttemptFailed, currentRetry + 1, uploadResult.Error.Message);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, LogMessages.Images.UploadNetworkException, currentRetry + 1);
                }

                currentRetry++;
                if (currentRetry < maxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(currentRetry));
                }
            }

            if (uploadResult == null || uploadResult.Error != null)
            {
                Log.Error(Images.UploadRetriesExhausted, maxRetries);
                return BadGateway<PostImageUploadResponseDto>(ImageM.Errors.UploadFailed, ImageM.Errors.UploadFailedCode);
            }

            var postImage = new PostImage
            {
                PostId = null,
                Url = uploadResult.SecureUrl.ToString(),
                PublicId = publicId,
                Alt = alt ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            await _postImageRepository.AddAsync(postImage, ct);
            await _postImageRepository.SaveChangesAsync(ct);

            return Success(new PostImageUploadResponseDto(postImage.Id, postImage.Url, postImage.Alt));
        }

        public async Task<Result> DeleteByUrlAsync(string url)
        {
            var publicId = ExtractPublicId(url);

            if (publicId is null)
            {
                Log.Warning(Images.CannotExtractPublicId, url);
                return Error<bool>(ImageM.Errors.DeleteFailed, ImageM.Errors.DeleteFailedCode);
            }

            var result = await _cloudinaryWrapper.DestroyAsync(new DeletionParams(publicId));

            if (result.Error != null)
            {
                Log.Error(Images.DeleteFailed, publicId, result.Error.Message);
                return Error<bool>(ImageM.Errors.DeleteFailed, ImageM.Errors.DeleteFailedCode);
            }

            Log.Information(Images.DeletedSuccessfully, publicId);
            return Success(true);
        }        

        private static string? ExtractPublicId(string url)
        {
            var marker = StorageConstants.ImageProcessing.CloudinaryUploadSegment;
            var idx = url.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0) return null;

            var path = url[(idx + marker.Length)..];
            path = Regex.Replace(path, @"^v\d+/", "");

            var dot = path.LastIndexOf('.');
            return dot > 0 ? path[..dot] : path;
        }        
    }
}