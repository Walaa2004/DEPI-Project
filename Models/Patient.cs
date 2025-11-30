using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Patient
    {
        // by convention , dataanotation, fluent api


        [Key]
        public int PatientId { get; set; }
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
        [Required]
        public int Age { get; set; }
        [Required]
        public string Gender { get; set; }
        public string? BloodGroup { get; set; }

        [Column(TypeName = "decimal(6,2)")]
        public decimal? Height { get; set; }

        [Column(TypeName = "decimal(6,2)")]
        public decimal? Weight { get; set; }
        public string Allergies { get; set; }
        public string Address { get; set; }
        public ICollection<Appointment> Appointments { get; set; }
     
     
    }
}

