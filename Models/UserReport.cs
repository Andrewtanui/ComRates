using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace TanuiApp.Models
{
    public class UserReport
    {
        public int Id { get; set; }
        public string ReportedUserId { get; set; } = string.Empty;
        public string ReporterId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime ReportedAt { get; set; } = DateTime.Now;
        public bool IsResolved { get; set; } = false;
        public DateTime? ResolvedAt { get; set; }
        public string? AdminNotes { get; set; }

        [ForeignKey("ReportedUserId")]
        public virtual Users? ReportedUser { get; set; }

        [ForeignKey("ReporterId")]
        public virtual Users? Reporter { get; set; }
    }
}
