using backend.Data;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Implementations;

public class PatientService : IPatientService
{
    private readonly AppDbContext _context;

    public PatientService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Patient?> GetPatientProfileAsync(string userId)
    {
        return await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<Patient?> GetPatientByIdAsync(int patientId)
    {
        return await _context.Patients
            .Include(p => p.User)
            .Include(p => p.Appointments)
            .ThenInclude(a => a.Doctor)
            .ThenInclude(d => d.User)
            .Include(p => p.MedicalRecords)
            .ThenInclude(mr => mr.Doctor)
            .ThenInclude(d => d.User)
            .FirstOrDefaultAsync(p => p.Id == patientId);
    }

    public async Task<bool> UpdatePatientProfileAsync(string userId, Patient updatedProfile)
    {
        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
            return false;

        // Update patient fields
        patient.PhoneNumber = updatedProfile.PhoneNumber;
        patient.DateOfBirth = updatedProfile.DateOfBirth;
        patient.Gender = updatedProfile.Gender;
        patient.Address = updatedProfile.Address;
        patient.EmergencyContact = updatedProfile.EmergencyContact;
        patient.BloodType = updatedProfile.BloodType;
        patient.Allergies = updatedProfile.Allergies;

        // Update user fields
        patient.User.FirstName = updatedProfile.User.FirstName;
        patient.User.LastName = updatedProfile.User.LastName;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Appointment>> GetPatientAppointmentsAsync(string userId)
    {
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
            return new List<Appointment>();

        return await _context.Appointments
            .Include(a => a.Doctor)
            .ThenInclude(d => d.User)
            .Where(a => a.PatientId == patient.Id)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<MedicalRecord>> GetPatientMedicalRecordsAsync(string userId)
    {
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
            return new List<MedicalRecord>();

        return await _context.MedicalRecords
            .Include(mr => mr.Doctor)
            .ThenInclude(d => d.User)
            .Include(mr => mr.Appointment)
            .Where(mr => mr.PatientId == patient.Id && !mr.IsPrivate)
            .OrderByDescending(mr => mr.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Patient>> SearchPatientsAsync(string? name, string? email)
    {
        var query = _context.Patients
            .Include(p => p.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(p => (p.User.FirstName + " " + p.User.LastName)
                .ToLower().Contains(name.ToLower()));
        }

        if (!string.IsNullOrEmpty(email))
        {
            query = query.Where(p => p.User.Email!.ToLower().Contains(email.ToLower()));
        }

        return await query
            .OrderBy(p => p.User.FirstName)
            .Take(50) // Limit results
            .ToListAsync();
    }

    public async Task<object> GetPatientDashboardAsync(string userId)
    {
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
            return new { };

        var today = DateTime.Today;
        var upcoming30Days = today.AddDays(30);

        var upcomingAppointments = await _context.Appointments
            .Include(a => a.Doctor)
            .ThenInclude(d => d.User)
            .Where(a => a.PatientId == patient.Id && 
                       a.AppointmentDate >= today &&
                       a.AppointmentDate <= upcoming30Days &&
                       a.Status != "Cancelled")
            .OrderBy(a => a.AppointmentDate)
            .Take(5)
            .Select(a => new 
            {
                a.Id,
                a.AppointmentDate,
                a.Status,
                DoctorName = $"{a.Doctor.User.FirstName} {a.Doctor.User.LastName}",
                a.Doctor.Specialization
            })
            .ToListAsync();

        var recentMedicalRecords = await _context.MedicalRecords
            .Include(mr => mr.Doctor)
            .ThenInclude(d => d.User)
            .Where(mr => mr.PatientId == patient.Id && !mr.IsPrivate)
            .OrderByDescending(mr => mr.CreatedAt)
            .Take(5)
            .Select(mr => new 
            {
                mr.Id,
                mr.Title,
                mr.CreatedAt,
                DoctorName = $"{mr.Doctor.User.FirstName} {mr.Doctor.User.LastName}",
                mr.Doctor.Specialization
            })
            .ToListAsync();

        var stats = new
        {
            TotalAppointments = await _context.Appointments
                .CountAsync(a => a.PatientId == patient.Id),
            CompletedAppointments = await _context.Appointments
                .CountAsync(a => a.PatientId == patient.Id && a.Status == "Completed"),
            UpcomingAppointments = await _context.Appointments
                .CountAsync(a => a.PatientId == patient.Id && 
                               a.AppointmentDate >= today &&
                               a.Status != "Cancelled"),
            MedicalRecords = await _context.MedicalRecords
                .CountAsync(mr => mr.PatientId == patient.Id && !mr.IsPrivate)
        };

        return new
        {
            Stats = stats,
            UpcomingAppointments = upcomingAppointments,
            RecentMedicalRecords = recentMedicalRecords
        };
    }
}