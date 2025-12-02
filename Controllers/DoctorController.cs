using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using System.Security.Cryptography;
using System.Text;

namespace WebApplication1.Controllers
{
    public class DoctorController : Controller
    {
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
            // FIX 1: Ignore validation for properties not in the form
            ModelState.Remove("Appointments");
            ModelState.Remove("Clinic");
            ModelState.Remove("Schedule"); // <--- This was missing!

            if (ModelState.IsValid)
            {
                // FIX 2: Hash the password before saving
                doctor.PasswordHashed = HashPassword(doctor.PasswordHashed);

                // Set default status
                doctor.IsConfirmed = ConfirmationStatus.Pending;

                _context.Add(doctor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Login));
            }

            // If we get here, something failed.
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
        // 2. LOGIN (POST) - Updated with Confirmation Check
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.Email == email);

            // Hash the input password to compare with the DB
            string inputHash = HashPassword(password);

            // 1. Check if User Exists and Password is Correct
            if (doctor == null || doctor.PasswordHashed != inputHash)
            {
                ViewBag.Error = "Invalid Username or Password.";
                return View();
            }

            // 2. CHECK STATUS: Block Login if Pending
            if (doctor.IsConfirmed == ConfirmationStatus.Pending)
            {
                ViewBag.Error = "Access Denied: Your account is currently Pending approval from the Admin.";
                return View();
            }

            // 3. CHECK STATUS: Block Login if Rejected (Optional but recommended)
            if (doctor.IsConfirmed == ConfirmationStatus.Rejected)
            {
                ViewBag.Error = "Access Denied: Your registration was Rejected by the Admin.";
                return View();
            }

            // 4. If Confirmed, Allow Login
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

            // Fetch schedule manually
            ViewBag.CurrentSchedule = await _context.Schedules.FirstOrDefaultAsync(s => s.DoctorId == id);

            return View(doctor);
        }

        // ==========================================
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

            ModelState.Remove("Appointments");
            ModelState.Remove("Clinic");
            ModelState.Remove("Schedule"); // Ignore validation

            if (ModelState.IsValid)
            {
                try
                {
                    var existingDoctor = await _context.Doctors.FindAsync(id);
                    if (existingDoctor == null) return NotFound();

                    // Update fields
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

        // ==========================================
        // 6. MANAGE APPOINTMENTS (List Created Slots)
        // ==========================================
        public async Task<IActionResult> ManageAppointments(int id)
        {
            // View all appointments created by this doctor (Booked and Empty)
            var appointments = await _context.Appointments
                .Include(a => a.Patient) // Include patient to see who booked
                .Where(a => a.DoctorId == id)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .ToListAsync();

            ViewBag.DoctorId = id;
            return View(appointments);
        }

        // ==========================================
        // 7. CREATE SLOT (GET)
        // ==========================================
        public IActionResult CreateSlot(int doctorId)
        {
            ViewBag.DoctorId = doctorId;
            return View();
        }

        // ==========================================
        // 8. CREATE SLOT (POST) - The Key Logic
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSlot(Appointment appointment)
        {
            // 1. Remove Patient Validation (Since we are creating an empty slot)
            ModelState.Remove("Patient");
            ModelState.Remove("PatientId");
            ModelState.Remove("Clinic");
            ModelState.Remove("Doctor");

            if (ModelState.IsValid)
            {
                // 2. FORCE PatientId to be NULL (This makes it appear in the Patient's view)
                appointment.PatientId = null;

                // 3. Set Default Status to Pending
                appointment.Status = AppointmentStatus.Pending;

                // 4. Ensure Doctor ID is set
                // (Assuming hidden field passes DoctorId, or get from User.Identity)

                _context.Add(appointment);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Appointment slot created successfully! Patients can now book this.";
                return RedirectToAction(nameof(ManageAppointments), new { id = appointment.DoctorId });
            }

            ViewBag.DoctorId = appointment.DoctorId;
            return View(appointment);
        }

        // ==========================================
        // HELPER: Simple SHA256 Password Hasher
        // ==========================================
        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return "";

            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }
    }
}