using System.Collections.Generic;
using System.Threading.Tasks;
using TanuiApp.Models;

namespace TanuiApp.Services
{
    public interface IChatbotService
    {
        Task<string> GetBotReplyAsync(string userMessage, string? userId = null);
        Task<(string text, List<(string text, string url)> links)> GetBotReplyWithLinksAsync(string userMessage, string? userName = null, string? userId = null);
        Task RetrainModelAsync();
        Task<bool> AddTrainingDataAsync(string text, string intent, string? addedBy = null);
        Task<List<ChatbotConversation>> GetUserConversationHistoryAsync(string userId, int limit = 10);
        Task<bool> ProvideFeedbackAsync(int conversationId, bool wasHelpful, string? feedback = null);
    }
}



