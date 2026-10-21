using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKTX.Models
{
    public class StudentRegistrationDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentRegistrationId { get; set; }

        [ForeignKey("StudentRegistrationId")]
        public StudentRegistration? StudentRegistration { get; set; }

        // CitizenId / HealthInsurance / AdmissionProof / PortraitPhoto
        [Required]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        public string FilePath { get; set; } = string.Empty;

        public string? OriginalFileName { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}
