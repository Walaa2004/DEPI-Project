using System.ComponentModel.DataAnnotations;
// This from seif
namespace WebApplication1.Models
{
    public class Doctor
    {
        [Key]
        public int Doctor_Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PasswordHashed { get; set; }
        public string PhoneNumber { get; set; }
        public int ExperienceYears { get; set; }
        public string LicenseNumber { get; set; }
        public string Specialization { get; set; }
        public string About { get; set; }
        public string ProfilePictureUrl { get; set; }
        public bool AvailableForVideo { get; set; }
        public decimal ConsultationFee { get; set; }
        public decimal OnlineFee { get; set; }
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
    }
}