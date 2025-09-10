using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Appointment> Appointments { get; set; } = null!;
    public DbSet<Doctor> Doctors { get; set; } = null!;
    public DbSet<Staff> Staff { get; set; } = null!;
    public DbSet<Patient> Patients { get; set; } = null!;


    protected override void OnModelCreating(ModelBuilder builder)
    {
        
        base.OnModelCreating(builder);

        //table mappings
        builder.Entity<Doctor>().ToTable("Doctors");
        builder.Entity<Staff>().ToTable("Staffs");
        builder.Entity<Patient>().ToTable("Patients");
        builder.Entity<Appointment>().ToTable("Appointments");

    }
}