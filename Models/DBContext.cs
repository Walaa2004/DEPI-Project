using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Models
{
    public class DBContext : DbContext
    {
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Clinic> Clinics { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<VideoCallSession> VideoCallSessions { get; set; }
        public DbSet<Payment> Payments { get; set; }
       protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=NISREEN;Database=DepiDB;Trusted_Connection=True;TrustServerCertificate=true;");

            //optionsBuilder.UseSqlServer("Server=NAREMAN-ADEL\\SQLEXPRESS;Database=DepiDB;Trusted_Connection=True;TrustServerCertificate=true;");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure one-to-one: Payment is the dependent with FK Payment.AppointmentId
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Payment)
                .WithOne(p => p.Appointment)
                .HasForeignKey<Payment>(p => p.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);
            // Configure one-to-one: Schedule is dependent with FK Schedule.DoctorId
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.Schedule)
                .WithOne(s => s.Doctor)
                .HasForeignKey<Schedule>(s => s.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);
        }

    }
}
