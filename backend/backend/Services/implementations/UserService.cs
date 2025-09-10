using backend.Constants;
using backend.Data;
using backend.Models;
using backend.DTO;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Implementations;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _dbContext;
    private readonly JwtTokenService _jwtTokenService;

    public UserService(UserManager<ApplicationUser> userManager, AppDbContext dbContext, JwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<string> RegisterPatientAsync(RegisterPatient dto)
    {
        if (await _userManager.FindByEmailAsync(dto.Email) != null)
            throw new Exception("Email already exists.");

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, Roles.Patient);

        var patient = new Patient
        {
            UserId = user.Id,
            PhoneNumber = dto.PhoneNumber,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender
        };

        _dbContext.Patients.Add(patient);
        await _dbContext.SaveChangesAsync();

        return "Patient registered successfully.";
    }

    public async Task<string> RegisterDoctorAsync(RegisterDoctor dto)
    {
        if (await _userManager.FindByEmailAsync(dto.Email) != null)
            throw new Exception("Email already exists.");

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, Roles.Doctor);

        var doctor = new Doctor
        {
            UserId = user.Id,
            Specialization = dto.Specialization,
            LicenseNumber = dto.LicenseNumber,
            ExperienceYears = dto.ExperienceYears,
            IsApproved = false
        };

        _dbContext.Doctors.Add(doctor);
        await _dbContext.SaveChangesAsync();

        return "Doctor application submitted for approval. Please wait for activation by staff.";



    }

    public async Task<string> RegisterStaffAsync(RegisterStaff dto)
    {
        if (await _userManager.FindByEmailAsync(dto.Email) != null)
            throw new Exception("Email already exists.");

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, Roles.Staff);

        var staff = new Staff
        {
            UserId = user.Id,
            Department = dto.Department,
            EmployeeId = dto.EmployeeId
        };

        _dbContext.Staff.Add(staff);
        await _dbContext.SaveChangesAsync();

        return "Staff registered successfully.";
    }

    public async Task<string> LoginAsync(LoginDTO dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            throw new Exception("Invalid email or password.");

        var result = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!result)
            throw new Exception("Invalid email or password.");

        var token = _jwtTokenService.GenerateToken(user);
        return token;
    }
}
