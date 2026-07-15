using Group9.Data;
using Group9.DTOs;
using Group9.Models;
using Group9.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Group9.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly CloudinaryStorageService _cloudinaryStorageService;

        public DocumentController(AppDbContext context, CloudinaryStorageService cloudinaryStorageService)
        {
            _context = context;
            _cloudinaryStorageService = cloudinaryStorageService;
        }

        [HttpPost("create-document")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDocument([FromForm] UploadDocumentRequest request)
        {
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new { message = "Không nhận được tệp tin hoặc tệp tin rỗng." });
            }

            var maxSize = 20 * 1024 * 1024; // 20MB
            if (request.File.Length > maxSize)
            {
                return BadRequest(new { message = "File không được vượt quá 20MB." });
            }

            var originalFileName = Path.GetFileName(request.File.FileName);
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            var allowedExtensions = new[]
            {
                ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".txt", ".csv", ".xls", ".xlsx"
            };

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "Chỉ hỗ trợ JPG, PNG, GIF, WEBP, PDF, TXT, CSV, XLS, XLSX." });
            }

            var subjectExists = await _context.Subjects.AnyAsync(s => s.Id == request.SubjectId);
            if (!subjectExists)
            {
                return BadRequest(new { message = "Môn học không tồn tại." });
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var contentType = string.IsNullOrWhiteSpace(request.File.ContentType)
                ? "application/octet-stream"
                : request.File.ContentType;

            string publicId = string.Empty;
            string secureUrl = string.Empty;

            try
            {
                using var stream = request.File.OpenReadStream();

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

                    publicId = result.PublicId;
                    secureUrl = result.SecureUrl?.ToString() ?? string.Empty;
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

                    publicId = result.PublicId;
                    secureUrl = result.SecureUrl?.ToString() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Upload file lên Cloud Storage thất bại.",
                    error = ex.Message
                });
            }

            var document = new Document
            {
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                FileName = originalFileName,
                StoragePath = secureUrl,
                PublicId = publicId,
                FileSize = request.File.Length,
                ContentType = contentType,
                SubjectId = request.SubjectId,
                UploadedByUserId = userId.Value,
                UploadedAt = DateTime.UtcNow
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            // Load relations to return populated DTO
            var createdDoc = await _context.Documents
                .Include(d => d.Subject)
                .Include(d => d.UploadedByUser)
                .FirstAsync(d => d.Id == document.Id);

            return CreatedAtAction(nameof(GetDocumentDetails), new { id = createdDoc.Id }, MapToDocumentResponse(createdDoc));
        }

        [HttpGet("filter-document")]
        public async Task<IActionResult> GetDocuments([FromQuery] string? search, [FromQuery] int? subjectId)
        {
            var query = _context.Documents
                .Include(d => d.Subject)
                .Include(d => d.UploadedByUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var cleanSearch = search.Trim().ToLower();
                query = query.Where(d => d.Title.ToLower().Contains(cleanSearch) 
                                      || (d.Description != null && d.Description.ToLower().Contains(cleanSearch))
                                      || (d.Subject != null && (d.Subject.SubjectName.ToLower().Contains(cleanSearch) 
                                                                || d.Subject.SubjectCode.ToLower().Contains(cleanSearch))));
            }

            if (subjectId.HasValue)
            {
                query = query.Where(d => d.SubjectId == subjectId.Value);
            }

            var documents = await query
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            var responseList = documents.Select(MapToDocumentResponse).ToList();

            if (responseList.Count == 0)
            {
                return NotFound(new { message = "Không tìm thấy tài liệu." });
            }

            return Ok(responseList);
        }

        [HttpGet("{id}/get-document-by-id")]
        public async Task<IActionResult> GetDocumentDetails(int id)
        {
            var document = await _context.Documents
                .Include(d => d.Subject)
                .Include(d => d.UploadedByUser)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
            {
                return NotFound(new { message = "Không tìm thấy tài liệu." });
            }

            return Ok(MapToDocumentResponse(document));
        }

        [HttpGet("{id}/download-document")]
        public async Task<IActionResult> DownloadDocument(int id, [FromServices] System.Net.Http.IHttpClientFactory httpClientFactory)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
            {
                return NotFound(new { message = "Không tìm thấy tài liệu." });
            }

            if (string.IsNullOrEmpty(document.StoragePath))
            {
                return NotFound(new { message = "Đường dẫn tải tài liệu không hợp lệ." });
            }

            // Handle local file schema for testing / development
            if (document.StoragePath.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var uri = new Uri(document.StoragePath);
                    var localPath = uri.LocalPath;
                    if (System.IO.File.Exists(localPath))
                    {
                        var fileStream = System.IO.File.OpenRead(localPath);
                        return File(fileStream, document.ContentType, document.FileName);
                    }
                    else
                    {
                        return NotFound(new { message = $"Không tìm thấy file cục bộ tại đường dẫn: {localPath}" });
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "Lỗi khi mở file cục bộ.", error = ex.Message });
                }
            }
            else if (!document.StoragePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                     !document.StoragePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                     System.IO.Path.IsPathRooted(document.StoragePath))
            {
                try
                {
                    if (System.IO.File.Exists(document.StoragePath))
                    {
                        var fileStream = System.IO.File.OpenRead(document.StoragePath);
                        return File(fileStream, document.ContentType, document.FileName);
                    }
                    else
                    {
                        return NotFound(new { message = $"Không tìm thấy file cục bộ tại đường dẫn: {document.StoragePath}" });
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "Lỗi khi mở file cục bộ.", error = ex.Message });
                }
            }

            // Handle remote HTTP/HTTPS file
            try
            {
                var client = httpClientFactory.CreateClient();
                var response = await client.GetAsync(document.StoragePath);
                
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new { message = "Không thể tải tài liệu từ bộ lưu trữ đám mây." });
                }

                var fileStream = await response.Content.ReadAsStreamAsync();
                return File(fileStream, document.ContentType, document.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tải tài liệu từ máy chủ đám mây.", error = ex.Message });
            }
        }

        [HttpPut("{id}/edit-document")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateDocument(int id, UpdateDocumentRequest request)
        {
            var document = await _context.Documents
                .Include(d => d.Subject)
                .Include(d => d.UploadedByUser)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
            {
                return NotFound(new { message = "Không tìm thấy tài liệu." });
            }

            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            // Authorization: Only owner or admin can update
            if (userId != document.UploadedByUserId && userRole != "Admin")
            {
                return Forbid();
            }

            var subjectExists = await _context.Subjects.AnyAsync(s => s.Id == request.SubjectId);
            if (!subjectExists)
            {
                return BadRequest(new { message = "Môn học không tồn tại." });
            }

            document.Title = request.Title.Trim();
            document.Description = request.Description?.Trim();
            document.SubjectId = request.SubjectId;

            _context.Documents.Update(document);
            await _context.SaveChangesAsync();

            // Reload populated object
            var updatedDoc = await _context.Documents
                .Include(d => d.Subject)
                .Include(d => d.UploadedByUser)
                .FirstAsync(d => d.Id == id);

            return Ok(MapToDocumentResponse(updatedDoc));
        }

        [HttpDelete("{id}/delete-document")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
            {
                return NotFound(new { message = "Không tìm thấy tài liệu." });
            }

            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            // Authorization: Only owner or admin can delete
            if (userId != document.UploadedByUserId && userRole != "Admin")
            {
                return Forbid();
            }

            // Remove file from Cloudinary first
            if (!string.IsNullOrEmpty(document.PublicId))
            {
                try
                {
                    var resourceType = document.ContentType.StartsWith("image/") ? "image" : "raw";
                    await _cloudinaryStorageService.DeleteFileAsync(document.PublicId, resourceType);
                }
                catch (Exception)
                {
                    // Log error or continue to remove metadata from DB
                }
            }

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa tài liệu thành công." });
        }

        private int? GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdValue)) return null;
            if (!int.TryParse(userIdValue, out var userId)) return null;
            return userId;
        }

        private string? GetCurrentUserRole()
        {
            return User.FindFirstValue(ClaimTypes.Role);
        }

        private static DocumentResponse MapToDocumentResponse(Document doc)
        {
            var firstName = doc.UploadedByUser?.FirstName ?? string.Empty;
            var lastName = doc.UploadedByUser?.LastName ?? string.Empty;
            var fullName = $"{firstName} {lastName}".Trim();

            return new DocumentResponse
            {
                Id = doc.Id,
                Title = doc.Title,
                Description = doc.Description,
                FileName = doc.FileName,
                FileSize = doc.FileSize,
                ContentType = doc.ContentType,
                UploadedAt = doc.UploadedAt,
                SubjectId = doc.SubjectId,
                SubjectCode = doc.Subject?.SubjectCode ?? string.Empty,
                SubjectName = doc.Subject?.SubjectName ?? string.Empty,
                UploadedByUserId = doc.UploadedByUserId,
                UploadedByUserName = string.IsNullOrEmpty(fullName) ? (doc.UploadedByUser?.Email ?? string.Empty) : fullName
            };
        }
    }
}
