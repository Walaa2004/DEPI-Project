using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            ViewBag.TotalRevenue = _context.Payments
                .Where(p => p.PaymentStatus == PaymentStatus.Completed)
                .Sum(p => p.Amount);

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

        public IActionResult Appointments()
        {
            var appointments = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .OrderByDescending(a => a.AppointmentDate)
                .ToList();

            return View(appointments);
        }

        public IActionResult AppointmentDetails(int id)
        {
            var appointment = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Clinic)
                .FirstOrDefault(a => a.AppointmentId == id);

            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        [HttpPost]
        public IActionResult CancelAppointment(int id)
        {
            var appointment = _context.Appointments.Find(id);

            if (appointment == null)
            {
                return NotFound();
            }

            appointment.Status = AppointmentStatus.Cancelled;
            _context.SaveChanges();

            return RedirectToAction("Appointments");
        }

        [HttpPost]
        public IActionResult DeleteAppointment(int id)
        {
            var appointment = _context.Appointments.Find(id);

            if (appointment == null)
            {
                return NotFound();
            }

            _context.Appointments.Remove(appointment);
            _context.SaveChanges();

            return RedirectToAction("Appointments");
        }

        public IActionResult Payments(string status = "all")
        {
            var paymentsQuery = _context.Payments
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Patient)
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Doctor)
                .AsQueryable();

            // Filter by payment status
            switch (status.ToLower())
            {
                case "pending":
                    paymentsQuery = paymentsQuery.Where(p => p.PaymentStatus == PaymentStatus.Pending);
                    ViewBag.CurrentFilter = "Pending";
                    break;
                case "completed":
                    paymentsQuery = paymentsQuery.Where(p => p.PaymentStatus == PaymentStatus.Completed);
                    ViewBag.CurrentFilter = "Completed";
                    break;
                case "failed":
                    paymentsQuery = paymentsQuery.Where(p => p.PaymentStatus == PaymentStatus.Failed);
                    ViewBag.CurrentFilter = "Failed";
                    break;
                default:
                    ViewBag.CurrentFilter = "All";
                    break;
            }

            var payments = paymentsQuery
                .OrderByDescending(p => p.PaymentId)
                .ToList();

         

            return View(payments);
        }

      
        public IActionResult PaymentDetails(int id)
        {
            var payment = _context.Payments
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Patient)
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Doctor)
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Clinic)
                .FirstOrDefault(p => p.PaymentId == id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

      
        [HttpPost]
        public IActionResult ApprovePayment(int id, string? transactionId = null)
        {
            try
            {
                var payment = _context.Payments.Find(id);

                if (payment == null)
                {
                    TempData["Error"] = "Payment not found.";
                    return RedirectToAction("Payments");
                }

                if (payment.PaymentStatus != PaymentStatus.Pending)
                {
                    TempData["Warning"] = "Only pending payments can be approved.";
                    return RedirectToAction("PaymentDetails", new { id = id });
                }

                // Update payment status
                payment.PaymentStatus = PaymentStatus.Completed;
                payment.PaidAt = DateTime.Now;

                // Set transaction ID if provided
                if (!string.IsNullOrEmpty(transactionId))
                {
                    payment.TransactionId = transactionId;
                }
                else
                {
                    // Generate a simple transaction ID if none provided
                    payment.TransactionId = $"ADMIN_{DateTime.Now:yyyyMMddHHmmss}_{payment.PaymentId}";
                }

                _context.SaveChanges();

                TempData["Success"] = $"Payment #{payment.PaymentId} has been approved successfully.";
                return RedirectToAction("PaymentDetails", new { id = id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to approve payment: {ex.Message}";
                return RedirectToAction("PaymentDetails", new { id = id });
            }
        }

        
        [HttpPost]
        public IActionResult RejectPayment(int id, string? rejectionReason = null)
        {
            try
            {
                var payment = _context.Payments.Find(id);

                if (payment == null)
                {
                    TempData["Error"] = "Payment not found.";
                    return RedirectToAction("Payments");
                }

                if (payment.PaymentStatus != PaymentStatus.Pending)
                {
                    TempData["Warning"] = "Only pending payments can be rejected.";
                    return RedirectToAction("PaymentDetails", new { id = id });
                }

                // Update payment status
                payment.PaymentStatus = PaymentStatus.Failed;

                // Store rejection reason in transaction ID field for reference
                if (!string.IsNullOrEmpty(rejectionReason))
                {
                    payment.TransactionId = $"REJECTED: {rejectionReason}";
                }
                else
                {
                    payment.TransactionId = $"REJECTED_BY_ADMIN_{DateTime.Now:yyyyMMddHHmmss}";
                }

                _context.SaveChanges();

                TempData["Success"] = $"Payment #{payment.PaymentId} has been rejected.";
                return RedirectToAction("PaymentDetails", new { id = id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to reject payment: {ex.Message}";
                return RedirectToAction("PaymentDetails", new { id = id });
            }
        }

    }
}