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
            //optionsBuilder.UseSqlServer("Server=NISREEN;Database=DepiDB;Trusted_Connection=True;TrustServerCertificate=true;");
            // optionsBuilder.UseSqlServer("Server=NAREMAN-ADEL\\SQLEXPRESS;Database=DepiDB;Trusted_Connection=True;TrustServerCertificate=true;");
            optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=DepiDB;Trusted_Connection=True;TrustServerCertificate=true;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Patient -> Appointments (One-to-Many)
            modelBuilder.Entity<Patient>()
                .HasMany(p => p.Appointments)
                .WithOne(a => a.Patient)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Doctor -> Appointments (One-to-Many)
            modelBuilder.Entity<Doctor>()
                .HasMany(d => d.Appointments)
                .WithOne(a => a.Doctor)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.SetNull); // Set to null if doctor is deleted

            // Configure Clinic -> Appointments (One-to-Many)
            modelBuilder.Entity<Clinic>()
                .HasMany(c => c.Appointments)
                .WithOne(a => a.Clinic)
                .HasForeignKey(a => a.ClinicId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Clinic -> Doctors (One-to-Many)
            modelBuilder.Entity<Clinic>()
                .HasMany(c => c.Doctors)
                .WithOne(d => d.Clinic)
                .HasForeignKey(d => d.ClinicId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Payment -> Appointment (One-to-One)
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Appointment)
                .WithMany() // Appointment doesn't have Payment navigation property
                .HasForeignKey(p => p.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Schedule -> Doctor (One-to-One)
            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Doctor)
                .WithMany() // Doctor doesn't have Schedule navigation property
                .HasForeignKey(s => s.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure VideoCallSession -> Appointment (One-to-One)
            modelBuilder.Entity<VideoCallSession>()
                .HasOne(v => v.Appointment)
                .WithOne(a => a.VideoCallSession)
                .HasForeignKey<VideoCallSession>(v => v.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure enum conversions to store as integers
            modelBuilder.Entity<Payment>()
                .Property(p => p.PaymentStatus)
                .HasConversion<int>();

            modelBuilder.Entity<Appointment>()
                .Property(a => a.Type)
                .HasConversion<int>();

            modelBuilder.Entity<Appointment>()
                .Property(a => a.Status)
                .HasConversion<int>();

            modelBuilder.Entity<VideoCallSession>()
                .Property(v => v.Status)
                .HasConversion<int>();

            modelBuilder.Entity<VideoCallSession>()
                .Property(v => v.SessionType)
                .HasConversion<int>();

            modelBuilder.Entity<Doctor>()
                .Property(d => d.IsConfirmed)
                .HasConversion<int>();

            // Configure decimal precision for monetary values
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Appointment>()
                .Property(a => a.Fee)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Doctor>()
                .Property(d => d.ConsultationFee)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Doctor>()
                .Property(d => d.OnlineFee)
                .HasPrecision(10, 2);

            // Configure decimal precision for patient measurements
            modelBuilder.Entity<Patient>()
                .Property(p => p.Height)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Patient>()
                .Property(p => p.Weight)
                .HasPrecision(5, 2);

            // Configure unique constraints
            modelBuilder.Entity<Patient>()
                .HasIndex(p => p.Email)
                .IsUnique();

            modelBuilder.Entity<Doctor>()
                .HasIndex(d => d.Email)
                .IsUnique();

            modelBuilder.Entity<Doctor>()
                .HasIndex(d => d.LicenseNumber)
                .IsUnique();

            // Configure default values
            modelBuilder.Entity<Payment>()
                .Property(p => p.Currency)
                .HasDefaultValue("EGP");

            modelBuilder.Entity<Doctor>()
                .Property(d => d.IsConfirmed)
                .HasDefaultValue(ConfirmationStatus.Pending);

            modelBuilder.Entity<Doctor>()
                .Property(d => d.AvailableForVideo)
                .HasDefaultValue(false);
        }
    }
}