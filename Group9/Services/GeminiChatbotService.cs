using Group9.Models;
using Microsoft.Extensions.Configuration;
using Mscc.GenerativeAI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Group9.Services
{
    public class GeminiChatbotService : IChatbotService
    {
        private readonly string _apiKey;
        private readonly string _modelName;

        public GeminiChatbotService(IConfiguration configuration)
        {
            _apiKey = configuration["Gemini:ApiKey"]
                      ?? throw new InvalidOperationException("Gemini:ApiKey chưa được cấu hình trong appsettings.json.");
            _modelName = configuration["Gemini:Model"] ?? "gemini-2.0-flash";
        }

        public async Task<string> GetResponseAsync(string userMessage, List<ChatMessage> history, Document? linkedDocument = null)
        {
            try
            {
                // Build system instruction using Content(string) constructor
                var systemPromptText = BuildSystemPrompt(linkedDocument);
                var systemInstruction = new Content(systemPromptText);

                // Create Gemini model with system instruction
                IGenerativeAI genAI = new GoogleAI(_apiKey);
                var model = genAI.GenerativeModel(_modelName, systemInstruction: systemInstruction);

                // Build chat history using ContentResponse(text, role) for StartChat
                var chatHistory = new List<ContentResponse>();
                foreach (var msg in history)
                {
                    var role = msg.Sender == "User" ? "user" : "model";
                    chatHistory.Add(new ContentResponse(msg.Content, role));
                }

                // Start chat with history and send current message
                var chat = model.StartChat(chatHistory);
                var response = await chat.SendMessage(userMessage);

                var responseText = response?.Text;
                if (string.IsNullOrWhiteSpace(responseText))
                {
                    return "Xin lỗi, tôi không thể tạo phản hồi lúc này. Vui lòng thử lại.";
                }

                return responseText;
            }
            catch (Exception ex)
            {
                return $"Đã xảy ra lỗi khi kết nối với Gemini AI: {ex.Message}. Vui lòng kiểm tra API Key và thử lại.";
            }
        }

        private static string BuildSystemPrompt(Document? linkedDocument)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Bạn là trợ lý AI thông minh của hệ thống quản lý tài liệu học tập Group9.");
            sb.AppendLine("Bạn hỗ trợ sinh viên học tập, giải đáp thắc mắc về tài liệu và môn học.");
            sb.AppendLine("Hãy trả lời bằng tiếng Việt, thân thiện, chính xác và hữu ích.");

            if (linkedDocument != null)
            {
                sb.AppendLine();
                sb.AppendLine("--- THÔNG TIN TÀI LIỆU ĐANG HỎI ---");
                sb.AppendLine($"Tiêu đề: {linkedDocument.Title}");
                sb.AppendLine($"File: {linkedDocument.FileName}");

                if (linkedDocument.Subject != null)
                {
                    sb.AppendLine($"Môn học: {linkedDocument.Subject.SubjectCode} - {linkedDocument.Subject.SubjectName}");
                }

                if (!string.IsNullOrWhiteSpace(linkedDocument.Description))
                {
                    sb.AppendLine($"Mô tả: {linkedDocument.Description}");
                }

                sb.AppendLine($"Kích thước file: {FormatSize(linkedDocument.FileSize)}");
                sb.AppendLine();
                sb.AppendLine("Hãy dựa trên thông tin tài liệu trên để hỗ trợ người dùng.");
                sb.AppendLine("Nếu câu hỏi liên quan đến nội dung bên trong file, hãy giải thích dựa trên tiêu đề và mô tả, đồng thời gợi ý người dùng mở file để xem chi tiết.");
            }

            return sb.ToString();
        }

        private static string FormatSize(long bytes)
        {
            string[] suffix = { "B", "KB", "MB", "GB" };
            int index = 0;
            double dblSvc = bytes;
            while (dblSvc >= 1024 && index < suffix.Length - 1)
            {
                index++;
                dblSvc /= 1024;
            }
            return $"{dblSvc:0.##} {suffix[index]}";
        }
    }
}
