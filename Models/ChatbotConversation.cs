using System;
using System.ComponentModel.DataAnnotations;

namespace TanuiApp.Models
{
    public class ChatbotConversation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string UserMessage { get; set; } = string.Empty;

        [Required]
        public string BotResponse { get; set; } = string.Empty;

        [Required]
        public string DetectedIntent { get; set; } = string.Empty;

        public float ConfidenceScore { get; set; }

        public bool WasHelpful { get; set; } = true;

        public string? UserFeedback { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? SessionId { get; set; }

        // Navigation properties
        public virtual Users? User { get; set; }
    }
}
