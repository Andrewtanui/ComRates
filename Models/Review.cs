using System;
using System.ComponentModel.DataAnnotations;

namespace TanuiApp.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Required, Range(1, 5)]
        public int Rating { get; set; }

        [Required, StringLength(500)]
        public string Content { get; set; }  // ✅ Use this for review text

        public string UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
