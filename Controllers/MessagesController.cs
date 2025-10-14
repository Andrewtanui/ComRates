using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TanuiApp.Data;
using TanuiApp.Models;
using TanuiApp.Hubs;
using Microsoft.AspNetCore.SignalR;
using TanuiApp.ViewModels;
using TanuiApp.Services;

namespace TanuiApp.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly IHubContext<MessageHub> _hubContext;
        private readonly IChatbotService _chatbot;

        public MessagesController(AppDbContext context, UserManager<Users> userManager, IHubContext<MessageHub> hubContext, IChatbotService chatbot)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
            _chatbot = chatbot;
        }

        public async Task<IActionResult> Inbox()
        {
            var userId = _userManager.GetUserId(User);
            var threads = await _context.Messages
                .Where(m => m.SenderId == userId || m.RecipientId == userId)
                .GroupBy(m => m.ThreadKey)
                .Select(g => new
                {
                    ThreadKey = g.Key,
                    LastAt = g.Max(m => m.CreatedAt),
                    LastMessage = g.OrderByDescending(m => m.CreatedAt).FirstOrDefault(),
                    Unread = g.Count(m => m.RecipientId == userId && !m.IsRead)
                })
                .OrderByDescending(t => t.LastAt)
                .ToListAsync();

            return View(threads);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AskBot([FromForm] string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return Json(new { ok = false, reply = "Please enter a question." });

            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            var (text, links) = await _chatbot.GetBotReplyWithLinksAsync(prompt, user?.FullName, userId);
            var linkDtos = links.Select(l => new { text = l.text, url = Url.Content(l.url) }).ToList();
            return Json(new { ok = true, reply = text, links = linkDtos });
        }

        [HttpGet]
        public async Task<IActionResult> ChatHistory()
        {
            var userId = _userManager.GetUserId(User);
            var history = await _chatbot.GetUserConversationHistoryAsync(userId, 20);
            return View(history);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProvideChatbotFeedback(int conversationId, bool wasHelpful, string? feedback)
        {
            var success = await _chatbot.ProvideFeedbackAsync(conversationId, wasHelpful, feedback);
            return Json(new { success });
        }

        [HttpGet]
        public async Task<IActionResult> ContactSupport(string? reason)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            var admin = await _context.Users
                .Where(u => u.UserRole == UserRole.SystemAdmin)
                .OrderBy(u => u.CreatedAt)
                .FirstOrDefaultAsync();

            if (admin == null)
            {
                TempData["Error"] = "Support is currently unavailable. Please try again later.";
                return RedirectToAction(nameof(Inbox));
            }

            // Build thread and create a greeting message if no thread exists
            var threadKey = BuildThreadKey(userId, admin.Id, null);
            var hasThread = await _context.Messages.AnyAsync(m => m.ThreadKey == threadKey);

            if (!hasThread)
            {
                // Customize message based on reason
                string messageContent = "Hello, I need help from support.";
                string notificationBody = "A user has started a support chat.";

                if (reason == "seller-upgrade" && user?.UserRole == UserRole.Buyer)
                {
                    messageContent = "Hello, I would like to request an upgrade to a Seller account. I want to start selling products on ComRates.";
                    notificationBody = $"{user.FullName} is requesting a Seller account upgrade.";
                }

                var intro = new Message
                {
                    SenderId = userId,
                    RecipientId = admin.Id,
                    Content = messageContent,
                    ProductId = null,
                    ThreadKey = threadKey
                };
                _context.Messages.Add(intro);
                await _context.SaveChangesAsync();

                // Notify admin
                var notification = new Notification
                {
                    UserId = admin.Id,
                    Type = "message",
                    Title = "New support request",
                    Body = notificationBody,
                    Link = Url.Action("Chat", "Messages", new { withUserId = userId }, Request.Scheme)
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Chat), new { withUserId = admin.Id });
        }

        public async Task<IActionResult> Chat(string? withUserId, int? productId)
        {
            var userId = _userManager.GetUserId(User);

            var threadsRaw = await _context.Messages
                .Where(m => m.SenderId == userId || m.RecipientId == userId)
                .GroupBy(m => m.ThreadKey)
                .Select(g => new
                {
                    ThreadKey = g.Key,
                    LastAt = g.Max(m => m.CreatedAt),
                    LastMessage = g.OrderByDescending(m => m.CreatedAt).FirstOrDefault(),
                    Unread = g.Count(m => m.RecipientId == userId && !m.IsRead)
                })
                .OrderByDescending(t => t.LastAt)
                .ToListAsync();

            var summaries = new List<ThreadSummary>();
            foreach (var t in threadsRaw)
            {
                var otherId = t.LastMessage.SenderId == userId ? t.LastMessage.RecipientId : t.LastMessage.SenderId;
                var other = await _context.Users.FirstOrDefaultAsync(u => u.Id == otherId);
                summaries.Add(new ThreadSummary
                {
                    ThreadKey = t.ThreadKey,
                    WithUserId = otherId,
                    WithUserName = other?.FullName ?? otherId,
                    WithUserAvatar = other?.ProfilePictureUrl,
                    LastMessage = t.LastMessage.Content,
                    LastAt = t.LastAt,
                    Unread = t.Unread,
                    ProductId = t.LastMessage.ProductId
                });
            }

            // pick selected thread
            string? selectedThreadKey = null;
            if (!string.IsNullOrEmpty(withUserId))
            {
                selectedThreadKey = BuildThreadKey(userId, withUserId, productId);
            }
            else if (summaries.Count > 0)
            {
                selectedThreadKey = summaries[0].ThreadKey;
                withUserId = summaries[0].WithUserId;
                productId = summaries[0].ProductId;
            }

            var messages = new List<Message>();
            if (!string.IsNullOrEmpty(selectedThreadKey))
            {
                messages = await _context.Messages
                    .Where(m => m.ThreadKey == selectedThreadKey)
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();

                foreach (var m in messages.Where(m => m.RecipientId == userId && !m.IsRead))
                    m.IsRead = true;
                await _context.SaveChangesAsync();

                // Determine if current user started the thread (first message sender)
                var firstMessage = messages.FirstOrDefault();
                ViewBag.CanDeleteThread = firstMessage != null && firstMessage.SenderId == userId;
            }

            var vm = new ChatViewModel
            {
                Threads = summaries,
                Messages = messages,
                SelectedThreadKey = selectedThreadKey,
                WithUserId = withUserId,
                ProductId = productId
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteThread(string threadKey)
        {
            if (string.IsNullOrWhiteSpace(threadKey)) return RedirectToAction(nameof(Chat));

            var userId = _userManager.GetUserId(User);
            var messages = await _context.Messages
                .Where(m => m.ThreadKey == threadKey)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            if (messages.Count == 0) return RedirectToAction(nameof(Chat));

            // Only the starter (first message sender) can delete the thread
            if (messages.First().SenderId != userId)
            {
                return Forbid();
            }

            _context.Messages.RemoveRange(messages);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Chat));
        }

        public async Task<IActionResult> Thread(string withUserId, int? productId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(withUserId) || withUserId == userId)
                return RedirectToAction(nameof(Inbox));

            var threadKey = BuildThreadKey(userId, withUserId, productId);

            var messages = await _context.Messages
                .Where(m => m.ThreadKey == threadKey)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            // mark as read
            foreach (var m in messages.Where(m => m.RecipientId == userId && !m.IsRead))
                m.IsRead = true;
            await _context.SaveChangesAsync();

            ViewBag.WithUserId = withUserId;
            ViewBag.ProductId = productId;
            return View(messages);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(string recipientId, string content, int? productId)
        {
            var senderId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(recipientId) || string.IsNullOrWhiteSpace(content))
                return RedirectToAction(nameof(Inbox));

            var message = new Message
            {
                SenderId = senderId,
                RecipientId = recipientId,
                Content = content.Trim(),
                ProductId = productId,
                ThreadKey = BuildThreadKey(senderId, recipientId, productId)
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Create notification for recipient
            var notification = new Notification
            {
                UserId = recipientId,
                Type = "message",
                Title = "New message",
                Body = content.Length > 80 ? content.Substring(0, 80) + "..." : content,
                Link = Url.Action("Chat", "Messages", new { withUserId = senderId, productId }, Request.Scheme)
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Broadcast to SignalR group
            await _hubContext.Clients.Group(message.ThreadKey).SendAsync("ReceiveMessage", new
            {
                message.SenderId,
                message.RecipientId,
                message.Content,
                message.ProductId,
                message.CreatedAt
            });

            // Fire notification to recipient group
            await _hubContext.Clients.Group($"user:{recipientId}").SendAsync("Notify", new { notification.Title, notification.Body, notification.Link });

            return RedirectToAction(nameof(Chat), new { withUserId = recipientId, productId });
        }

        internal static string BuildThreadKey(string a, string b, int? productId)
        {
            var first = string.CompareOrdinal(a, b) < 0 ? a : b;
            var second = string.CompareOrdinal(a, b) < 0 ? b : a;
            return $"{first}:{second}:{productId?.ToString() ?? "-"}";
        }
    }
}


