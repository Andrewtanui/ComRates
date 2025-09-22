using System.Threading.Tasks;

namespace TanuiApp.Services
{
    public interface IChatbotService
    {
        Task<string> GetBotReplyAsync(string userMessage, string? userId = null);
        Task<(string text, System.Collections.Generic.List<(string text, string url)> links)> GetBotReplyWithLinksAsync(string userMessage, string? userName = null);
    }
}



