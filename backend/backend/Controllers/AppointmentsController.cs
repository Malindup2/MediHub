using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AppointmentsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/appointments
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Appointment>>> GetAppointments()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        // Find the user's patient or doctor record
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
        
        var appointments = await _context.Appointments
            .Include(a => a.Patient)
            .ThenInclude(p => p.User)
            .Include(a => a.Doctor)
            .ThenInclude(d => d.User)
            .Where(a => (patient != null && a.PatientId == patient.Id) || 
                       (doctor != null && a.DoctorId == doctor.Id))
            .ToListAsync();
        
        return Ok(appointments);
    }

    // GET: api/appointments/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Appointment>> GetAppointment(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
        
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .ThenInclude(p => p.User)
            .Include(a => a.Doctor)
            .ThenInclude(d => d.User)
            .Where(a => a.Id == id && 
                       ((patient != null && a.PatientId == patient.Id) || 
                        (doctor != null && a.DoctorId == doctor.Id)))
            .FirstOrDefaultAsync();

        if (appointment == null)
        {
            return NotFound(new { message = $"Appointment with ID {id} not found" });
        }

        return appointment;
    }

    // POST: api/appointments
    [HttpPost]
    public async Task<ActionResult<Appointment>> CreateAppointment([FromBody] CreateAppointmentDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
        
        if (patient == null)
        {
            return BadRequest(new { message = "Only patients can create appointments" });
        }

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = dto.DoctorId,
            AppointmentDate = dto.AppointmentDate,
            Description = dto.Description,
            Status = "Scheduled"
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        // Include navigation properties in response
        await _context.Entry(appointment)
            .Reference(a => a.Patient)
            .LoadAsync();
        await _context.Entry(appointment.Patient)
            .Reference(p => p.User)
            .LoadAsync();
        await _context.Entry(appointment)
            .Reference(a => a.Doctor)
            .LoadAsync();
        await _context.Entry(appointment.Doctor)
            .Reference(d => d.User)
            .LoadAsync();

        return CreatedAtAction(nameof(GetAppointment), new { id = appointment.Id }, appointment);
    }

    // PUT: api/appointments/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAppointment(int id, [FromBody] UpdateAppointmentDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);

        var existingAppointment = await _context.Appointments
            .Where(a => a.Id == id && 
                       ((patient != null && a.PatientId == patient.Id) || 
                        (doctor != null && a.DoctorId == doctor.Id)))
            .FirstOrDefaultAsync();

        if (existingAppointment == null)
        {
            return NotFound(new { message = $"Appointment with ID {id} not found" });
        }

        existingAppointment.AppointmentDate = dto.AppointmentDate;
        existingAppointment.Description = dto.Description;
        existingAppointment.Status = dto.Status;
        existingAppointment.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!AppointmentExists(id))
            {
                return NotFound(new { message = $"Appointment with ID {id} not found" });
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // DELETE: api/appointments/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);

        var appointment = await _context.Appointments
            .Where(a => a.Id == id && 
                       ((patient != null && a.PatientId == patient.Id) || 
                        (doctor != null && a.DoctorId == doctor.Id)))
            .FirstOrDefaultAsync();

        if (appointment == null)
        {
            return NotFound(new { message = $"Appointment with ID {id} not found" });
        }

        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool AppointmentExists(int id)
    {
        return _context.Appointments.Any(e => e.Id == id);
    }
}

// DTOs for appointments
public class CreateAppointmentDto
{
    public int DoctorId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class UpdateAppointmentDto
{
    public DateTime AppointmentDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
