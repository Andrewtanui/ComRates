using System;
using System.Collections.Generic;
using TanuiApp.Models;

namespace TanuiApp.ViewModels
{
    public class ThreadSummary
    {
        public string ThreadKey { get; set; } = string.Empty;
        public string WithUserId { get; set; } = string.Empty;
        public string WithUserName { get; set; } = string.Empty;
        public string? WithUserAvatar { get; set; }
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastAt { get; set; }
        public int Unread { get; set; }
        public int? ProductId { get; set; }
    }

    public class ChatViewModel
    {
        public List<ThreadSummary> Threads { get; set; } = new();
        public List<Message> Messages { get; set; } = new();
        public string? SelectedThreadKey { get; set; }
        public string? WithUserId { get; set; }
        public int? ProductId { get; set; }
    }
}







