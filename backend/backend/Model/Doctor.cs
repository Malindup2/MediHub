namespace backend.Models;

public class Doctor
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty; // FK
    public string Specialization { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
}
