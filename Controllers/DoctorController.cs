using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class DoctorController : Controller
    {
        // Connection to Database
        private readonly DBContext _context = new DBContext();

        // ==========================================
        // 1. REGISTER (GET)
        // ==========================================
        public IActionResult Register()
        {
            return View();
        }

        // ==========================================
        // 1. REGISTER (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Doctor doctor)
        {
            // FIX: Removed "Schedule" validation because the property is gone
            ModelState.Remove("Appointments");
            ModelState.Remove("Clinic");

            if (ModelState.IsValid)
            {
                doctor.IsConfirmed = ConfirmationStatus.Pending;
                _context.Add(doctor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Login));
            }
            return View(doctor);
        }

        // ==========================================
        // 2. LOGIN (GET)
        // ==========================================
        public IActionResult Login()
        {
            return View();
        }

        // ==========================================
        // 2. LOGIN (POST)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            // FIX: Removed .Include(d => d.Schedule) because the relationship was removed from the model
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.Email == email);

            if (doctor == null || doctor.PasswordHashed != password)
            {
                ViewBag.Error = "Invalid Username or Password.";
                return View();
            }

            return RedirectToAction("Dashboard", new { id = doctor.DoctorId });
        }

        // ==========================================
        // 3. DASHBOARD
        // ==========================================
        public async Task<IActionResult> Dashboard(int? id)
        {
            if (id == null) return NotFound();

            var doctor = await _context.Doctors.FirstOrDefaultAsync(m => m.DoctorId == id);

            if (doctor == null) return NotFound();

            // FIX: Fetch the schedule manually and put it in ViewBag
            ViewBag.CurrentSchedule = await _context.Schedules.FirstOrDefaultAsync(s => s.DoctorId == id);

            return View(doctor);
        }        // ==========================================
        // 4. EDIT PROFILE (GET)
        // ==========================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return NotFound();

            return View(doctor);
        }

        // ==========================================
        // 5. EDIT PROFILE (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Doctor doctor)
        {
            if (id != doctor.DoctorId) return NotFound();

            // FIX: Removed "Schedule" validation
            ModelState.Remove("Appointments");
            ModelState.Remove("Clinic");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingDoctor = await _context.Doctors.FindAsync(id);

                    if (existingDoctor == null) return NotFound();

                    // Update allowed fields
                    existingDoctor.FirstName = doctor.FirstName;
                    existingDoctor.LastName = doctor.LastName;
                    existingDoctor.PhoneNumber = doctor.PhoneNumber;
                    existingDoctor.Age = doctor.Age;
                    existingDoctor.Gender = doctor.Gender;

                    existingDoctor.Specialization = doctor.Specialization;
                    existingDoctor.ExperienceYears = doctor.ExperienceYears;
                    existingDoctor.LicenseNumber = doctor.LicenseNumber;
                    existingDoctor.About = doctor.About;
                    existingDoctor.Address = doctor.Address;

                    existingDoctor.AvailableForVideo = doctor.AvailableForVideo;
                    existingDoctor.ConsultationFee = doctor.ConsultationFee;
                    existingDoctor.OnlineFee = doctor.OnlineFee;

                    _context.Update(existingDoctor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Doctors.Any(e => e.DoctorId == doctor.DoctorId)) return NotFound();
                    else throw;
                }

                return RedirectToAction("Dashboard", new { id = doctor.DoctorId });
            }
            return View(doctor);
        }
    }
}