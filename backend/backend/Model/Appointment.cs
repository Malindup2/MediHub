using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class Appointment
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string PatientName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string DoctorName { get; set; } = string.Empty;
    
    [Required]
    public DateTime AppointmentDate { get; set; }
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string Status { get; set; } = "Scheduled";
    
    // Optional: Keep these for future relations
    public string? PatientId { get; set; }
    public string? DoctorId { get; set; }
}
