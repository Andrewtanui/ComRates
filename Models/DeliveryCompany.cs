using System;
using System.ComponentModel.DataAnnotations;

namespace TanuiApp.Models
{
    public class DeliveryCompany
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(80)]
        public string? LicenseNumber { get; set; }

        [MaxLength(120)]
        public string? ContactEmail { get; set; }

        [MaxLength(40)]
        public string? ContactPhone { get; set; }

        [MaxLength(80)]
        public string? Town { get; set; }

        [MaxLength(80)]
        public string? County { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
