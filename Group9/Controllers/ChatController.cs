using Group9.Data;
using Group9.DTOs;
using Group9.Models;
using Group9.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Group9.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "User,Admin,ChatbotService")]
    public class ChatController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IChatbotService _chatbotService;

        public ChatController(AppDbContext context, IChatbotService chatbotService)
        {
            _context = context;
            _chatbotService = chatbotService;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // GET /api/chat/sessions?keyword=
        // Lấy danh sách session của user, hỗ trợ tìm kiếm theo từ khóa.
        // ─────────────────────────────────────────────────────────────────────────
        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions([FromQuery] string? keyword = null)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var query = _context.ChatSessions
                .Include(s => s.Document)
                .Where(s => s.UserId == userId.Value);

            // Lọc theo từ khóa trong tiêu đề session
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(s => s.Title.ToLower().Contains(kw));
            }

            var sessions = await query
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new ChatSessionResponse
                {
                    Id = s.Id,
                    Title = s.Title,
                    CreatedAt = s.CreatedAt,
                    DocumentId = s.DocumentId,
                    DocumentTitle = s.Document != null ? s.Document.Title : null
                })
                .ToListAsync();

            return Ok(sessions);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // GET /api/chat/sessions/{sessionId}/messages
        // Lấy lịch sử tin nhắn của một session.
        // ─────────────────────────────────────────────────────────────────────────
        [HttpGet("sessions/{sessionId}/messages")]
        public async Task<IActionResult> GetMessages(int sessionId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var session = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId.Value);

            if (session == null)
                return NotFound(new { message = "Không tìm thấy phiên trò chuyện hoặc bạn không có quyền truy cập." });

            var messages = await _context.ChatMessages
                .Where(m => m.ChatSessionId == sessionId)
                .OrderBy(m => m.SentAt)
                .Select(m => new ChatMessageResponse
                {
                    Id = m.Id,
                    Sender = m.Sender,
                    Content = m.Content,
                    SentAt = m.SentAt
                })
                .ToListAsync();

            return Ok(messages);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // POST /api/chat/messages
        // Gửi tin nhắn. Nếu SessionId == null → tự động tạo session mới với
        // tiêu đề sinh từ nội dung tin nhắn đầu tiên.
        // ─────────────────────────────────────────────────────────────────────────
        [HttpPost("messages")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
                return BadRequest(new { message = "Nội dung tin nhắn không được để trống." });

            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            ChatSession? session = null;
            bool isNewSession = false;
            Document? linkedDocument = null;

            if (request.SessionId.HasValue && request.SessionId.Value > 0)
            {
                // ── Dùng session đã có ──────────────────────────────────────────
                session = await _context.ChatSessions
                    .Include(s => s.Document)
                        .ThenInclude(d => d!.Subject)
                    .FirstOrDefaultAsync(s => s.Id == request.SessionId.Value && s.UserId == userId.Value);

                if (session == null)
                    return NotFound(new { message = "Không tìm thấy phiên trò chuyện hoặc bạn không có quyền truy cập." });

                linkedDocument = session.Document;
            }
            else
            {
                // ── Tự động tạo session mới ────────────────────────────────────
                isNewSession = true;

                if (request.DocumentId.HasValue && request.DocumentId.Value > 0)
                {
                    linkedDocument = await _context.Documents
                        .Include(d => d.Subject)
                        .FirstOrDefaultAsync(d => d.Id == request.DocumentId.Value);

                    if (linkedDocument == null)
                        return BadRequest(new { message = "Tài liệu liên kết không tồn tại." });
                }

                // Sinh tiêu đề từ nội dung tin nhắn đầu tiên
                var autoTitle = linkedDocument != null
                    ? $"Hỏi đáp: {linkedDocument.Title}"
                    : GenerateSessionTitle(request.Content);

                session = new ChatSession
                {
                    Title = autoTitle,
                    UserId = userId.Value,
                    DocumentId = linkedDocument?.Id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ChatSessions.Add(session);
                await _context.SaveChangesAsync(); // cần Id trước
            }

            // Guard: session luôn được gán tại đây (null-forgiving vì đã xử lý ở trên)
            if (session == null) return StatusCode(500, new { message = "Lỗi khởi tạo session." });

            // ── Lưu tin nhắn của user ─────────────────────────────────────────
            var userMsg = new ChatMessage
            {
                ChatSessionId = session.Id,
                Sender = "User",
                Content = request.Content.Trim(),
                SentAt = DateTime.UtcNow
            };
            _context.ChatMessages.Add(userMsg);

            // ── Lấy lịch sử (tối đa 20 tin nhắn gần nhất) ───────────────────
            var history = await _context.ChatMessages
                .Where(m => m.ChatSessionId == session.Id)
                .OrderByDescending(m => m.SentAt)
                .Take(20)
                .ToListAsync();
            history.Reverse();

            // ── Gọi Gemini AI ─────────────────────────────────────────────────
            var botReply = await _chatbotService.GetResponseAsync(
                userMsg.Content, history, linkedDocument);

            // ── Lưu tin nhắn của bot ──────────────────────────────────────────
            var botMsg = new ChatMessage
            {
                ChatSessionId = session.Id,
                Sender = "Bot",
                Content = botReply,
                SentAt = DateTime.UtcNow
            };
            _context.ChatMessages.Add(botMsg);
            await _context.SaveChangesAsync();

            return Ok(new SendMessageResponse
            {
                SessionId = session.Id,
                SessionTitle = session.Title,
                IsNewSession = isNewSession,
                BotMessage = new ChatMessageResponse
                {
                    Id = botMsg.Id,
                    Sender = botMsg.Sender,
                    Content = botMsg.Content,
                    SentAt = botMsg.SentAt
                }
            });
        }

        // ─────────────────────────────────────────────────────────────────────────
        // DELETE /api/chat/sessions/{sessionId}
        // Xóa phiên trò chuyện.
        // ─────────────────────────────────────────────────────────────────────────
        [HttpDelete("sessions/{sessionId}")]
        public async Task<IActionResult> DeleteSession(int sessionId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var session = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId.Value);

            if (session == null)
                return NotFound(new { message = "Không tìm thấy phiên trò chuyện hoặc bạn không có quyền truy cập." });

            _context.ChatSessions.Remove(session);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa phiên trò chuyện thành công." });
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Sinh tiêu đề session từ nội dung tin nhắn đầu tiên.
        /// Lấy tối đa 60 ký tự đầu, cắt theo từ để tiêu đề tự nhiên hơn.
        /// </summary>
        private static string GenerateSessionTitle(string content)
        {
            var text = content.Trim();
            if (text.Length <= 60) return text;

            // Cắt tại khoảng trắng gần nhất trước ký tự thứ 60
            var cutoff = text.LastIndexOf(' ', 60);
            return cutoff > 0
                ? text[..cutoff] + "…"
                : text[..60] + "…";
        }

        private int? GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdValue)) return null;
            if (!int.TryParse(userIdValue, out var userId)) return null;
            return userId;
        }
    }
}
