using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class MedicalRecord
{
    public int Id { get; set; }
    
    // Foreign Keys
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public int? AppointmentId { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Diagnosis { get; set; }
    
    [StringLength(1000)]
    public string? Treatment { get; set; }
    
    [StringLength(1000)]
    public string? Prescription { get; set; }
    
    [StringLength(500)]
    public string? LabResults { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsPrivate { get; set; } = false;
    
    // Navigation properties
    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
    public Appointment? Appointment { get; set; }
}