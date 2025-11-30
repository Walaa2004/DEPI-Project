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


            // Configure Payment -> Appointment (One-to-One)
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Appointment)
                .WithMany() 
                .HasForeignKey(p => p.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Schedule -> Doctor (One-to-One)
            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Doctor)
                .WithMany() 
                .HasForeignKey(s => s.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}