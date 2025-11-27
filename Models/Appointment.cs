using System.ComponentModel.DataAnnotations;


namespace WebApplication1.Models
{
    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public TimeSpan AppointmentTime { get; set; }

        [Required]
        public AppointmentType Type { get; set; }

        [Required]
        public AppointmentStatus Status { get; set; }

        [MaxLength(500)]
        public string? Symptoms { get; set; }

        [MaxLength(1000)]
        public string? PerceptionNotes { get; set; }

        [Required]
        public decimal Fee { get; set; }


        // Navigation Properties
        public int PatientId { get; set; }
        public Patient Patient { get; set; }

        public int? DoctorId { get; set; }
        public Doctor Doctor { get; set; }
        public int? ClinicId { get; set; }
        public Clinic Clinic { get; set; }
        public int? PaymentId { get; set; }
        public Payment? Payment { get; set; }
        public int SessionId { get; set; }
        public VideoCallSession? VideoCallSession { get; set; }

    
    }

    public enum AppointmentType
    {
        Video = 1,
        InPerson = 2
    }

    public enum AppointmentStatus
    {
        Pending = 1,
        Confirmed = 2,
        Completed = 3,
        Cancelled = 4
    }
}