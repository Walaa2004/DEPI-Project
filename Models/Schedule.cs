using System.ComponentModel.DataAnnotations;
namespace WebApplication1.Models
{
    public class Schedule
    {
        [Key]
        public int ScheduleId { get; set; }

        public string DayOfWeek { get; set; }

        public int MaxPatientPerDay { get; set; }


        public TimeSpan Starttime { get; set; }

        public TimeSpan Endtime { get; set; }

        public int DoctorId { get; set; }

        public Doctor Doctor { get; set; }
    }
}
