namespace backend.Models;

public class Patient
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty; // FK to ApplicationUser
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? EmergencyContact { get; set; }
    public string? BloodType { get; set; }
    public string? Allergies { get; set; }

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
}
