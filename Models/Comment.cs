using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TanuiApp.Models
{
    public class Comment
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string UserId { get; set; }

        public string Text { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
