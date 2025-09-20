using backend.Data;
using backend.Models;
using backend.Services.Interfaces;
using backend.Constants;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Implementations;

public class AppointmentService : IAppointmentService
{
    private readonly AppDbContext _context;

    public AppointmentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsDoctorAvailableAsync(int doctorId, DateTime appointmentDate)
    {
        // Check if doctor exists and is approved
        var doctor = await _context.Doctors.FindAsync(doctorId);
        if (doctor == null || !doctor.IsApproved || !doctor.IsAvailable)
            return false;

        // Check if there's already an appointment at the same time (within 1 hour)
        var existingAppointment = await _context.Appointments
            .AnyAsync(a => a.DoctorId == doctorId && 
                          a.AppointmentDate <= appointmentDate.AddMinutes(30) && 
                          a.AppointmentDate >= appointmentDate.AddMinutes(-30) &&
                          a.Status != "Cancelled");

        return !existingAppointment;
    }

    public async Task<IEnumerable<DateTime>> GetAvailableTimeSlotsAsync(int doctorId, DateTime date)
    {
        var doctor = await _context.Doctors.FindAsync(doctorId);
        if (doctor == null || !doctor.IsApproved || !doctor.IsAvailable)
            return new List<DateTime>();

        // Define working hours (9 AM to 6 PM)
        var workingHours = new List<TimeSpan>();
        for (int hour = 9; hour <= 17; hour++)
        {
            workingHours.Add(new TimeSpan(hour, 0, 0));
            workingHours.Add(new TimeSpan(hour, 30, 0));
        }

        // Get existing appointments for the day
        var existingAppointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && 
                       a.AppointmentDate.Date == date.Date &&
                       a.Status != "Cancelled")
            .Select(a => a.AppointmentDate.TimeOfDay)
            .ToListAsync();

        // Filter out booked slots
        var availableSlots = workingHours
            .Where(time => !existingAppointments.Any(existing => 
                Math.Abs((time - existing).TotalMinutes) < 60))
            .Select(time => date.Date.Add(time))
            .Where(slot => slot > DateTime.Now) // Only future slots
            .ToList();

        return availableSlots;
    }

    public async Task<Appointment> ScheduleAppointmentAsync(int patientId, int doctorId, DateTime appointmentDate, string description)
    {
        // Validate inputs
        var patient = await _context.Patients.FindAsync(patientId);
        var doctor = await _context.Doctors.FindAsync(doctorId);

        if (patient == null)
            throw new ArgumentException("Patient not found");

        if (doctor == null || !doctor.IsApproved || !doctor.IsAvailable)
            throw new ArgumentException("Doctor not available");

        if (appointmentDate <= DateTime.Now)
            throw new ArgumentException("Appointment date must be in the future");

        // Check availability
        if (!await IsDoctorAvailableAsync(doctorId, appointmentDate))
            throw new InvalidOperationException("Doctor is not available at the requested time");

        var appointment = new Appointment
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentDate = appointmentDate,
            Description = description,
            Status = "Scheduled",
            Fee = doctor.ConsultationFee,
            CreatedAt = DateTime.UtcNow
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        // Load navigation properties
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

        return appointment;
    }

    public async Task<bool> CancelAppointmentAsync(int appointmentId, string userId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment == null)
            return false;

        // Check if user has permission to cancel
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);

        var canCancel = (patient != null && appointment.PatientId == patient.Id) ||
                       (doctor != null && appointment.DoctorId == doctor.Id);

        if (!canCancel)
            return false;

        // Can only cancel future appointments
        if (appointment.AppointmentDate <= DateTime.Now)
            return false;

        appointment.Status = "Cancelled";
        appointment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ConfirmAppointmentAsync(int appointmentId, string userId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment == null)
            return false;

        // Only doctor can confirm appointments
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
        if (doctor == null || appointment.DoctorId != doctor.Id)
            return false;

        if (appointment.Status != "Scheduled")
            return false;

        appointment.Status = "Confirmed";
        appointment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Appointment>> GetUpcomingAppointmentsAsync(string userId, string userRole)
    {
        var query = _context.Appointments
            .Include(a => a.Patient)
            .ThenInclude(p => p.User)
            .Include(a => a.Doctor)
            .ThenInclude(d => d.User)
            .Where(a => a.AppointmentDate > DateTime.Now && a.Status != "Cancelled");

        if (userRole == Roles.Patient)
        {
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
            if (patient != null)
            {
                query = query.Where(a => a.PatientId == patient.Id);
            }
        }
        else if (userRole == Roles.Doctor)
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor != null)
            {
                query = query.Where(a => a.DoctorId == doctor.Id);
            }
        }

        return await query
            .OrderBy(a => a.AppointmentDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetAppointmentHistoryAsync(string userId, string userRole)
    {
        var query = _context.Appointments
            .Include(a => a.Patient)
            .ThenInclude(p => p.User)
            .Include(a => a.Doctor)
            .ThenInclude(d => d.User)
            .Where(a => a.AppointmentDate <= DateTime.Now || a.Status == "Completed" || a.Status == "Cancelled");

        if (userRole == Roles.Patient)
        {
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
            if (patient != null)
            {
                query = query.Where(a => a.PatientId == patient.Id);
            }
        }
        else if (userRole == Roles.Doctor)
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor != null)
            {
                query = query.Where(a => a.DoctorId == doctor.Id);
            }
        }

        return await query
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();
    }

    public async Task<Appointment?> RescheduleAppointmentAsync(int appointmentId, DateTime newDateTime, string userId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment == null)
            return null;

        // Check if user has permission to reschedule
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);

        var canReschedule = (patient != null && appointment.PatientId == patient.Id) ||
                           (doctor != null && appointment.DoctorId == doctor.Id);

        if (!canReschedule)
            return null;

        // Can only reschedule future appointments
        if (appointment.AppointmentDate <= DateTime.Now)
            return null;

        // Check if new time is available
        if (!await IsDoctorAvailableAsync(appointment.DoctorId, newDateTime))
            return null;

        appointment.AppointmentDate = newDateTime;
        appointment.Status = "Rescheduled";
        appointment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return appointment;
    }
}