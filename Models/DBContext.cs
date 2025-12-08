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
            //optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=DepiDB;Trusted_Connection=True;TrustServerCertificate=true;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================================================
            // 1. Doctor <-> Schedule (One-to-One)
            // =========================================================
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.Schedule)       // Doctor has one Schedule
                .WithOne(s => s.Doctor)        // Schedule belongs to one Doctor
                .HasForeignKey<Schedule>(s => s.DoctorId) // The FK is inside Schedule
                .OnDelete(DeleteBehavior.Cascade);

            // =========================================================
            // 2. Appointment <-> Payment (One-to-One)
            // =========================================================
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Payment)
                .WithOne(p => p.Appointment)
                .HasForeignKey<Payment>(p => p.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Appointment>()
               .HasOne(a => a.VideoCallSession)
               .WithOne(v => v.Appointment)
               .HasForeignKey<VideoCallSession>(v => v.AppointmentId)
               .OnDelete(DeleteBehavior.Cascade);
            // =========================================================
            // 3. Money Precision Fixes (To prevent warnings)
            // =========================================================
            //modelBuilder.Entity<Doctor>().Property(d => d.ConsultationFee).HasColumnType("decimal(18,2)");
            //modelBuilder.Entity<Doctor>().Property(d => d.OnlineFee).HasColumnType("decimal(18,2)");
            //modelBuilder.Entity<Appointment>().Property(a => a.Fee).HasColumnType("decimal(18,2)");
            //modelBuilder.Entity<Payment>().Property(p => p.Amount).HasColumnType("decimal(18,2)");
        }
    }
}