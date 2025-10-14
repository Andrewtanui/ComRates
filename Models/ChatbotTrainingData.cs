using System;
using System.ComponentModel.DataAnnotations;

namespace TanuiApp.Models
{
    public class ChatbotTrainingData
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Text { get; set; } = string.Empty;

        [Required]
        public string Intent { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public string? AddedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
