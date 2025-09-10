using System.ComponentModel.DataAnnotations;

namespace backend.DTO
{
    public class RegisterStaff
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
        public string Department { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string EmployeeId { get; set; } = string.Empty;
    }
}

