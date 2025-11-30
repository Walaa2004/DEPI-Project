using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class PatientController : Controller
    {
        DBContext db;

        public PatientController()
        {
            db = new DBContext();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(Patient patient)
        {
            try
            {
                // Check if email already exists
                var existingPatient = db.Patients.FirstOrDefault(p => p.Email == patient.Email);
                if (existingPatient != null)
                {
                    ViewBag.Error = "Email already registered. Please use a different email or login.";
                    return View(patient);
                }

                // Hash password before saving
                patient.PasswordHashed = BCrypt.Net.BCrypt.HashPassword(patient.PasswordHashed);

                db.Patients.Add(patient);
                db.SaveChanges();

                ViewBag.Success = "Registration successful! Please login with your credentials.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Registration failed: {ex.Message}";
                return View(patient);
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            try
            {
                // Check if parameters are received
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    ViewBag.Error = "Email and password are required.";
                    return View();
                }

                var patient = db.Patients.FirstOrDefault(p => p.Email == email);

                if (patient == null)
                {
                    ViewBag.Error = "No account found with this email address.";
                    return View();
                }

                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, patient.PasswordHashed);

                if (isPasswordValid)
                {
                    return RedirectToAction("Profile", new { id = patient.PatientId });
                }
                else
                {
                    ViewBag.Error = "Invalid password. Please try again.";
                    return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "An error occurred during login.";
                return View();
            }
        }

        [Route("Patient/Profile/{id:int}")]
        public IActionResult Profile(int id)
        {
            // Fetch patient from database
            var patient = db.Patients.Find(id);

            if (patient == null)
            {
                ViewBag.Error = "Patient not found.";
                return RedirectToAction("Login");
            }

            return View(patient);
        }

        [Route("Patient/Payment/{id:int}")]
        public IActionResult Payment(int id)
        {
            try
            {
                // Get patient with appointments, payments, and related data
                var patient = db.Patients
                    .Include(p => p.Appointments)
                        .ThenInclude(a => a.Payment)
                    .Include(p => p.Appointments)
                        .ThenInclude(a => a.Doctor)
                    .Include(p => p.Appointments)
                        .ThenInclude(a => a.Clinic)
                    .FirstOrDefault(p => p.PatientId == id);

                if (patient == null)
                {
                    return RedirectToAction("Login");
                }

                // Get payments from patient's appointments with all related data
                var payments = patient.Appointments
                    .Where(a => a.Payment != null)
                    .Select(a => a.Payment)
                    .OrderByDescending(p => p.PaidAt ?? DateTime.MinValue)
                    .ThenByDescending(p => p.PaymentId)
                    .ToList();

                //// Calculate summary statistics and pass via ViewBag
                ViewBag.Patient = patient;
                return View(payments);
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                ViewBag.Error = $"An error occurred while loading payment data: {ex.Message}";
                return RedirectToAction("Profile", new { id = id });
            }
        }

        [Route("Patient/AppointmentHistory/{id:int}")]
        public IActionResult AppointmentHistory(int id)
        {
            var patient = db.Patients.Find(id);
            if (patient == null)
            {
                return RedirectToAction("Login");
            }

            // Get patient's past appointments with related data
            var appointmentHistory = db.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Clinic)
                .Include(a => a.Payment)
                .Where(a => a.PatientId == id)
                .Where(a => a.AppointmentDate < DateTime.Now)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToList();

            ViewBag.Patient = patient;
            ViewBag.TotalAppointments = appointmentHistory.Count;
            ViewBag.CompletedAppointments = appointmentHistory.Count(a => a.Status == AppointmentStatus.Completed);
            ViewBag.CancelledAppointments = appointmentHistory.Count(a => a.Status == AppointmentStatus.Cancelled);

            return View(appointmentHistory);
        }

        [Route("Patient/BookAppointment/{id:int}")]
        public IActionResult BookAppointment(int id)
        {
            var patient = db.Patients.Find(id);
            if (patient == null)
            {
                return RedirectToAction("Login");
            }

            // Load available doctors with their clinics
            var doctors = db.Doctors
                .Include(d => d.Clinic)
               .Include(d => d.Schedule)
                .Where(d => d.IsConfirmed == ConfirmationStatus.Confirmed)
                .OrderBy(d => d.Specialization)
                .ThenBy(d => d.FirstName)
                .ToList();

            // Load all clinics for location selection
            var clinics = db.Clinics
                .OrderBy(c => c.City)
                .ThenBy(c => c.ClinicName)
                .ToList();

            ViewBag.Patient = patient;
            ViewBag.Doctors = doctors;
            ViewBag.Clinics = clinics;
            ViewBag.AppointmentTypes = Enum.GetValues(typeof(AppointmentType)).Cast<AppointmentType>().ToList();
            ViewBag.MinDate = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"); // Tomorrow
            ViewBag.MaxDate = DateTime.Today.AddMonths(3).ToString("yyyy-MM-dd"); // 3 months ahead

            return View(new Appointment { PatientId = id });
        }

        [HttpPost]
        [Route("Patient/BookAppointment/{id:int}")]
        public IActionResult BookAppointment(int id, Appointment appointment, string selectedTimeSlot)
        {
            try
            {
                var patient = db.Patients.Find(id);
                if (patient == null)
                {
                    return RedirectToAction("Login");
                }

                // Validate appointment data
                if (!ModelState.IsValid)
                {
                    // Reload data for the form
                    ReloadBookAppointmentData(id);
                    return View(appointment);
                }

                // Parse the selected time slot
                if (TimeSpan.TryParse(selectedTimeSlot, out TimeSpan appointmentTime))
                {
                    appointment.AppointmentTime = appointmentTime;
                }
                else
                {
                    ModelState.AddModelError("", "Please select a valid time slot.");
                    ReloadBookAppointmentData(id);
                    return View(appointment);
                }

                // Check if the time slot is available
                var existingAppointment = db.Appointments
                    .FirstOrDefault(a => a.DoctorId == appointment.DoctorId &&
                                       a.AppointmentDate.Date == appointment.AppointmentDate.Date &&
                                       a.AppointmentTime == appointment.AppointmentTime &&
                                       a.Status != AppointmentStatus.Cancelled);

                if (existingAppointment != null)
                {
                    ModelState.AddModelError("", "This time slot is already booked. Please select another time.");
                    ReloadBookAppointmentData(id);
                    return View(appointment);
                }

                // Set appointment details
                appointment.PatientId = id;
                appointment.Status = AppointmentStatus.Pending;

                // Set clinic based on appointment type
                if (appointment.Type == AppointmentType.InPerson && appointment.DoctorId.HasValue)
                {
                    var doctor = db.Doctors.Include(d => d.Clinic).FirstOrDefault(d => d.DoctorId == appointment.DoctorId);
                    appointment.ClinicId = doctor?.ClinicId;
                    appointment.Fee = doctor?.ConsultationFee ?? 0;
                }
                else if (appointment.Type == AppointmentType.Video)
                {
                    appointment.ClinicId = null; // No clinic for video calls
                    var doctor = db.Doctors.FirstOrDefault(d => d.DoctorId == appointment.DoctorId);
                    appointment.Fee = doctor?.OnlineFee ?? 0;
                }

                // Generate a unique session ID for video calls
                if (appointment.Type == AppointmentType.Video)
                {
                    appointment.SessionId = new Random().Next(100000, 999999);
                }

                db.Appointments.Add(appointment);
                db.SaveChanges();

                ViewBag.Success = "Appointment booked successfully!";
                return RedirectToAction("UpcomingAppointments", new { id = id });
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Failed to book appointment: {ex.Message}";
                ReloadBookAppointmentData(id);
                return View(appointment);
            }
        }

        private void ReloadBookAppointmentData(int patientId)
        {
            var patient = db.Patients.Find(patientId);
            var doctors = db.Doctors
                .Include(d => d.Clinic)
                .Include(d => d.Schedule)
                .Where(d => d.IsConfirmed == ConfirmationStatus.Confirmed)
                .OrderBy(d => d.Specialization)
                .ThenBy(d => d.FirstName)
                .ToList();
            var clinics = db.Clinics.OrderBy(c => c.City).ThenBy(c => c.ClinicName).ToList();

            ViewBag.Patient = patient;
            ViewBag.Doctors = doctors;
            ViewBag.Clinics = clinics;
            ViewBag.AppointmentTypes = Enum.GetValues(typeof(AppointmentType)).Cast<AppointmentType>().ToList();
            ViewBag.MinDate = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
            ViewBag.MaxDate = DateTime.Today.AddMonths(3).ToString("yyyy-MM-dd");
        }

        // NEW: Upcoming Appointments Action
        [Route("Patient/UpcomingAppointments/{id:int}")]
        public IActionResult UpcomingAppointments(int id)
        {
            var patient = db.Patients.Find(id);
            if (patient == null)
            {
                return RedirectToAction("Login");
            }

            var now = DateTime.Now;
            var today = DateTime.Today;

            // Find patient's upcoming appointments
            var upcomingAppointments = db.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Clinic)
                .Where(a => a.PatientId == id)
                .Where(a => a.AppointmentDate > now || (a.AppointmentDate.Date == today && a.AppointmentDate.TimeOfDay >= now.TimeOfDay))
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .ToList();
            ViewBag.Patient = patient;
            return View(upcomingAppointments);
        }

        // NEW: Profile Info Action
        [Route("Patient/ProfileInfo/{id:int}")]
        public IActionResult ProfileInfo(int id)
        {
            var patient = db.Patients.Find(id);
            if (patient == null)
            {
                return RedirectToAction("Login");
            }

            var now = DateTime.Now;
            var today = DateTime.Today;

            var upcomingCount = db.Appointments
               .Where(a => a.PatientId == id)
               .Where(a => a.AppointmentDate > today || (a.AppointmentDate == today && a.AppointmentTime >= now.TimeOfDay))
               .Count();

            var pastCount = db.Appointments
                .Where(a => a.PatientId == id)
                .Where(a => a.AppointmentDate < today || (a.AppointmentDate == today && a.AppointmentTime < now.TimeOfDay))
                .Count();

            var nextAppointment = db.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Clinic)
                .Where(a => a.PatientId == id)
                .Where(a => a.AppointmentDate > today || (a.AppointmentDate == today && a.AppointmentTime >= now.TimeOfDay))
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .FirstOrDefault();

            var paymentsQuery = db.Payments
             .Join(db.Appointments, p => p.AppointmentId, a => a.AppointmentId, (p, a) => new { Payment = p, Appointment = a })
             .Where(pa => pa.Appointment.PatientId == id)
             .Select(pa => pa.Payment);

            var lastPayment = paymentsQuery
                .OrderByDescending(p => p.PaidAt)
                .FirstOrDefault();

            var totalPaid = paymentsQuery
                .Where(p => p.PaidAt != null)
                .Select(p => (decimal?)p.Amount)
                .Sum() ?? 0m;

            ViewBag.UpcomingCount = upcomingCount;
            ViewBag.PastCount = pastCount;
            ViewBag.NextAppointment = nextAppointment;
            ViewBag.LastPayment = lastPayment;
            ViewBag.TotalPaid = totalPaid;

            return View(patient);
        }
    }
}