using backend.Models;
using backend.Data;
using backend.DTO;

namespace backend.Services.Interfaces
{
    public interface IUserService
    {
        Task<string> RegisterPatientAsync(RegisterPatient dto);
        Task<string> RegisterDoctorAsync(RegisterDoctor dto);
        Task<string> RegisterStaffAsync(RegisterStaff dto);
        Task<string> LoginAsync(LoginDTO dto);
    }
}