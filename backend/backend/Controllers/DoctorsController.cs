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
[Authorize]
public class DoctorsController : ControllerBase
{
    private readonly AppDbContext _context;

    public DoctorsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/doctors - Get all approved doctors (public endpoint)
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<object>>> GetDoctors()
    {
        var doctors = await _context.Doctors
            .Include(d => d.User)
            .Where(d => d.IsApproved && d.IsAvailable)
            .Select(d => new 
            {
                d.Id,
                d.Specialization,
                d.ExperienceYears,
                d.ConsultationFee,
                d.Bio,
                FullName = $"{d.User.FirstName} {d.User.LastName}",
                d.IsAvailable
            })
            .ToListAsync();
        
        return Ok(doctors);
    }

    // GET: api/doctors/5 - Get specific doctor details
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetDoctor(int id)
    {
        var doctor = await _context.Doctors
            .Include(d => d.User)
            .Where(d => d.Id == id && d.IsApproved)
            .Select(d => new 
            {
                d.Id,
                d.Specialization,
                d.ExperienceYears,
                d.ConsultationFee,
                d.Bio,
                FullName = $"{d.User.FirstName} {d.User.LastName}",
                d.IsAvailable,
                Email = d.User.Email
            })
            .FirstOrDefaultAsync();

        if (doctor == null)
        {
            return NotFound(new { message = $"Doctor with ID {id} not found" });
        }

        return doctor;
    }

    // GET: api/doctors/profile - Get current doctor's profile
    [HttpGet("profile")]
    [Authorize(Roles = Roles.Doctor)]
    public async Task<ActionResult<Doctor>> GetMyProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var doctor = await _context.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null)
        {
            return NotFound(new { message = "Doctor profile not found" });
        }

        return doctor;
    }

    // PUT: api/doctors/profile - Update current doctor's profile
    [HttpPut("profile")]
    [Authorize(Roles = Roles.Doctor)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateDoctorProfileDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var doctor = await _context.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null)
        {
            return NotFound(new { message = "Doctor profile not found" });
        }

        // Update doctor fields
        doctor.Specialization = dto.Specialization;
        doctor.ExperienceYears = dto.ExperienceYears;
        doctor.ConsultationFee = dto.ConsultationFee;
        doctor.Bio = dto.Bio;
        doctor.IsAvailable = dto.IsAvailable;

        // Update user fields
        doctor.User.FirstName = dto.FirstName;
        doctor.User.LastName = dto.LastName;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/doctors/pending - Get pending doctor approvals (Staff/Admin only)
    [HttpGet("pending")]
    [Authorize(Roles = $"{Roles.Staff},{Roles.Admin}")]
    public async Task<ActionResult<IEnumerable<object>>> GetPendingDoctors()
    {
        var doctors = await _context.Doctors
            .Include(d => d.User)
            .Where(d => !d.IsApproved)
            .Select(d => new 
            {
                d.Id,
                d.Specialization,
                d.LicenseNumber,
                d.ExperienceYears,
                FullName = $"{d.User.FirstName} {d.User.LastName}",
                Email = d.User.Email,
                AppliedDate = d.User.Id // This could be enhanced with actual application date
            })
            .ToListAsync();
        
        return Ok(doctors);
    }

    // PUT: api/doctors/5/approve - Approve a doctor (Staff/Admin only)
    [HttpPut("{id}/approve")]
    [Authorize(Roles = $"{Roles.Staff},{Roles.Admin}")]
    public async Task<IActionResult> ApproveDoctor(int id)
    {
        var doctor = await _context.Doctors.FindAsync(id);

        if (doctor == null)
        {
            return NotFound(new { message = $"Doctor with ID {id} not found" });
        }

        doctor.IsApproved = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Doctor approved successfully" });
    }

    // PUT: api/doctors/5/reject - Reject a doctor (Staff/Admin only)
    [HttpPut("{id}/reject")]
    [Authorize(Roles = $"{Roles.Staff},{Roles.Admin}")]
    public async Task<IActionResult> RejectDoctor(int id)
    {
        var doctor = await _context.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doctor == null)
        {
            return NotFound(new { message = $"Doctor with ID {id} not found" });
        }

        // Remove the doctor record and user account
        _context.Doctors.Remove(doctor);
        _context.Users.Remove(doctor.User);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Doctor application rejected and removed" });
    }

    // GET: api/doctors/search - Search doctors by specialization
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<object>>> SearchDoctors([FromQuery] string? specialization)
    {
        var query = _context.Doctors
            .Include(d => d.User)
            .Where(d => d.IsApproved && d.IsAvailable);

        if (!string.IsNullOrEmpty(specialization))
        {
            query = query.Where(d => d.Specialization.Contains(specialization));
        }

        var doctors = await query
            .Select(d => new 
            {
                d.Id,
                d.Specialization,
                d.ExperienceYears,
                d.ConsultationFee,
                FullName = $"{d.User.FirstName} {d.User.LastName}",
                d.IsAvailable
            })
            .ToListAsync();
        
        return Ok(doctors);
    }
}

// DTOs for Doctor operations
public class UpdateDoctorProfileDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public decimal ConsultationFee { get; set; }
    public string? Bio { get; set; }
    public bool IsAvailable { get; set; } = true;
}