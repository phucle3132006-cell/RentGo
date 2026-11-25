using System.Buffers.Binary;
using CarRental.BLL.Interfaces;
using CarRental.DAL.Interfaces;
using CarRental.Models.DTOs.VehicleImages;
using CarRental.Models.Entities;
using Microsoft.Extensions.Logging;

namespace CarRental.BLL.Services
{
    public class VehicleImageService : IVehicleImageService
    {
        private const int MaxFileBytes = 5 * 1024 * 1024;
        private const string UrlPrefix = "/uploads/vehicles/";

        private readonly IVehicleImageRepository _repository;
        private readonly string _uploadDirectory;
        private readonly ILogger<VehicleImageService> _logger;

        public VehicleImageService(
            IVehicleImageRepository repository,
            string webRootPath,
            ILogger<VehicleImageService> logger)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(webRootPath);
            if (!Path.IsPathFullyQualified(webRootPath))
                throw new ArgumentException("Web root must be an absolute path.", nameof(webRootPath));

            _repository = repository;
            _uploadDirectory = Path.Combine(Path.GetFullPath(webRootPath), "uploads", "vehicles");
            _logger = logger;
        }

        public async Task<List<VehicleImageResponse>> GetByVehicleIdAsync(long vehicleId)
        {
            if (!await _repository.VehicleExistsAsync(vehicleId))
                throw new KeyNotFoundException("Xe không tồn tại.");

            var images = await _repository.GetByVehicleIdAsync(vehicleId);
            return images.Select(MapToResponse).ToList();
        }

        public async Task<VehicleImageResponse> UploadAsync(
            long vehicleId,
            Stream fileStream,
            string fileName,
            long fileLength,
            bool isPrimary)
        {
            ArgumentNullException.ThrowIfNull(fileStream);
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
            if (fileLength <= 0 || fileLength > MaxFileBytes)
                throw new ArgumentException("Dung lượng ảnh phải lớn hơn 0 và tối đa 5 MB.", nameof(fileLength));

            if (!await _repository.VehicleExistsAsync(vehicleId))
                throw new KeyNotFoundException("Xe không tồn tại.");

            // Read one extra byte to reject streams that exceed their declared length.
            var buffer = new byte[(int)fileLength + 1];
            int bytesRead = await fileStream.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false);
            if (bytesRead != fileLength)
                throw new ArgumentException("Dung lượng ảnh không khớp với nội dung file.", nameof(fileLength));

            string extension = GetImageExtension(buffer.AsSpan(0, bytesRead));
            string storedName = $"{Guid.NewGuid():N}{extension}";
            string filePath = Path.Combine(_uploadDirectory, storedName);
            var image = new VehicleImage
            {
                VehicleId = vehicleId,
                ImageUrl = UrlPrefix + storedName,
                IsPrimary = isPrimary,
                CreatedAt = DateTime.UtcNow
            };

