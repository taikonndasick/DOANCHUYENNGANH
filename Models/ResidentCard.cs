using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKTX.Models
{
    public class ResidentCard
    {
        public int Id { get; set; }

        [Required]
        public int ResidentProfileId { get; set; }

        [ForeignKey("ResidentProfileId")]
        public ResidentProfile? ResidentProfile { get; set; }

        [Required]
        public string CardCode { get; set; } = string.Empty;

        public DateTime IssueDate { get; set; } = DateTime.Now;

        public DateTime? ExpiryDate { get; set; }

        public string Status { get; set; } = "Active";
    }
}