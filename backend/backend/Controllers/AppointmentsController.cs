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
        var appointments = await _context.Appointments
            .Where(a => a.PatientId == userId || a.DoctorId == userId)
            .ToListAsync();
        
        return Ok(appointments);
    }

    // GET: api/appointments/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Appointment>> GetAppointment(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var appointment = await _context.Appointments
            .Where(a => a.Id == id && (a.PatientId == userId || a.DoctorId == userId))
            .FirstOrDefaultAsync();

        if (appointment == null)
        {
            return NotFound(new { message = $"Appointment with ID {id} not found" });
        }

        return appointment;
    }

    // POST: api/appointments
    [HttpPost]
    public async Task<ActionResult<Appointment>> CreateAppointment([FromBody] Appointment appointment)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        appointment.PatientId = userId!; // Set the current user as the patient
        appointment.Status = "Scheduled";

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAppointment), new { id = appointment.Id }, appointment);
    }

    // PUT: api/appointments/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAppointment(int id, [FromBody] Appointment appointment)
    {
        if (id != appointment.Id)
        {
            return BadRequest(new { message = "ID mismatch" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var existingAppointment = await _context.Appointments
            .Where(a => a.Id == id && (a.PatientId == userId || a.DoctorId == userId))
            .FirstOrDefaultAsync();

        if (existingAppointment == null)
        {
            return NotFound(new { message = $"Appointment with ID {id} not found" });
        }

        existingAppointment.AppointmentDate = appointment.AppointmentDate;
        existingAppointment.Description = appointment.Description;
        existingAppointment.Status = appointment.Status;

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
        var appointment = await _context.Appointments
            .Where(a => a.Id == id && (a.PatientId == userId || a.DoctorId == userId))
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
