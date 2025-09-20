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
public class PatientsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PatientsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/patients - Get all patients (Staff/Admin only)
    [HttpGet]
    [Authorize(Roles = $"{Roles.Staff},{Roles.Admin},{Roles.Doctor}")]
    public async Task<ActionResult<IEnumerable<object>>> GetPatients()
    {
        var patients = await _context.Patients
            .Include(p => p.User)
            .Select(p => new 
            {
                p.Id,
                FullName = $"{p.User.FirstName} {p.User.LastName}",
                Email = p.User.Email,
                p.PhoneNumber,
                p.DateOfBirth,
                p.Gender,
                Age = DateTime.Now.Year - p.DateOfBirth.Year
            })
            .ToListAsync();
        
        return Ok(patients);
    }

    // GET: api/patients/5 - Get specific patient (Staff/Admin/Doctor only)
    [HttpGet("{id}")]
    [Authorize(Roles = $"{Roles.Staff},{Roles.Admin},{Roles.Doctor}")]
    public async Task<ActionResult<object>> GetPatient(int id)
    {
        var patient = await _context.Patients
            .Include(p => p.User)
            .Where(p => p.Id == id)
            .Select(p => new 
            {
                p.Id,
                FullName = $"{p.User.FirstName} {p.User.LastName}",
                Email = p.User.Email,
                p.PhoneNumber,
                p.DateOfBirth,
                p.Gender,
                p.Address,
                p.EmergencyContact,
                p.BloodType,
                p.Allergies,
                Age = DateTime.Now.Year - p.DateOfBirth.Year
            })
            .FirstOrDefaultAsync();

        if (patient == null)
        {
            return NotFound(new { message = $"Patient with ID {id} not found" });
        }

        return patient;
    }

    // GET: api/patients/profile - Get current patient's profile
    [HttpGet("profile")]
    [Authorize(Roles = Roles.Patient)]
    public async Task<ActionResult<Patient>> GetMyProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
        {
            return NotFound(new { message = "Patient profile not found" });
        }

        return patient;
    }

    // PUT: api/patients/profile - Update current patient's profile
    [HttpPut("profile")]
    [Authorize(Roles = Roles.Patient)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdatePatientProfileDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
        {
            return NotFound(new { message = "Patient profile not found" });
        }

        // Update patient fields
        patient.PhoneNumber = dto.PhoneNumber;
        patient.DateOfBirth = dto.DateOfBirth;
        patient.Gender = dto.Gender;
        patient.Address = dto.Address;
        patient.EmergencyContact = dto.EmergencyContact;
        patient.BloodType = dto.BloodType;
        patient.Allergies = dto.Allergies;

        // Update user fields
        patient.User.FirstName = dto.FirstName;
        patient.User.LastName = dto.LastName;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/patients/5/appointments - Get patient's appointments (Patient themselves, or Staff/Doctor)
    [HttpGet("{id}/appointments")]
    [Authorize(Roles = $"{Roles.Patient},{Roles.Staff},{Roles.Admin},{Roles.Doctor}")]
    public async Task<ActionResult<IEnumerable<object>>> GetPatientAppointments(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        // Check if the user is the patient themselves
        var isOwnProfile = false;
        if (userRoles.Contains(Roles.Patient))
        {
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
            isOwnProfile = patient?.Id == id;
        }

        // Allow access if it's the patient's own profile or if user is staff/doctor/admin
        if (!isOwnProfile && !userRoles.Any(r => r == Roles.Staff || r == Roles.Admin || r == Roles.Doctor))
        {
            return Forbid();
        }

        var appointments = await _context.Appointments
            .Include(a => a.Doctor)
            .ThenInclude(d => d.User)
            .Where(a => a.PatientId == id)
            .Select(a => new 
            {
                a.Id,
                a.AppointmentDate,
                a.Status,
                a.Description,
                DoctorName = $"{a.Doctor.User.FirstName} {a.Doctor.User.LastName}",
                a.Doctor.Specialization,
                a.Fee,
                a.IsPaid
            })
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();
        
        return Ok(appointments);
    }

    // GET: api/patients/5/medical-records - Get patient's medical records (Patient themselves, or Staff/Doctor)
    [HttpGet("{id}/medical-records")]
    [Authorize(Roles = $"{Roles.Patient},{Roles.Staff},{Roles.Admin},{Roles.Doctor}")]
    public async Task<ActionResult<IEnumerable<object>>> GetPatientMedicalRecords(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        // Check if the user is the patient themselves
        var isOwnProfile = false;
        if (userRoles.Contains(Roles.Patient))
        {
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
            isOwnProfile = patient?.Id == id;
        }

        // Allow access if it's the patient's own profile or if user is staff/doctor/admin
        if (!isOwnProfile && !userRoles.Any(r => r == Roles.Staff || r == Roles.Admin || r == Roles.Doctor))
        {
            return Forbid();
        }

        var medicalRecords = await _context.MedicalRecords
            .Include(mr => mr.Doctor)
            .ThenInclude(d => d.User)
            .Where(mr => mr.PatientId == id && (!mr.IsPrivate || isOwnProfile))
            .Select(mr => new 
            {
                mr.Id,
                mr.Title,
                mr.Description,
                mr.Diagnosis,
                mr.Treatment,
                mr.Prescription,
                mr.CreatedAt,
                DoctorName = $"{mr.Doctor.User.FirstName} {mr.Doctor.User.LastName}",
                mr.Doctor.Specialization
            })
            .OrderByDescending(mr => mr.CreatedAt)
            .ToListAsync();
        
        return Ok(medicalRecords);
    }

    // GET: api/patients/search - Search patients (Staff/Admin only)
    [HttpGet("search")]
    [Authorize(Roles = $"{Roles.Staff},{Roles.Admin}")]
    public async Task<ActionResult<IEnumerable<object>>> SearchPatients([FromQuery] string? name, [FromQuery] string? email)
    {
        var query = _context.Patients
            .Include(p => p.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(p => (p.User.FirstName + " " + p.User.LastName).Contains(name));
        }

        if (!string.IsNullOrEmpty(email))
        {
            query = query.Where(p => p.User.Email!.Contains(email));
        }

        var patients = await query
            .Select(p => new 
            {
                p.Id,
                FullName = $"{p.User.FirstName} {p.User.LastName}",
                Email = p.User.Email,
                p.PhoneNumber,
                p.DateOfBirth,
                p.Gender
            })
            .Take(50) // Limit results
            .ToListAsync();
        
        return Ok(patients);
    }
}

// DTOs for Patient operations
public class UpdatePatientProfileDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? EmergencyContact { get; set; }
    public string? BloodType { get; set; }
    public string? Allergies { get; set; }
}