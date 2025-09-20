using backend.Models;

namespace backend.Services.Interfaces;

public interface IDoctorService
{
    Task<IEnumerable<Doctor>> GetAvailableDoctorsAsync();
    Task<IEnumerable<Doctor>> GetDoctorsBySpecializationAsync(string specialization);
    Task<Doctor?> GetDoctorDetailsAsync(int doctorId);
    Task<bool> UpdateAvailabilityAsync(string userId, bool isAvailable);
    Task<IEnumerable<Doctor>> SearchDoctorsAsync(string? name, string? specialization, int? minExperience);
    Task<IEnumerable<string>> GetSpecializationsAsync();
    Task<bool> ApproveDoctorAsync(int doctorId);
    Task<bool> RejectDoctorAsync(int doctorId);
    Task<IEnumerable<Doctor>> GetPendingDoctorsAsync();
}