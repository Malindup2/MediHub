namespace backend.Models;

public class Patient
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty; // FK to User.Id
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
}
