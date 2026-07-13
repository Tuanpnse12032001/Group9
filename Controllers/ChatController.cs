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

        [HttpPost("sessions")]
        public async Task<IActionResult> CreateSession(CreateChatSessionRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            string title = "Trò chuyện mới";
            Document? document = null;

            if (request.DocumentId.HasValue)
            {
                document = await _context.Documents.FindAsync(request.DocumentId.Value);
                if (document == null)
                {
                    return BadRequest(new { message = "Tài liệu liên kết không tồn tại." });
                }
                title = $"Hỏi đáp: {document.Title}";
            }

            var session = new ChatSession
            {
                Title = title,
                UserId = userId.Value,
                DocumentId = request.DocumentId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync();

            return Ok(new ChatSessionResponse
            {
                Id = session.Id,
                Title = session.Title,
                CreatedAt = session.CreatedAt,
                DocumentId = session.DocumentId,
                DocumentTitle = document?.Title
            });
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var sessions = await _context.ChatSessions
                .Include(s => s.Document)
                .Where(s => s.UserId == userId.Value)
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

        [HttpGet("sessions/{sessionId}/messages")]
        public async Task<IActionResult> GetMessages(int sessionId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var session = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId.Value);

            if (session == null)
            {
                return NotFound(new { message = "Không tìm thấy phiên trò chuyện hoặc bạn không có quyền truy cập." });
            }

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

        [HttpPost("sessions/{sessionId}/messages")]
        public async Task<IActionResult> SendMessage(int sessionId, SendMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { message = "Nội dung tin nhắn không được để trống." });
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var session = await _context.ChatSessions
                .Include(s => s.Document)
                .ThenInclude(d => d!.Subject)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId.Value);

            if (session == null)
            {
                return NotFound(new { message = "Không tìm thấy phiên trò chuyện hoặc bạn không có quyền truy cập." });
            }

            // 1. Save User's Message
            var userMsg = new ChatMessage
            {
                ChatSessionId = sessionId,
                Sender = "User",
                Content = request.Content.Trim(),
                SentAt = DateTime.UtcNow
            };
            _context.ChatMessages.Add(userMsg);

            // 2. Fetch history (limit to last 20 messages for context)
            var history = await _context.ChatMessages
                .Where(m => m.ChatSessionId == sessionId)
                .OrderByDescending(m => m.SentAt)
                .Take(20)
                .ToListAsync();
            history.Reverse(); // Chronological order

            // 3. Generate response using chatbot service (Mock Service)
            var botReply = await _chatbotService.GetResponseAsync(userMsg.Content, history, session.Document);

            // 4. Save Bot's Message
            var botMsg = new ChatMessage
            {
                ChatSessionId = sessionId,
                Sender = "Bot",
                Content = botReply,
                SentAt = DateTime.UtcNow
            };
            _context.ChatMessages.Add(botMsg);

            await _context.SaveChangesAsync();

            return Ok(new ChatMessageResponse
            {
                Id = botMsg.Id,
                Sender = botMsg.Sender,
                Content = botMsg.Content,
                SentAt = botMsg.SentAt
            });
        }

        [HttpDelete("sessions/{sessionId}")]
        public async Task<IActionResult> DeleteSession(int sessionId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var session = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId.Value);

            if (session == null)
            {
                return NotFound(new { message = "Không tìm thấy phiên trò chuyện hoặc bạn không có quyền truy cập." });
            }

            _context.ChatSessions.Remove(session);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa phiên trò chuyện thành công." });
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
