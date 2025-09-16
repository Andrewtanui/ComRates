using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TanuiApp.Data;
using TanuiApp.Models;
using TanuiApp.Hubs;
using Microsoft.AspNetCore.SignalR;
using TanuiApp.ViewModels;

namespace TanuiApp.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly IHubContext<MessageHub> _hubContext;

        public MessagesController(AppDbContext context, UserManager<Users> userManager, IHubContext<MessageHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
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


