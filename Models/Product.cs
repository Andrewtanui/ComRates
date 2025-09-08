using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TanuiApp.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required, StringLength(500)]
        public string Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; }
        public double Rating { get; set; }
        public bool OnSale { get; set; }
        public DateTime? SaleEndDate { get; set; }
        public string Category { get; set; }

        // 🔗 Link to the User who created it
        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; }
    }
}
