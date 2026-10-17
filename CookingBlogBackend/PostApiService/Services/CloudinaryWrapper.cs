using CloudinaryDotNet.Actions;
using PostApiService.Interfaces;

namespace PostApiService.Services
{
    public class CloudinaryWrapper : ICloudinaryWrapper
    {
        private readonly CloudinaryDotNet.Cloudinary _cloudinary;

        public CloudinaryWrapper(CloudinaryDotNet.Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        public async Task<ImageUploadResult> UploadAsync(ImageUploadParams parameters)
        {
            return await _cloudinary.UploadAsync(parameters);
        }
    }
}
