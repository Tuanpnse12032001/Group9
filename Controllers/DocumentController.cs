using Group9.Data;
using Group9.DTOs;
using Group9.Models;
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
        private readonly string _uploadsFolder;

        public DocumentController(AppDbContext context)
        {
            _context = context;
            _uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(_uploadsFolder))
            {
                Directory.CreateDirectory(_uploadsFolder);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDocument([FromForm] UploadDocumentRequest request)
        {
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new { message = "Không nhận được tệp tin hoặc tệp tin rỗng." });
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

            // Generate unique name for saving
            var fileExtension = Path.GetExtension(request.File.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var storagePath = Path.Combine(_uploadsFolder, uniqueFileName);

            // Save file physically
            using (var stream = new FileStream(storagePath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream);
            }

            var document = new Document
            {
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                FileName = request.File.FileName,
                StoragePath = storagePath,
                FileSize = request.File.Length,
                ContentType = request.File.ContentType,
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

        [HttpGet]
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

        [HttpGet("{id}")]
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

        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
            {
                return NotFound(new { message = "Không tìm thấy tài liệu." });
            }

            if (!System.IO.File.Exists(document.StoragePath))
            {
                return NotFound(new { message = "Tệp tin vật lý không tồn tại trên hệ thống." });
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(document.StoragePath);
            return File(fileBytes, document.ContentType, document.FileName);
        }

        [HttpPut("{id}")]
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

        [HttpDelete("{id}")]
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

            // Remove physical file first
            if (System.IO.File.Exists(document.StoragePath))
            {
                try
                {
                    System.IO.File.Delete(document.StoragePath);
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
