using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Doctor
    {
        [Key]
        public int DoctorId { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string PasswordHashed { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        public int ExperienceYears { get; set; }
        [Required]
        [MinLength(14)]
        public string LicenseNumber { get; set; }
        public string Specialization { get; set; }
        public string About { get; set; }
        public bool AvailableForVideo { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ConsultationFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OnlineFee { get; set; }
        public string Address { get; set; }
        public string Gender { get; set; }
        [Required]
        public int Age { get; set; }

        // **FIX: Add the isConfirmed property**
        public ConfirmationStatus IsConfirmed { get; set; } = ConfirmationStatus.Pending;

        public ICollection<Appointment> Appointments { get; set; }
        public int? ClinicId { get; set; }
        public Clinic Clinic { get; set; }
        public Schedule? Schedule { get; set; }

    }

    // **FIX: Move enum outside the Doctor **
    public enum ConfirmationStatus
    {
        Pending = 1,
        Confirmed = 2,
        Cancelled = 3
    }
}