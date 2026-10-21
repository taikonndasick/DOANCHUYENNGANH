using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKTX.Models
{
    public class SurveyResponse
    {
        public int Id { get; set; }

        [Required]
        public int ResidentProfileId { get; set; }

        [ForeignKey(nameof(ResidentProfileId))]
        public ResidentProfile? ResidentProfile { get; set; }

        [Required]
        public string SurveyTitle { get; set; } = "Khảo sát mức độ hài lòng";

        [Range(1, 5)]
        public int SatisfactionLevel { get; set; } = 5;

        public string? Feedback { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
