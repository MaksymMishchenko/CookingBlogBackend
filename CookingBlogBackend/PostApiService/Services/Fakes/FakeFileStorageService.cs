using PostApiService.Interfaces;

namespace PostApiService.Services.Fakes
{
    public class FakeFileStorageService : BaseService, IFileStorageService
    {
        public Task<Result> DeleteByUrlAsync(string url)
        {
            return Task.FromResult(Result.Success());
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
