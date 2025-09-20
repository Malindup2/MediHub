namespace backend.Models;

public class Staff
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty; // FK to ApplicationUser
    public string Department { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateTime HireDate { get; set; } = DateTime.UtcNow;
    public decimal Salary { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    // Navigation property
    public ApplicationUser User { get; set; } = null!;
}

