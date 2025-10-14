using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TanuiApp.Data;
using TanuiApp.Models;
using TanuiApp.Services;
using System.Linq;
using System.Threading.Tasks;

namespace TanuiApp.Controllers
{
    [Authorize]
    public class ChatbotAdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IChatbotService _chatbot;

        public ChatbotAdminController(AppDbContext context, IChatbotService chatbot)
        {
            _context = context;
            _chatbot = chatbot;
        }

        // Admin dashboard for chatbot analytics
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            // Check if user is admin
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user?.UserRole != UserRole.SystemAdmin)
            {
                return Forbid();
            }

            var totalConversations = await _context.ChatbotConversations.CountAsync();
            var todayConversations = await _context.ChatbotConversations
                .Where(c => c.CreatedAt.Date == DateTime.UtcNow.Date)
                .CountAsync();

            var avgConfidence = await _context.ChatbotConversations
                .AverageAsync(c => (double?)c.ConfidenceScore) ?? 0;

            var topIntents = await _context.ChatbotConversations
                .GroupBy(c => c.DetectedIntent)
                .Select(g => new { Intent = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            var recentConversations = await _context.ChatbotConversations
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAt)
                .Take(20)
                .ToListAsync();

            var lowConfidenceConversations = await _context.ChatbotConversations
                .Include(c => c.User)
                .Where(c => c.ConfidenceScore < 0.5f)
                .OrderByDescending(c => c.CreatedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.TotalConversations = totalConversations;
            ViewBag.TodayConversations = todayConversations;
            ViewBag.AvgConfidence = avgConfidence;
            ViewBag.TopIntents = topIntents;
            ViewBag.RecentConversations = recentConversations;
            ViewBag.LowConfidenceConversations = lowConfidenceConversations;

            return View();
        }

        // Training data management
        [HttpGet]
        public async Task<IActionResult> TrainingData()
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user?.UserRole != UserRole.SystemAdmin)
            {
                return Forbid();
            }

            var trainingData = await _context.ChatbotTrainingData
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(trainingData);
        }

        // Add new training data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTrainingData(string text, string intent)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user?.UserRole != UserRole.SystemAdmin)
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(intent))
            {
                TempData["Error"] = "Text and intent are required.";
                return RedirectToAction(nameof(TrainingData));
            }

            var success = await _chatbot.AddTrainingDataAsync(text, intent, user.Id);

            if (success)
            {
                TempData["Success"] = "Training data added and model retrained successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to add training data.";
            }

            return RedirectToAction(nameof(TrainingData));
        }

        // Delete training data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTrainingData(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user?.UserRole != UserRole.SystemAdmin)
            {
                return Forbid();
            }

            var trainingData = await _context.ChatbotTrainingData.FindAsync(id);
            if (trainingData != null)
            {
                _context.ChatbotTrainingData.Remove(trainingData);
                await _context.SaveChangesAsync();
                await _chatbot.RetrainModelAsync();
                TempData["Success"] = "Training data deleted and model retrained.";
            }

            return RedirectToAction(nameof(TrainingData));
        }

        // Retrain model manually
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RetrainModel()
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user?.UserRole != UserRole.SystemAdmin)
            {
                return Forbid();
            }

            await _chatbot.RetrainModelAsync();
            TempData["Success"] = "Model retrained successfully!";

            return RedirectToAction(nameof(Dashboard));
        }

        // View all conversations
        [HttpGet]
        public async Task<IActionResult> AllConversations(int page = 1, int pageSize = 50)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user?.UserRole != UserRole.SystemAdmin)
            {
                return Forbid();
            }

            var totalCount = await _context.ChatbotConversations.CountAsync();
            var conversations = await _context.ChatbotConversations
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            return View(conversations);
        }

        // Export conversations to CSV
        [HttpGet]
        public async Task<IActionResult> ExportConversations()
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user?.UserRole != UserRole.SystemAdmin)
            {
                return Forbid();
            }

            var conversations = await _context.ChatbotConversations
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Date,Time,User,UserMessage,BotResponse,Intent,Confidence,WasHelpful");

            foreach (var conv in conversations)
            {
                csv.AppendLine($"\"{conv.CreatedAt:yyyy-MM-dd}\",\"{conv.CreatedAt:HH:mm:ss}\",\"{conv.User?.FullName ?? "Unknown"}\",\"{EscapeCsv(conv.UserMessage)}\",\"{EscapeCsv(conv.BotResponse)}\",\"{conv.DetectedIntent}\",\"{conv.ConfidenceScore:F4}\",\"{conv.WasHelpful}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"chatbot_conversations_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\"", "\"\"");
        }
    }
}
