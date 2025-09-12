using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TanuiApp.Models
{
    public class Message
    {
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [Required]
        public string RecipientId { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;

        // Optional context (e.g., product conversation)
        public int? ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        // Thread key groups messages between two users (and optional product)
        [Required]
        [StringLength(200)]
        public string ThreadKey { get; set; } = string.Empty;
    }
}

