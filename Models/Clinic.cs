using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Clinic
    {
        [Key]
        public int ClinicId { get; set; }

        [Required]
        [MaxLength(200)]
        public string ClinicName { get; set; }

        [Required]
        [MaxLength(100)]
        public string City { get; set; }

        [MaxLength(200)]
        public string? OpeningHours { get; set; }
        [Required]
        public string Address{ get; set; }
        [Required]
        public string PhoneNumber { get; set; } 
        public ICollection<Doctor> Doctors { get; set; }
        public ICollection<Appointment> Appointments { get; set; }



   
    }
}