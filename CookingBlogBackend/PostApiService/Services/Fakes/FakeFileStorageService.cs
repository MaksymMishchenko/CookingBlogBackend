using PostApiService.Infrastructure.Constants;
using PostApiService.Interfaces;

namespace PostApiService.Services.Fakes
{
    public class FakeFileStorageService : BaseService, IFileStorageService
    {
        public Task<Result> DeleteByUrlAsync(string url)
        {
            return Task.FromResult(Result.Success());
        }

        public Task<Result> DeleteByUrlsAsync(IEnumerable<string> urls)
        {
            return Task.FromResult(Result.Success());
        }

        public Task<Result<List<string>>> SavePostContentImagesAsync(
            IReadOnlyList<IFormFile> files, string slug)
        {
            var safeSlug = string.IsNullOrWhiteSpace(slug) ? "post" : slug.ToLowerInvariant();
            
            var urls = files
                .Select((_, i) =>
                {
                    var suffix = Guid.NewGuid().ToString("N")[..6];
                    return $"https://res.cloudinary.com/fake/image/upload/{FileLimits.ContentImagesFolder}/{safeSlug}-{i + 1}-{suffix}.webp";
                })
                .ToList();

            return Task.FromResult(Result<List<string>>.Success(urls));
        }

        public Task<Result<string>> SavePostPreviewAsync(IFormFile file, string slug)
        {
            var suffix = Guid.NewGuid().ToString("N")[..6];
            var safeSlug = string.IsNullOrWhiteSpace(slug) ? "recipe-preview" : slug.ToLowerInvariant();
            var url = $"https://fake-storage.local/posts/{safeSlug}-{suffix}.webp";

            return Task.FromResult(Result<string>.Success(url));
        }
    }
}
