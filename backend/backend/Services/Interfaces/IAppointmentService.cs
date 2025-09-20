using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Interfaces;

public interface IAppointmentService
{
    Task<bool> IsDoctorAvailableAsync(int doctorId, DateTime appointmentDate);
    Task<IEnumerable<DateTime>> GetAvailableTimeSlotsAsync(int doctorId, DateTime date);
    Task<Appointment> ScheduleAppointmentAsync(int patientId, int doctorId, DateTime appointmentDate, string description);
    Task<bool> CancelAppointmentAsync(int appointmentId, string userId);
    Task<bool> ConfirmAppointmentAsync(int appointmentId, string userId);
    Task<IEnumerable<Appointment>> GetUpcomingAppointmentsAsync(string userId, string userRole);
    Task<IEnumerable<Appointment>> GetAppointmentHistoryAsync(string userId, string userRole);
    Task<Appointment?> RescheduleAppointmentAsync(int appointmentId, DateTime newDateTime, string userId);
}