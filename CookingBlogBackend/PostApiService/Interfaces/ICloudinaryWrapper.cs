using CloudinaryDotNet.Actions;

namespace PostApiService.Interfaces
{
    public interface ICloudinaryWrapper
    {
        Task<ImageUploadResult> UploadAsync(ImageUploadParams parameters);
        Task<DeletionResult> DestroyAsync(DeletionParams parameters);        
        Task<DelResResult> DeleteResourcesAsync(DelResParams parameters);
    }
}