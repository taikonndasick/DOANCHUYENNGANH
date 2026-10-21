using System.ComponentModel.DataAnnotations;

namespace QuanLyKTX.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public string Type { get; set; } = "Monthly"; // Monthly / OneTime
    }
}