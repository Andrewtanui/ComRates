using System;
using System.ComponentModel.DataAnnotations;

namespace TanuiApp.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty; 

        [Required]
        [StringLength(200)]
        public string Type { get; set; } = "message"; 

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Body { get; set; }

        public string? Link { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}