            Directory.CreateDirectory(_uploadDirectory);
            bool fileCreated = false;
            bool readyToCommit = false;
            try
            {
                await using (var output = new FileStream(
                    filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                {
                    fileCreated = true;
                    await output.WriteAsync(buffer.AsMemory(0, bytesRead));
                }

                await _repository.ExecuteForVehicleAsync(vehicleId, async vehicleExists =>
                {
                    if (!vehicleExists)
                        throw new KeyNotFoundException("Xe không tồn tại.");

                    if (isPrimary)
                        await ClearPrimaryAsync(vehicleId);

                    await _repository.AddAsync(image);
                    await _repository.SaveChangesAsync();
                    readyToCommit = true;
                    return true;
                });
            }
            catch
            {
                if (fileCreated && !readyToCommit)
                    TryDeleteFile(filePath);

                // A connection failure during commit can leave its outcome unknown.
                if (readyToCommit)
                    _logger.LogWarning("Could not confirm the upload commit for {ImageUrl}. The file was retained.", image.ImageUrl);

                throw;
            }

            return MapToResponse(image);
        }

        public Task<bool> SetPrimaryAsync(long vehicleId, long imageId)
        {
            return _repository.ExecuteForVehicleAsync(vehicleId, async vehicleExists =>
            {
                var image = vehicleExists ? await _repository.GetByIdAsync(imageId) : null;
                if (image == null || image.VehicleId != vehicleId)
                    return false;

                if (!image.IsPrimary)
                {
                    await ClearPrimaryAsync(vehicleId);
                    image.IsPrimary = true;
                    await _repository.SaveChangesAsync();
                }

                return true;
            });
        }

        public async Task<bool> DeleteAsync(long vehicleId, long imageId)
        {
            var deletedImage = await _repository.ExecuteForVehicleAsync<VehicleImage?>(vehicleId, async vehicleExists =>
            {
                var image = vehicleExists ? await _repository.GetByIdAsync(imageId) : null;
                if (image == null || image.VehicleId != vehicleId)
                    return null;

                _repository.Delete(image);
                await _repository.SaveChangesAsync();
                return image;
            });

            if (deletedImage == null)
                return false;

            // Delete only canonical paths generated by this service after the DB commit.
            if (deletedImage.ImageUrl.StartsWith(UrlPrefix, StringComparison.Ordinal))
            {
                string storedName = deletedImage.ImageUrl[UrlPrefix.Length..];
                string extension = Path.GetExtension(storedName);
                if (extension is ".jpg" or ".png" or ".webp"
                    && Guid.TryParseExact(Path.GetFileNameWithoutExtension(storedName), "N", out var id)
                    && storedName == $"{id:N}{extension}")
                {
                    TryDeleteFile(Path.Combine(_uploadDirectory, storedName));
                }
            }

            return true;
        }

        private async Task ClearPrimaryAsync(long vehicleId)
        {
            var current = await _repository.GetPrimaryImageAsync(vehicleId);
            if (current == null)
                return;

            current.IsPrimary = false;
            // Run the demotion before inserting or promoting into the filtered unique index.
            await _repository.SaveChangesAsync();
        }

        private void TryDeleteFile(string filePath)
        {
            try
            {
                File.Delete(filePath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Could not remove vehicle image file {FilePath}.", filePath);
            }
        }

        private static string GetImageExtension(ReadOnlySpan<byte> data)
        {
            if (data.Length >= 8
                && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF
                && data[^2] == 0xFF && data[^1] == 0xD9)
            {
                return ".jpg";
            }

            ReadOnlySpan<byte> pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
            if (data.Length >= 45 && data.StartsWith(pngSignature)
                && BinaryPrimitives.ReadUInt32BigEndian(data[8..12]) == 13
                && data[12..16].SequenceEqual("IHDR"u8)
                && BinaryPrimitives.ReadUInt32BigEndian(data[^12..^8]) == 0
                && data[^8..^4].SequenceEqual("IEND"u8))
            {
                return ".png";
            }

            if (data.Length >= 20 && data.StartsWith("RIFF"u8)
                && BinaryPrimitives.ReadUInt32LittleEndian(data[4..8]) == (uint)(data.Length - 8)
                && data[8..12].SequenceEqual("WEBP"u8))
            {
                var firstChunk = data[12..16];
                uint chunkLength = BinaryPrimitives.ReadUInt32LittleEndian(data[16..20]);
                if (chunkLength > 0 && chunkLength + (ulong)(chunkLength % 2) <= (ulong)(data.Length - 20)
                    && (firstChunk.SequenceEqual("VP8 "u8)
                        || firstChunk.SequenceEqual("VP8L"u8)
                        || (firstChunk.SequenceEqual("VP8X"u8) && chunkLength == 10 && data.Length > 30)))
                {
                    return ".webp";
                }
            }

            throw new ArgumentException("Ảnh phải có nội dung JPEG, PNG hoặc WebP hợp lệ.");
        }

        private static VehicleImageResponse MapToResponse(VehicleImage image)
        {
            return new VehicleImageResponse
            {
                Id = image.Id,
                VehicleId = image.VehicleId,
                ImageUrl = image.ImageUrl,
                IsPrimary = image.IsPrimary,
                CreatedAt = image.CreatedAt
            };
        }
    }
}
