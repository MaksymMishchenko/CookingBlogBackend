using PostApiService.Infrastructure.Constants;
using PostApiService.Infrastructure.Services;
using PostApiService.Interfaces;
using PostApiService.Models.Dto.Response;

namespace PostApiService.Services.Fakes
{
    public class FakeFileStorageService : AdminBaseService, IFileStorageService
    {
        public FakeFileStorageService(IWebContext webContext) : base(webContext) { }

        public Task<Result> DeleteByUrlAsync(string url)
        {
            return Task.FromResult(Result.Success());
        }

        public Task<Result<string>> SavePostPreviewAsync(IFormFile file, string slug)
        {
            var suffix = Guid.NewGuid().ToString("N")[..StorageConstants.ImageProcessing.PublicIdSuffixLength];
            var safeSlug = string.IsNullOrWhiteSpace(slug)
                ? StorageConstants.Folders.DefaultPreviewSlug
                : slug.ToLowerInvariant();
            var url = $"https://fake-storage.local/{StorageConstants.Folders.Posts}/{safeSlug}-{suffix}.{StorageConstants.ImageProcessing.WebpFormat}";

            return Task.FromResult(Result<string>.Success(url));
        }

        public Task<Result<PostImageUploadResponseDto>> UploadContentImageAsync(IFormFile file, string? alt)
        {
            var accessError = ValidateAdminOrContributor<PostImageUploadResponseDto>();
            if (accessError != null) return Task.FromResult(accessError);

            var imageId = Guid.NewGuid();
            var suffix = Guid.NewGuid().ToString("N")[..StorageConstants.ImageProcessing.PublicIdSuffixLength];
            var fakeUrl = $"https://fake-storage.local/{StorageConstants.Folders.PostContent}/{StorageConstants.Folders.ContentImagePrefix}-{imageId}-{suffix}.{StorageConstants.ImageProcessing.WebpFormat}";

            var responseDto = new PostImageUploadResponseDto(
                Id: imageId,
                Url: fakeUrl,
                Alt: alt ?? string.Empty
            );

            return Task.FromResult(Result<PostImageUploadResponseDto>.Success(responseDto));
        }
    }
}