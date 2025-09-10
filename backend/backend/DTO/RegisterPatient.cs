using System.ComponentModel.DataAnnotations;

namespace backend.DTO
{
    public class RegisterPatient
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
        
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }
        
        [StringLength(10)]
        public string Gender { get; set; } = string.Empty;
    }
}
