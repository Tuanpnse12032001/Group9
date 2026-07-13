using System.Collections.Generic;
using System.Threading.Tasks;
using Group9.Models;

namespace Group9.Services
{
    public interface IChatbotService
    {
        Task<string> GetResponseAsync(string userMessage, List<ChatMessage> history, Document? linkedDocument = null);
    }
}
