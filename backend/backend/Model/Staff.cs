namespace backend.Models;

public class Staff
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty; // FK
    public string Department { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
}

