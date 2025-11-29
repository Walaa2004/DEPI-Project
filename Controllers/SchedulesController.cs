using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class SchedulesController : Controller
    {
        // التعديل هنا: إنشاء الاتصال مباشرة
        private readonly DBContext _context = new DBContext();

        // POST: Schedules/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Schedule schedule)
        {
            // التأكد من عدم وجود جدول مواعيد سابق (علاقة One-to-One)
            var existingSchedule = await _context.Schedules
                .FirstOrDefaultAsync(s => s.DoctorId == schedule.DoctorId);

            if (existingSchedule != null)
            {
                // تحديث الموعد الموجود
                existingSchedule.DayOfWeek = schedule.DayOfWeek;
                existingSchedule.Starttime = schedule.Starttime;
                existingSchedule.Endtime = schedule.Endtime;
                existingSchedule.MaxPatientPerDay = schedule.MaxPatientPerDay;
                _context.Update(existingSchedule);
            }
            else
            {
                // إنشاء موعد جديد
                _context.Add(schedule);
            }

            await _context.SaveChangesAsync();

            // العودة لصفحة الدكتور
            return RedirectToAction("Dashboard", "Doctor", new { id = schedule.DoctorId });
        }
    }
}