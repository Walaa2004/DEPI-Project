using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    public class PatientController : Controller
    {
        private readonly DBContext db;
        private readonly ZoomService? _zoomService;

        // Fix the constructor to properly handle dependency injection
        public PatientController(IServiceProvider serviceProvider)
        {
            db = new DBContext();
            // Try to get ZoomService, but don't fail if it's not registered
            _zoomService = serviceProvider.GetService<ZoomService>();
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

                
                if (patient.Appointments.Any())
                {
                   
                    var appointmentsWithoutPayments = patient.Appointments
                        .Where(a => a.Payment == null && a.Status != AppointmentStatus.Cancelled)
                        .ToList();

                    
                    foreach (var appointment in appointmentsWithoutPayments)
                    {
                        var payment = new Payment
                        {
                            Amount = appointment.Fee,
                            Currency = "EGP", 
                            PaymentStatus = PaymentStatus.Pending, 
                            AppointmentId = appointment.AppointmentId,
                            PaidAt = null,
                            TransactionId = null 
                        };

                        db.Payments.Add(payment);
                    }

                    // Save changes if any new payments were created
                    if (appointmentsWithoutPayments.Any())
                    {
                        db.SaveChanges();

                        patient = db.Patients
                            .Include(p => p.Appointments)
                                .ThenInclude(a => a.Payment)
                            .Include(p => p.Appointments)
                                .ThenInclude(a => a.Doctor)
                            .Include(p => p.Appointments)
                                .ThenInclude(a => a.Clinic)
                            .FirstOrDefault(p => p.PatientId == id);
                    }
                }

                // Get all payments from patient's appointments with all related data
                var payments = patient.Appointments
                    .Where(a => a.Payment != null)
                    .Select(a => a.Payment)
                    .OrderByDescending(p => p.PaidAt ?? DateTime.MinValue)
                    .ThenByDescending(p => p.PaymentId)
                    .ToList();

               
                ViewBag.Patient = patient;
                return View(payments);
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                ViewBag.Error = $"An error occurred while loading payment data: {ex.Message}";
                return RedirectToAction("Profile",id);
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
         

            return View(appointmentHistory);
        }

   
        [Route("Patient/AvailableAppointments/{id:int}")]
        public IActionResult AvailableAppointments(int id)
        {
            var patient = db.Patients.Find(id);
            if (patient == null)
            {
                return RedirectToAction("Login");
            }

          
            var allAvailableAppointments = db.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Clinic)
                .Where(a => a.PatientId == null) // Available appointments
                .Where(a => a.Status == AppointmentStatus.Pending)
                .Where(a => a.AppointmentDate >= DateTime.Today) // Future appointments only
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .ToList();

          
            var nearestAppointmentsByDoctor = allAvailableAppointments
                .GroupBy(a => a.DoctorId)
                .Select(group => group
                    .OrderBy(a => a.AppointmentDate)
                    .ThenBy(a => a.AppointmentTime)
                    .First()) // Take the earliest appointment for each doctor
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .ToList();

            ViewBag.Patient = patient;
            ViewBag.ShowFilter = true; // Flag to show filter buttons
            return View(nearestAppointmentsByDoctor);
        }

        
        [Route("Patient/AvailableInPersonAppointments/{id:int}")]
        public IActionResult AvailableInPersonAppointments(int id)
        {
            var patient = db.Patients.Find(id);
            if (patient == null)
            {
                return RedirectToAction("Login");
            }

            var nearestInPersonAppointments = db.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Clinic)
                .Where(a => a.PatientId == null) // Available appointments
                .Where(a => a.Status == AppointmentStatus.Pending)
                .Where(a => a.Type == AppointmentType.InPerson) // In-person only
                .Where(a => a.AppointmentDate >= DateTime.Today)
                .GroupBy(a => a.DoctorId)
                .AsEnumerable()
                .Select(group => group
                    .OrderBy(a => a.AppointmentDate)
                    .ThenBy(a => a.AppointmentTime)
                    .First())
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .ToList();

            ViewBag.Patient = patient;
            ViewBag.ShowFilter = false; // Don't show filter buttons
            ViewBag.FilterType = "In-Person"; // Show current filter
            return View("AvailableAppointments", nearestInPersonAppointments);
        }

        [Route("Patient/AvailableVideoAppointments/{id:int}")]
        public IActionResult AvailableVideoAppointments(int id)
        {
            var patient = db.Patients.Find(id);
            if (patient == null)
            {
                return RedirectToAction("Login");
            }

            var nearestVideoAppointments = db.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Clinic)
                .Where(a => a.PatientId == null) // Available appointments
                .Where(a => a.Status == AppointmentStatus.Pending)
                .Where(a => a.Type == AppointmentType.Video) // Video only
                .Where(a => a.AppointmentDate >= DateTime.Today)
                .GroupBy(a => a.DoctorId)
                .AsEnumerable()
                .Select(group => group
                    .OrderBy(a => a.AppointmentDate)
                    .ThenBy(a => a.AppointmentTime)
                    .First())
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .ToList();

            ViewBag.Patient = patient;
            ViewBag.ShowFilter = false; // Don't show filter buttons
            ViewBag.FilterType = "Video"; // Show current filter
            return View("AvailableAppointments", nearestVideoAppointments);
        }

      
        [HttpPost]
        [Route("Patient/BookAppointment")]
        public async Task<IActionResult> BookAppointment(int appointmentId, int patientId, string? symptoms)
        {
            try
            {
                var patient = db.Patients.Find(patientId);
                var appointment = db.Appointments
                    .Include(a => a.Doctor)
                    .FirstOrDefault(a => a.AppointmentId == appointmentId);

                if (patient == null)
                {
                    return RedirectToAction("Login");
                }

                if (appointment == null)
                {
                    TempData["Error"] = "Appointment not found.";
                    return RedirectToAction("AvailableAppointments", new { id = patientId });
                }

                // Check if appointment is still available
                if (appointment.PatientId != null)
                {
                    TempData["Error"] = "This appointment has already been booked by another patient.";
                    return RedirectToAction("AvailableAppointments", new { id = patientId });
                }

                // Book the appointment by inserting the PatientId
                appointment.PatientId = patientId;
                appointment.Status = AppointmentStatus.Confirmed;

                // Add symptoms if provided
                if (!string.IsNullOrEmpty(symptoms))
                {
                    appointment.Symptoms = symptoms;
                }

               
                if (appointment.Type == AppointmentType.Video)
                {
                    
                    string videoCallUrl = "https://zoom.us/j/2176899212?pwd=JlUCylFK7mCfuUZc3IsgbuV5aGVACy.1";
                    string successMessage = "Video appointment successfully booked";

        

                    if (_zoomService != null)
                    {
                        try
                        {
                            var meetingTopic = $"Medical Consultation - Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName} with {patient.FirstName} {patient.LastName}";
                            var startTime = appointment.AppointmentDate.Add(appointment.AppointmentTime);
                            var durationMinutes = 60;

                            var zoomMeeting = await _zoomService.CreateMeetingAsync(
                                meetingTopic,
                                startTime,
                                durationMinutes,
                                appointment.Doctor.Email
                            );

                            if (zoomMeeting != null && !string.IsNullOrEmpty(zoomMeeting.JoinUrl))
                            {
                                videoCallUrl = zoomMeeting.JoinUrl;
                                Console.WriteLine($"DEBUG: Zoom meeting created successfully: {videoCallUrl}");
                                successMessage = $"Video appointment successfully booked with Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}! Zoom meeting has been created.";
                            }
                            else
                            {
                                Console.WriteLine("DEBUG: Zoom meeting creation returned null or empty URL");
                            }
                        }
                        catch (Exception zoomEx)
                        {
                    
                            Console.WriteLine($"Zoom creation failed: {zoomEx.Message}");
                            
                        }
                    }
                    else
                    {
                        Console.WriteLine("DEBUG: ZoomService is null, using fallback URL");
                    
                    }

                    var videoSession = new VideoCallSession
                    {
                        AppointmentId = appointment.AppointmentId,
                        StartTime = appointment.AppointmentDate.Add(appointment.AppointmentTime),
                        Status = VideoCallStatus.Scheduled,
                        SessionType = SessionType.Consultation,
                        VideoCallUrl = videoCallUrl,
                        SessionTime = appointment.AppointmentDate.Add(appointment.AppointmentTime)
                    };

                

                    db.VideoCallSessions.Add(videoSession);
                    db.SaveChanges();
                   

                    appointment.SessionId = videoSession.SessionId;

                    TempData["Success"] = successMessage;
                }
                else if (appointment.Type == AppointmentType.InPerson && appointment.Doctor.ClinicId.HasValue)
                {
                    // Set clinic for in-person appointments
                    appointment.ClinicId = appointment.Doctor.ClinicId;
                    TempData["Success"] = $"In-person appointment successfully booked with Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}!";
                }

                db.SaveChanges();
                Console.WriteLine($"DEBUG: Appointment saved with SessionId: {appointment.SessionId}");

                return RedirectToAction("UpcomingAppointments", new { id = patientId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Exception in BookAppointment: {ex.Message}");

                TempData["Error"] = $"Failed to book appointment: {ex.Message}";
                return RedirectToAction("AvailableAppointments", new { id = patientId });
            }
        }

        // GET: Upcoming Appointments
        [Route("Patient/UpcomingAppointments/{id:int}")]
        public IActionResult UpcomingAppointments(int id)
        {
            var patient = db.Patients.Find(id);
            if (patient == null) return RedirectToAction("Login");

            var now = DateTime.Now;
            var today = DateTime.Today;

            var upcomingAppointments = db.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d.Clinic)
                .Include(a => a.Clinic)

                .Where(a => a.PatientId == id)
                .Include(a => a.VideoCallSession)
                // 1. Only show Future appointments
                .Where(a => a.AppointmentDate > today || (a.AppointmentDate == today && a.AppointmentTime >= now.TimeOfDay))
                .Where(a => a.Status != AppointmentStatus.Cancelled)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .ToList();

            ViewBag.Patient = patient;
            return View(upcomingAppointments);
        }
        // POST: Cancel Appointment
        [HttpPost]
        public IActionResult CancelAppointment(int id)
        {
            var appointment = db.Appointments.Find(id);

            if (appointment != null)
            {
                // 1. Change status to Cancelled (Enum value 4)
                appointment.Status = AppointmentStatus.Cancelled;

                // 2. Save changes to DB
                db.SaveChanges();

                // 3. Set a temporary success message
                TempData["Success"] = "Your appointment has been successfully cancelled.";

                // 4. Redirect back to the list (the cancelled item will now be hidden due to the filter above)
                return RedirectToAction("UpcomingAppointments", new { id = appointment.PatientId });
            }

            return RedirectToAction("Login");
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

            // 1. FIX: Filter out Cancelled appointments from the Count
            var upcomingCount = db.Appointments
               .Where(a => a.PatientId == id)
               .Where(a => a.Status != AppointmentStatus.Cancelled) // <--- ADDED THIS
               .Where(a => a.AppointmentDate > today || (a.AppointmentDate == today && a.AppointmentTime >= now.TimeOfDay))
               .Count();

            var pastCount = db.Appointments
                .Where(a => a.PatientId == id)
                .Where(a => a.AppointmentDate < today || (a.AppointmentDate == today && a.AppointmentTime < now.TimeOfDay))
                .Count();

            // 2. FIX: Filter out Cancelled appointments from the Next Appointment card
            var nextAppointment = db.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Clinic)
                .Where(a => a.PatientId == id)
                .Where(a => a.Status != AppointmentStatus.Cancelled) // <--- ADDED THIS
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

        // 1. GET: Show the Edit Form
        [HttpGet]
        [Route("Patient/EditProfile/{id:int}")]
        public IActionResult EditProfile(int id)
        {
            var patient = db.Patients.Find(id);
            if (patient == null)
            {
                return RedirectToAction("Login");
            }
            return View(patient);
        }

        // 2. POST: Process the Update
        [HttpPost]
        [Route("Patient/EditProfile/{id:int}")]
        public IActionResult EditProfile(Patient model)
        {
            try
            {
                var existingPatient = db.Patients.Find(model.PatientId);

                if (existingPatient == null)
                {
                    return RedirectToAction("Login");
                }

                // Update only the editable fields
                existingPatient.FirstName = model.FirstName;
                existingPatient.LastName = model.LastName;
                existingPatient.PhoneNumber = model.PhoneNumber;
                existingPatient.Age = model.Age;
                existingPatient.Gender = model.Gender;
                existingPatient.BloodGroup = model.BloodGroup;
                existingPatient.Height = model.Height;
                existingPatient.Weight = model.Weight;
                existingPatient.Allergies = model.Allergies;
                existingPatient.Address = model.Address;

                db.SaveChanges();

                ViewBag.Success = "Profile updated successfully!";
                return RedirectToAction("ProfileInfo", new { id = model.PatientId });
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error updating profile: " + ex.Message;
                return View(model);
            }
        }
    }
}