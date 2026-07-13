using Group9.Data;
using Group9.DTOs;
using Group9.Models;
using Group9.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Group9.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "User,Admin,ChatbotService")]
    public class FilesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly CloudinaryStorageService _cloudinaryStorageService;

        public FilesController(
            AppDbContext context,
            CloudinaryStorageService cloudinaryStorageService)
        {
            _context = context;
            _cloudinaryStorageService = cloudinaryStorageService;
        }

        [HttpPost("upload")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "Bạn chưa đăng nhập."
                });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    message = "File không hợp lệ."
                });
            }

            var maxSize = 20 * 1024 * 1024;

            if (file.Length > maxSize)
            {
                return BadRequest(new
                {
                    message = "File không được vượt quá 20MB."
                });
            }

            var originalFileName = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();

            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".gif",
                ".webp",
                ".pdf",
                ".txt",
                ".csv",
                ".xls",
                ".xlsx"
            };

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new
                {
                    message = "Chỉ hỗ trợ JPG, PNG, GIF, WEBP, PDF, TXT, CSV, XLS, XLSX."
                });
            }

            var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType;

            var allowedContentTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "image/gif",
                "image/webp",

                "application/pdf",
                "text/plain",

                "text/csv",
                "application/csv",

                "application/vnd.ms-excel",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                "application/octet-stream"
            };

            if (!allowedContentTypes.Contains(contentType))
            {
                return BadRequest(new
                {
                    message = $"Loại file không được hỗ trợ. Content-Type hiện tại: {contentType}"
                });
            }

            var cloudFile = new CloudFile
            {
                OriginalFileName = originalFileName,
                PublicId = "",
                SecureUrl = "",
                ResourceType = "",
                ContentType = contentType,
                Size = file.Length,
                Status = "Uploading",
                UserId = userId.Value,
                CreatedAt = DateTime.UtcNow
            };

            _context.CloudFiles.Add(cloudFile);
            await _context.SaveChangesAsync();

            try
            {
                using var stream = file.OpenReadStream();

                if (contentType.StartsWith("image/"))
                {
                    var result = await _cloudinaryStorageService.UploadImageAsync(
                        stream,
                        originalFileName,
                        userId.Value
                    );

                    if (result.Error != null)
                    {
                        throw new Exception(result.Error.Message);
                    }

                    cloudFile.PublicId = result.PublicId;
                    cloudFile.SecureUrl = result.SecureUrl?.ToString() ?? "";
                    cloudFile.ResourceType = "image";
                }
                else
                {
                    var result = await _cloudinaryStorageService.UploadRawAsync(
                        stream,
                        originalFileName,
                        userId.Value
                    );

                    if (result.Error != null)
                    {
                        throw new Exception(result.Error.Message);
                    }

                    cloudFile.PublicId = result.PublicId;
                    cloudFile.SecureUrl = result.SecureUrl?.ToString() ?? "";
                    cloudFile.ResourceType = "raw";
                }

                cloudFile.Status = "Uploaded";
                cloudFile.UploadedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(MapToResponse(cloudFile));
            }
            catch (Exception ex)
            {
                cloudFile.Status = "Failed";
                await _context.SaveChangesAsync();

                return StatusCode(500, new
                {
                    message = "Upload file thất bại.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("my-files")]
        public async Task<IActionResult> GetMyFiles()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "Bạn chưa đăng nhập."
                });
            }

            var files = await _context.CloudFiles
                .Where(f => f.UserId == userId.Value)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new CloudFileResponse
                {
                    Id = f.Id,
                    OriginalFileName = f.OriginalFileName,
                    PublicId = f.PublicId,
                    SecureUrl = f.SecureUrl,
                    ResourceType = f.ResourceType,
                    ContentType = f.ContentType,
                    Size = f.Size,
                    Status = f.Status,
                    CreatedAt = f.CreatedAt,
                    UploadedAt = f.UploadedAt
                })
                .ToListAsync();

            return Ok(files);
        }

        [HttpGet("{id}/status")]
        public async Task<IActionResult> GetUploadStatus(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            var file = await _context.CloudFiles
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId.Value);

            if (file == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy file."
                });
            }

            return Ok(new
            {
                file.Id,
                file.OriginalFileName,
                file.Status,
                file.CreatedAt,
                file.UploadedAt
            });
        }

        [HttpGet("{id}/preview-url")]
        public async Task<IActionResult> GetPreviewUrl(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            var file = await _context.CloudFiles
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId.Value);

            if (file == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy file."
                });
            }

            if (file.Status != "Uploaded")
            {
                return BadRequest(new
                {
                    message = "File chưa upload xong."
                });
            }

            return Ok(new PreviewFileResponse
            {
                Id = file.Id,
                FileName = file.OriginalFileName,
                ContentType = file.ContentType,
                PreviewUrl = file.SecureUrl
            });
        }

        private int? GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdValue))
            {
                return null;
            }

            if (!int.TryParse(userIdValue, out var userId))
            {
                return null;
            }

            return userId;
        }

        private static CloudFileResponse MapToResponse(CloudFile file)
        {
            return new CloudFileResponse
            {
                Id = file.Id,
                OriginalFileName = file.OriginalFileName,
                PublicId = file.PublicId,
                SecureUrl = file.SecureUrl,
                ResourceType = file.ResourceType,
                ContentType = file.ContentType,
                Size = file.Size,
                Status = file.Status,
                CreatedAt = file.CreatedAt,
                UploadedAt = file.UploadedAt
            };
        }
    }
}