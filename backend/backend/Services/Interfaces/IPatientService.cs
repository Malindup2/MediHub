using backend.Models;

namespace backend.Services.Interfaces;

public interface IPatientService
{
    Task<Patient?> GetPatientProfileAsync(string userId);
    Task<Patient?> GetPatientByIdAsync(int patientId);
    Task<bool> UpdatePatientProfileAsync(string userId, Patient updatedProfile);
    Task<IEnumerable<Appointment>> GetPatientAppointmentsAsync(string userId);
    Task<IEnumerable<MedicalRecord>> GetPatientMedicalRecordsAsync(string userId);
    Task<IEnumerable<Patient>> SearchPatientsAsync(string? name, string? email);
    Task<object> GetPatientDashboardAsync(string userId);
}