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
public class MedicalRecordsController : ControllerBase
{
    private readonly AppDbContext _context;

    public MedicalRecordsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/medical-records - Get medical records (role-based access)
    [HttpGet]
    [Authorize(Roles = $"{Roles.Doctor},{Roles.Staff},{Roles.Admin}")]
    public async Task<ActionResult<IEnumerable<object>>> GetMedicalRecords()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        IQueryable<MedicalRecord> query = _context.MedicalRecords
            .Include(mr => mr.Patient)
            .ThenInclude(p => p.User)
            .Include(mr => mr.Doctor)
            .ThenInclude(d => d.User);

        // If user is a doctor, only show their own records
        if (userRoles.Contains(Roles.Doctor))
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor != null)
            {
                query = query.Where(mr => mr.DoctorId == doctor.Id);
            }
        }

        var records = await query
            .Select(mr => new 
            {
                mr.Id,
                mr.Title,
                mr.Description,
                mr.Diagnosis,
                mr.CreatedAt,
                PatientName = $"{mr.Patient.User.FirstName} {mr.Patient.User.LastName}",
                DoctorName = $"{mr.Doctor.User.FirstName} {mr.Doctor.User.LastName}",
                mr.IsPrivate
            })
            .OrderByDescending(mr => mr.CreatedAt)
            .ToListAsync();
        
        return Ok(records);
    }

    // GET: api/medical-records/5 - Get specific medical record
    [HttpGet("{id}")]
    [Authorize(Roles = $"{Roles.Doctor},{Roles.Staff},{Roles.Admin},{Roles.Patient}")]
    public async Task<ActionResult<MedicalRecord>> GetMedicalRecord(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        var medicalRecord = await _context.MedicalRecords
            .Include(mr => mr.Patient)
            .ThenInclude(p => p.User)
            .Include(mr => mr.Doctor)
            .ThenInclude(d => d.User)
            .Include(mr => mr.Appointment)
            .FirstOrDefaultAsync(mr => mr.Id == id);

        if (medicalRecord == null)
        {
            return NotFound(new { message = $"Medical record with ID {id} not found" });
        }

        // Check access permissions
        var hasAccess = false;

        if (userRoles.Contains(Roles.Admin) || userRoles.Contains(Roles.Staff))
        {
            hasAccess = true;
        }
        else if (userRoles.Contains(Roles.Doctor))
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            hasAccess = doctor?.Id == medicalRecord.DoctorId;
        }
        else if (userRoles.Contains(Roles.Patient))
        {
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
            hasAccess = patient?.Id == medicalRecord.PatientId && !medicalRecord.IsPrivate;
        }

        if (!hasAccess)
        {
            return Forbid();
        }

        return medicalRecord;
    }

    // POST: api/medical-records - Create new medical record (Doctor only)
    [HttpPost]
    [Authorize(Roles = Roles.Doctor)]
    public async Task<ActionResult<MedicalRecord>> CreateMedicalRecord([FromBody] CreateMedicalRecordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null)
        {
            return BadRequest(new { message = "Doctor profile not found" });
        }

        // Verify patient exists
        var patient = await _context.Patients.FindAsync(dto.PatientId);
        if (patient == null)
        {
            return BadRequest(new { message = "Patient not found" });
        }

        var medicalRecord = new MedicalRecord
        {
            PatientId = dto.PatientId,
            DoctorId = doctor.Id,
            AppointmentId = dto.AppointmentId,
            Title = dto.Title,
            Description = dto.Description,
            Diagnosis = dto.Diagnosis,
            Treatment = dto.Treatment,
            Prescription = dto.Prescription,
            LabResults = dto.LabResults,
            IsPrivate = dto.IsPrivate,
            CreatedAt = DateTime.UtcNow
        };

        _context.MedicalRecords.Add(medicalRecord);
        await _context.SaveChangesAsync();

        // Load navigation properties for response
        await _context.Entry(medicalRecord)
            .Reference(mr => mr.Patient)
            .LoadAsync();
        await _context.Entry(medicalRecord.Patient)
            .Reference(p => p.User)
            .LoadAsync();
        await _context.Entry(medicalRecord)
            .Reference(mr => mr.Doctor)
            .LoadAsync();
        await _context.Entry(medicalRecord.Doctor)
            .Reference(d => d.User)
            .LoadAsync();

        return CreatedAtAction(nameof(GetMedicalRecord), new { id = medicalRecord.Id }, medicalRecord);
    }

    // PUT: api/medical-records/5 - Update medical record (Doctor only - own records)
    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Doctor)]
    public async Task<IActionResult> UpdateMedicalRecord(int id, [FromBody] UpdateMedicalRecordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null)
        {
            return BadRequest(new { message = "Doctor profile not found" });
        }

        var medicalRecord = await _context.MedicalRecords
            .FirstOrDefaultAsync(mr => mr.Id == id && mr.DoctorId == doctor.Id);

        if (medicalRecord == null)
        {
            return NotFound(new { message = $"Medical record with ID {id} not found or access denied" });
        }

        medicalRecord.Title = dto.Title;
        medicalRecord.Description = dto.Description;
        medicalRecord.Diagnosis = dto.Diagnosis;
        medicalRecord.Treatment = dto.Treatment;
        medicalRecord.Prescription = dto.Prescription;
        medicalRecord.LabResults = dto.LabResults;
        medicalRecord.IsPrivate = dto.IsPrivate;
        medicalRecord.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/medical-records/5 - Delete medical record (Doctor only - own records)
    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Doctor)]
    public async Task<IActionResult> DeleteMedicalRecord(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null)
        {
            return BadRequest(new { message = "Doctor profile not found" });
        }

        var medicalRecord = await _context.MedicalRecords
            .FirstOrDefaultAsync(mr => mr.Id == id && mr.DoctorId == doctor.Id);

        if (medicalRecord == null)
        {
            return NotFound(new { message = $"Medical record with ID {id} not found or access denied" });
        }

        _context.MedicalRecords.Remove(medicalRecord);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/medical-records/patient/5 - Get medical records for specific patient
    [HttpGet("patient/{patientId}")]
    [Authorize(Roles = $"{Roles.Doctor},{Roles.Staff},{Roles.Admin}")]
    public async Task<ActionResult<IEnumerable<object>>> GetPatientMedicalRecords(int patientId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        var query = _context.MedicalRecords
            .Include(mr => mr.Doctor)
            .ThenInclude(d => d.User)
            .Where(mr => mr.PatientId == patientId);

        // If user is a doctor, only show non-private records or their own records
        if (userRoles.Contains(Roles.Doctor))
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor != null)
            {
                query = query.Where(mr => !mr.IsPrivate || mr.DoctorId == doctor.Id);
            }
        }

        var records = await query
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
                mr.Doctor.Specialization,
                mr.IsPrivate
            })
            .OrderByDescending(mr => mr.CreatedAt)
            .ToListAsync();
        
        return Ok(records);
    }
}

// DTOs for Medical Record operations
public class CreateMedicalRecordDto
{
    public int PatientId { get; set; }
    public int? AppointmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Diagnosis { get; set; }
    public string? Treatment { get; set; }
    public string? Prescription { get; set; }
    public string? LabResults { get; set; }
    public bool IsPrivate { get; set; } = false;
}

public class UpdateMedicalRecordDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Diagnosis { get; set; }
    public string? Treatment { get; set; }
    public string? Prescription { get; set; }
    public string? LabResults { get; set; }
    public bool IsPrivate { get; set; } = false;
}