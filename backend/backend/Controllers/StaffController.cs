using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using backend.Constants;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{Roles.Staff},{Roles.Admin}")]
public class StaffController : ControllerBase
{
    private readonly AppDbContext _context;

    public StaffController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/staff - Get all staff members
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetStaff()
    {
        var staff = await _context.Staff
            .Include(s => s.User)
            .Where(s => s.IsActive)
            .Select(s => new 
            {
                s.Id,
                FullName = $"{s.User.FirstName} {s.User.LastName}",
                Email = s.User.Email,
                s.Department,
                s.Position,
                s.EmployeeId,
                s.HireDate,
                s.IsActive
            })
            .ToListAsync();
        
        return Ok(staff);
    }

    // GET: api/staff/5 - Get specific staff member
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetStaffMember(int id)
    {
        var staff = await _context.Staff
            .Include(s => s.User)
            .Where(s => s.Id == id)
            .Select(s => new 
            {
                s.Id,
                FullName = $"{s.User.FirstName} {s.User.LastName}",
                Email = s.User.Email,
                s.Department,
                s.Position,
                s.EmployeeId,
                s.HireDate,
                s.Salary,
                s.IsActive
            })
            .FirstOrDefaultAsync();

        if (staff == null)
        {
            return NotFound(new { message = $"Staff member with ID {id} not found" });
        }

        return staff;
    }

    // GET: api/staff/profile - Get current staff member's profile
    [HttpGet("profile")]
    public async Task<ActionResult<Staff>> GetMyProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var staff = await _context.Staff
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (staff == null)
        {
            return NotFound(new { message = "Staff profile not found" });
        }

        return staff;
    }

    // PUT: api/staff/profile - Update current staff member's profile
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateStaffProfileDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var staff = await _context.Staff
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (staff == null)
        {
            return NotFound(new { message = "Staff profile not found" });
        }

        // Update user fields
        staff.User.FirstName = dto.FirstName;
        staff.User.LastName = dto.LastName;

        // Update staff fields (limited for self-update)
        staff.Department = dto.Department;
        staff.Position = dto.Position;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PUT: api/staff/5 - Update staff member (Admin only)
    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UpdateStaffMember(int id, [FromBody] UpdateStaffAdminDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var staff = await _context.Staff
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (staff == null)
        {
            return NotFound(new { message = $"Staff member with ID {id} not found" });
        }

        // Update user fields
        staff.User.FirstName = dto.FirstName;
        staff.User.LastName = dto.LastName;

        // Update staff fields
        staff.Department = dto.Department;
        staff.Position = dto.Position;
        staff.Salary = dto.Salary;
        staff.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/staff/5 - Deactivate staff member (Admin only)
    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeactivateStaffMember(int id)
    {
        var staff = await _context.Staff.FindAsync(id);

        if (staff == null)
        {
            return NotFound(new { message = $"Staff member with ID {id} not found" });
        }

        // Don't actually delete, just deactivate
        staff.IsActive = false;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Staff member deactivated successfully" });
    }

    // GET: api/staff/dashboard - Get dashboard statistics
    [HttpGet("dashboard")]
    public async Task<ActionResult<object>> GetDashboard()
    {
        var today = DateTime.Today;
        var thisMonth = new DateTime(today.Year, today.Month, 1);

        var stats = new
        {
            TotalPatients = await _context.Patients.CountAsync(),
            TotalDoctors = await _context.Doctors.CountAsync(d => d.IsApproved),
            PendingDoctors = await _context.Doctors.CountAsync(d => !d.IsApproved),
            TodayAppointments = await _context.Appointments
                .CountAsync(a => a.AppointmentDate.Date == today),
            MonthlyAppointments = await _context.Appointments
                .CountAsync(a => a.AppointmentDate >= thisMonth),
            ActiveStaff = await _context.Staff.CountAsync(s => s.IsActive),
            RecentAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                .ThenInclude(d => d.User)
                .Where(a => a.AppointmentDate >= today)
                .OrderBy(a => a.AppointmentDate)
                .Take(10)
                .Select(a => new 
                {
                    a.Id,
                    PatientName = $"{a.Patient.User.FirstName} {a.Patient.User.LastName}",
                    DoctorName = $"{a.Doctor.User.FirstName} {a.Doctor.User.LastName}",
                    a.AppointmentDate,
                    a.Status
                })
                .ToListAsync()
        };

        return Ok(stats);
    }

    // GET: api/staff/departments - Get all departments
    [HttpGet("departments")]
    public async Task<ActionResult<IEnumerable<object>>> GetDepartments()
    {
        var departments = await _context.Staff
            .Where(s => s.IsActive)
            .GroupBy(s => s.Department)
            .Select(g => new 
            {
                Department = g.Key,
                StaffCount = g.Count()
            })
            .ToListAsync();
        
        return Ok(departments);
    }

    // GET: api/staff/search - Search staff members
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<object>>> SearchStaff([FromQuery] string? name, [FromQuery] string? department)
    {
        var query = _context.Staff
            .Include(s => s.User)
            .Where(s => s.IsActive);

        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(s => (s.User.FirstName + " " + s.User.LastName).Contains(name));
        }

        if (!string.IsNullOrEmpty(department))
        {
            query = query.Where(s => s.Department.Contains(department));
        }

        var staff = await query
            .Select(s => new 
            {
                s.Id,
                FullName = $"{s.User.FirstName} {s.User.LastName}",
                Email = s.User.Email,
                s.Department,
                s.Position,
                s.EmployeeId
            })
            .Take(50)
            .ToListAsync();
        
        return Ok(staff);
    }
}

// DTOs for Staff operations
public class UpdateStaffProfileDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
}

public class UpdateStaffAdminDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public bool IsActive { get; set; } = true;
}