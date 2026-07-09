using Group9.Data;
using Group9.DTOs;
using Group9.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Group9.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubjectController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SubjectController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllSubjects()
        {
            var subjects = await _context.Subjects
                .Select(s => new SubjectResponse
                {
                    Id = s.Id,
                    SubjectCode = s.SubjectCode,
                    SubjectName = s.SubjectName
                })
                .ToListAsync();

            return Ok(subjects);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateSubject(CreateSubjectRequest request)
        {
            var code = request.SubjectCode.Trim().ToUpper();

            if (await _context.Subjects.AnyAsync(s => s.SubjectCode.ToUpper() == code))
            {
                return BadRequest(new { message = "Mã môn học đã tồn tại." });
            }

            var subject = new Subject
            {
                SubjectCode = code,
                SubjectName = request.SubjectName.Trim()
            };

            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();

            return Ok(new SubjectResponse
            {
                Id = subject.Id,
                SubjectCode = subject.SubjectCode,
                SubjectName = subject.SubjectName
            });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
            {
                return NotFound(new { message = "Không tìm thấy môn học." });
            }

            // Check if there are documents under this subject
            var hasDocuments = await _context.Documents.AnyAsync(d => d.SubjectId == id);
            if (hasDocuments)
            {
                return BadRequest(new { message = "Không thể xóa môn học này vì đã có tài liệu liên kết." });
            }

            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa môn học thành công." });
        }
    }
}
