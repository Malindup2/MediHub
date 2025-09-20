namespace backend.Models;

public class Doctor
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty; // FK to ApplicationUser
    public string Specialization { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public bool IsApproved { get; set; } = false; 
    public bool IsAvailable { get; set; } = true;
    public decimal ConsultationFee { get; set; } = 0;
    public string? Bio { get; set; }

    // Navigation property
    public ApplicationUser User { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
