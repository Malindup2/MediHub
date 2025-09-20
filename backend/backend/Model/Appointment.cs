using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class Appointment
{
    public int Id { get; set; }
    
    // Foreign Keys
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    
    [Required]
    public DateTime AppointmentDate { get; set; }
    
    public DateTime? EndTime { get; set; }
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string Status { get; set; } = "Scheduled"; // Scheduled, Confirmed, Completed, Cancelled
    
    [StringLength(1000)]
    public string? Notes { get; set; }
    
    public decimal? Fee { get; set; }
    
    public bool IsPaid { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
}
