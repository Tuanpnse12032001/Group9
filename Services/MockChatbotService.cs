using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Group9.Models;

namespace Group9.Services
{
    public class MockChatbotService : IChatbotService
    {
        public Task<string> GetResponseAsync(string userMessage, List<ChatMessage> history, Document? linkedDocument = null)
        {
            // Simulate brief network delay
            var cleanMsg = userMessage.Trim().ToLower();
            string reply;

            if (linkedDocument != null)
            {
                reply = $"[MOCK CHATBOT - HỎI ĐÁP TÀI LIỆU]\n" +
                        $"Tôi đã nhận ngữ cảnh từ tài liệu: \"{linkedDocument.Title}\" ({linkedDocument.FileName}, kích thước: {FormatSize(linkedDocument.FileSize)}).\n\n" +
                        $"Dựa trên tài liệu này, câu trả lời giả lập cho câu hỏi \"{userMessage}\" là:\n" +
                        $"- Tài liệu thuộc môn học: {linkedDocument.Subject?.SubjectCode ?? "N/A"} - {linkedDocument.Subject?.SubjectName ?? "N/A"}.\n" +
                        $"- Mô tả tài liệu: {linkedDocument.Description ?? "Không có mô tả"}.\n" +
                        $"- Đây là phản hồi thử nghiệm tự động từ Mock Chatbot. Trong thực tế, AI sẽ đọc toàn bộ nội dung của file vật lý lưu tại '{linkedDocument.StoragePath}' và tổng hợp câu trả lời chính xác cho bạn.";
            }
            else
            {
                if (cleanMsg.Contains("chào") || cleanMsg.Contains("hello") || cleanMsg.Contains("hi"))
                {
                    reply = "Xin chào! Tôi là trợ lý ảo giả lập của Group9. Tôi có thể hỗ trợ gì cho bạn?";
                }
                else if (cleanMsg.Contains("giúp") || cleanMsg.Contains("help") || cleanMsg.Contains("tính năng"))
                {
                    reply = "Hiện tại tôi đang hoạt động ở chế độ giả lập (Mock Mode). Bạn có thể thử nghiệm các chức năng:\n" +
                            "1. Gửi tin nhắn thông thường để xem lịch sử chat.\n" +
                            "2. Tạo phiên chat liên kết với Tài liệu (Document) để hỏi đáp về tài liệu đó.\n" +
                            "3. Quản lý, xóa các phiên trò chuyện.";
                }
                else if (cleanMsg.Contains("tên") || cleanMsg.Contains("là ai") || cleanMsg.Contains("who are you"))
                {
                    reply = "Tôi là Mock Chatbot Service hỗ trợ kiểm thử API của dự án Group9.";
                }
                else if (cleanMsg.Contains("thời gian") || cleanMsg.Contains("giờ"))
                {
                    reply = $"Thời gian hệ thống hiện tại là: {DateTime.Now:dd/MM/yyyy HH:mm:ss}.";
                }
                else
                {
                    reply = $"[MOCK RESPONSE]\n" +
                            $"Cảm ơn bạn đã gửi tin nhắn: \"{userMessage}\".\n" +
                            $"Đây là phản hồi tự động mẫu từ Mock Chatbot.\n" +
                            $"Số lượng tin nhắn trước đó trong phiên chat này là: {history.Count} tin nhắn.";
                }
            }

            return Task.FromResult(reply);
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
