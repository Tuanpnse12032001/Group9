using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace Group9.Services
{
    public class CloudinaryStorageService
    {
        private readonly Cloudinary _cloudinary;
        private readonly string _folder;

        public CloudinaryStorageService(IConfiguration configuration)
        {
            var cloudName = configuration["Cloudinary:CloudName"]
                ?? throw new Exception("Cloudinary:CloudName chưa được cấu hình.");

            var apiKey = configuration["Cloudinary:ApiKey"]
                ?? throw new Exception("Cloudinary:ApiKey chưa được cấu hình.");

            var apiSecret = configuration["Cloudinary:ApiSecret"]
                ?? throw new Exception("Cloudinary:ApiSecret chưa được cấu hình.");

            _folder = configuration["Cloudinary:Folder"] ?? "group9_uploads";

            var account = new Account(cloudName, apiKey, apiSecret);

            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }

        public async Task<ImageUploadResult> UploadImageAsync(
            Stream fileStream,
            string fileName,
            int userId)
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                Folder = $"{_folder}/users/{userId}",
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false
            };

            return await _cloudinary.UploadAsync(uploadParams);
        }

        public async Task<RawUploadResult> UploadRawAsync(
            Stream fileStream,
            string fileName,
            int userId)
        {
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                Folder = $"{_folder}/users/{userId}",
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false
            };

            return await _cloudinary.UploadAsync(uploadParams);
        }

        public async Task<DeletionResult> DeleteFileAsync(string publicId, string resourceType = "raw")
        {
            var deletionParams = new DeletionParams(publicId)
            {
                ResourceType = resourceType == "image" ? ResourceType.Image : ResourceType.Raw
            };

            return await _cloudinary.DestroyAsync(deletionParams);
        }
    }
}