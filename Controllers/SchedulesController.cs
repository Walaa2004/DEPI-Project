using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class SchedulesController : Controller
    {
        private readonly DBContext _context = new DBContext();

        // ==========================================
        // SAVE SCHEDULE & AUTO-GENERATE SLOTS
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Schedule schedule)
        {
            // 1. Save the Weekly Schedule Preference (Existing Logic)
            var existingSchedule = await _context.Schedules
                .FirstOrDefaultAsync(s => s.DoctorId == schedule.DoctorId);

            if (existingSchedule != null)
            {
                existingSchedule.DayOfWeek = schedule.DayOfWeek;
                existingSchedule.Starttime = schedule.Starttime;
                existingSchedule.Endtime = schedule.Endtime;
                existingSchedule.MaxPatientPerDay = schedule.MaxPatientPerDay;
                _context.Update(existingSchedule);
            }
            else
            {
                _context.Add(schedule);
            }

            await _context.SaveChangesAsync();

            // 2. TRIGGER: Auto-Generate Appointments for the next 4 Weeks
            await GenerateAppointmentsFromSchedule(schedule);

            return RedirectToAction("Dashboard", "Doctor", new { id = schedule.DoctorId });
        }

        // ==========================================
        // HELPER: Generation Logic
        // ==========================================
        private async Task GenerateAppointmentsFromSchedule(Schedule schedule)
        {
            var doctor = await _context.Doctors.FindAsync(schedule.DoctorId);
            if (doctor == null) return;

            // Calculate Slot Duration based on MaxPatients
            // Example: 480 mins (8 hours) / 10 patients = 48 mins per slot
            double totalMinutes = (schedule.Endtime - schedule.Starttime).TotalMinutes;
            int slotDuration = (int)(totalMinutes / (schedule.MaxPatientPerDay > 0 ? schedule.MaxPatientPerDay : 1));

            // Generate for the next 4 occurrences of this Day (e.g., next 4 Mondays)
            DateTime targetDate = DateTime.Today;
            int daysGenerated = 0;

            while (daysGenerated < 4) // Generate 1 month ahead
            {
                // Find the next date that matches the chosen DayOfWeek
                if (targetDate.DayOfWeek.ToString() == schedule.DayOfWeek)
                {
                    TimeSpan currentSlot = schedule.Starttime;

                    // Create slots for this specific day
                    for (int i = 0; i < schedule.MaxPatientPerDay; i++)
                    {
                        // Stop if we exceed end time
                        if (currentSlot.Add(TimeSpan.FromMinutes(slotDuration)) > schedule.Endtime) break;

                        // Check if slot already exists to prevent duplicates
                        bool exists = await _context.Appointments.AnyAsync(a => 
                            a.DoctorId == schedule.DoctorId && 
                            a.AppointmentDate == targetDate && 
                            a.AppointmentTime == currentSlot);

                        if (!exists)
                        {
                            var newAppointment = new Appointment
                            {
                                DoctorId = schedule.DoctorId,
                                AppointmentDate = targetDate,
                                AppointmentTime = currentSlot,
                                Type = AppointmentType.InPerson, // Default to Clinic
                                Fee = doctor.ConsultationFee,    // Use Doctor's default fee
                                Status = AppointmentStatus.Pending,
                                PatientId = null // Available
                            };
                            _context.Add(newAppointment);
                        }

                        // Move to next slot
                        currentSlot = currentSlot.Add(TimeSpan.FromMinutes(slotDuration));
                    }
                    daysGenerated++;
                }
                targetDate = targetDate.AddDays(1);
            }

            await _context.SaveChangesAsync();
        }
    }
}