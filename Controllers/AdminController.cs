using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class AdminController : Controller
    {
        private readonly DBContext _context = new DBContext();

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (username == "admin123" && password == "admin123")
            {
                return RedirectToAction("Dashboard");
            }
            else
            {
                ViewBag.Error = "Invalid username or password. Please try again.";
                return View();
            }
        }

        public IActionResult Dashboard()
        {
            ViewBag.TotalPatients = _context.Patients.Count();
            ViewBag.TotalDoctors = _context.Doctors.Count();
            ViewBag.TotalAppointments = _context.Appointments.Count();

            var today = DateTime.Today;
            ViewBag.TodayAppointments = _context.Appointments
                .Count(a => a.AppointmentDate.Date == today);

            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(7);
            ViewBag.WeekAppointments = _context.Appointments
                .Count(a => a.AppointmentDate >= startOfWeek && a.AppointmentDate < endOfWeek);

            return View();
        }
        
        public IActionResult Patients()
        {
            var patients = _context.Patients
                .OrderByDescending(p => p.PatientId)
                .ToList();

            return View(patients);
        }

        public IActionResult PatientDetails(int id)
        {
            var patient = _context.Patients.Find(id);

            if (patient == null)
            {
                return NotFound();
            }

            var appointments = _context.Appointments
                .Where(a => a.PatientId == id)
                .OrderByDescending(a => a.AppointmentDate)
                .ToList();

            ViewBag.Appointments = appointments;

            return View(patient);
        }

        [HttpPost]
        public IActionResult DeletePatient(int id)
        {
            var patient = _context.Patients.Find(id);

            if (patient == null)
            {
                return NotFound();
            }

            _context.Patients.Remove(patient);
            _context.SaveChanges();

            return RedirectToAction("Patients");
        }
        public IActionResult Doctors()
        {
            var doctors = _context.Doctors
                .OrderByDescending(d => d.DoctorId)
                .ToList();

            return View(doctors);
        }
        public IActionResult DoctorDetails(int id)
        {
            var doctor = _context.Doctors.Find(id);

            if (doctor == null)
            {
                return NotFound();
            }

            var appointments = _context.Appointments
                .Where(a => a.DoctorId == id)
                .OrderByDescending(a => a.AppointmentDate)
                .ToList();

            var schedule = _context.Schedules
                .FirstOrDefault(s => s.DoctorId == id);

            ViewBag.Appointments = appointments;
            ViewBag.Schedule = schedule;

            return View(doctor);
        }

        [HttpPost]
        public IActionResult ConfirmDoctor(int id)
        {
            var doctor = _context.Doctors.Find(id);

            if (doctor == null)
            {
                return NotFound();
            }

            doctor.IsConfirmed = ConfirmationStatus.Confirmed;
            _context.SaveChanges();

            return RedirectToAction("Doctors");
        }

        [HttpPost]
        public IActionResult RejectDoctor(int id)
        {
            var doctor = _context.Doctors.Find(id);

            if (doctor == null)
            {
                return NotFound();
            }

            doctor.IsConfirmed = ConfirmationStatus.Rejected;
            _context.SaveChanges();

            return RedirectToAction("Doctors");
        }

        [HttpPost]
        public IActionResult DeleteDoctor(int id)
        {
            var doctor = _context.Doctors.Find(id);

            if (doctor == null)
            {
                return NotFound();
            }

            _context.Doctors.Remove(doctor);
            _context.SaveChanges();

            return RedirectToAction("Doctors");
        }
    }
}