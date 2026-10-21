using System.ComponentModel.DataAnnotations;

namespace QuanLyKTX.Models
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Resident";
    }
}