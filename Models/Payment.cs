using System.ComponentModel.DataAnnotations;


namespace WebApplication1.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(5)]
        public string Currency { get; set; } = "EGP"; // Egyptian Pound as default

        [Required]
        public PaymentStatus PaymentStatus { get; set; }

        public DateTime? PaidAt { get; set; }
   
        [MaxLength(100)]
        public string? TransactionId { get; set; }
        
        public int AppointmentId { get; set; }
        public virtual Appointment Appointment { get; set; }
    }

    public enum PaymentStatus
    {
        Pending = 1,
        Completed = 2,
        Failed = 3
    }

 
}