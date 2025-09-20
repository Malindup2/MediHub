using backend.Data;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Implementations;

public class DoctorService : IDoctorService
{
    private readonly AppDbContext _context;

    public DoctorService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Doctor>> GetAvailableDoctorsAsync()
    {
        return await _context.Doctors
            .Include(d => d.User)
            .Where(d => d.IsApproved && d.IsAvailable)
            .OrderBy(d => d.User.FirstName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Doctor>> GetDoctorsBySpecializationAsync(string specialization)
    {
        return await _context.Doctors
            .Include(d => d.User)
            .Where(d => d.IsApproved && d.IsAvailable && 
                       d.Specialization.ToLower().Contains(specialization.ToLower()))
            .OrderBy(d => d.User.FirstName)
            .ToListAsync();
    }

    public async Task<Doctor?> GetDoctorDetailsAsync(int doctorId)
    {
        return await _context.Doctors
            .Include(d => d.User)
            .Include(d => d.Appointments)
            .ThenInclude(a => a.Patient)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(d => d.Id == doctorId && d.IsApproved);
    }

    public async Task<bool> UpdateAvailabilityAsync(string userId, bool isAvailable)
    {
        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null || !doctor.IsApproved)
            return false;

        doctor.IsAvailable = isAvailable;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Doctor>> SearchDoctorsAsync(string? name, string? specialization, int? minExperience)
    {
        var query = _context.Doctors
            .Include(d => d.User)
            .Where(d => d.IsApproved && d.IsAvailable);

        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(d => (d.User.FirstName + " " + d.User.LastName)
                .ToLower().Contains(name.ToLower()));
        }

        if (!string.IsNullOrEmpty(specialization))
        {
            query = query.Where(d => d.Specialization.ToLower().Contains(specialization.ToLower()));
        }

        if (minExperience.HasValue)
        {
            query = query.Where(d => d.ExperienceYears >= minExperience.Value);
        }

        return await query
            .OrderBy(d => d.User.FirstName)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetSpecializationsAsync()
    {
        return await _context.Doctors
            .Where(d => d.IsApproved)
            .Select(d => d.Specialization)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();
    }

    public async Task<bool> ApproveDoctorAsync(int doctorId)
    {
        var doctor = await _context.Doctors.FindAsync(doctorId);
        if (doctor == null || doctor.IsApproved)
            return false;

        doctor.IsApproved = true;
        doctor.IsAvailable = true; // Make available by default when approved
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectDoctorAsync(int doctorId)
    {
        var doctor = await _context.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == doctorId);

        if (doctor == null || doctor.IsApproved)
            return false;

        // Remove doctor and associated user account
        _context.Doctors.Remove(doctor);
        _context.Users.Remove(doctor.User);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Doctor>> GetPendingDoctorsAsync()
    {
        return await _context.Doctors
            .Include(d => d.User)
            .Where(d => !d.IsApproved)
            .OrderBy(d => d.User.FirstName)
            .ToListAsync();
    }
}