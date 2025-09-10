using System.ComponentModel.DataAnnotations;

namespace backend.DTO
{
    public class RegisterDoctor
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string Specialization { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string LicenseNumber { get; set; } = string.Empty;
        
        [Range(0, 50)]
        public int ExperienceYears { get; set; }
    }
}
