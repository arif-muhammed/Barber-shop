using Hairdresser_Website.Data;
using Hairdresser_Website.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Hairdresser_Website.Controllers
{
    [Authorize] 
    public class AppointmentController : Controller
    {
        private readonly SalonDbContext _context;

        public AppointmentController(SalonDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Index()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Service)
                .Include(a => a.Employee)
                .ToListAsync();

            return View(appointments);
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            ViewBag.Services = new SelectList(_context.Services, "ServiceId", "Name", appointment.ServiceId);
            ViewBag.Employees = new SelectList(_context.Employees, "EmployeeId", "Name", appointment.EmployeeId);

            return View(appointment);
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Appointment appointment)
        {
            if (id != appointment.AppointmentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Appointments.Any(a => a.AppointmentId == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.Services = new SelectList(_context.Services, "ServiceId", "Name", appointment.ServiceId);
            ViewBag.Employees = new SelectList(_context.Employees, "EmployeeId", "Name", appointment.EmployeeId);

            return View(appointment);
        }



        public async Task<IActionResult> Delete(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Service)
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);

            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }


        public IActionResult Create()
        {
            string userId = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Create", "Appointment") });
            }

            var salons = _context.Salons.ToList();
            ViewBag.Salons = new SelectList(salons, "SalonId", "Name");


            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appointment, int salonId, int serviceId, int employeeId, DateTime appointmentDate, TimeSpan appointmentTime)
        {
            Console.WriteLine($"Received Time: {appointmentTime}");
            Console.WriteLine($"Received Date: {appointmentDate}");
            

            string userId = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Create", "Appointment") });
            }

            appointmentDate = appointmentDate.Date.Add(appointmentTime);
            appointmentDate = DateTime.SpecifyKind(appointmentDate, DateTimeKind.Utc);

            if (!IsTimeSlotAvailable(employeeId, appointmentDate, serviceId))
            {
                TempData["Error"] = "The selected time slot is already blocked.";
                return RedirectToAction(nameof(Create));
            }

            var newAppointment = new Appointment
            {
                UserId = userId,
                ServiceId = serviceId,
                EmployeeId = employeeId,
                AppointmentDate = appointmentDate,
                Status = AppointmentStatus.Confirmed
            };

            try
            {
                _context.Appointments.Add(newAppointment);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while saving the appointment. Please try again.";
                return RedirectToAction(nameof(Create));
            }

        }

        private bool HandleConflictingAppointments(int employeeId, DateTime appointmentDate, int serviceId)
        {
            try
            {
                var service = _context.Services.Find(serviceId);
                if (service == null)
                {
                    return false;
                }
                int serviceDuration = service.Duration;

                var conflictingAppointments = _context.Appointments
                    .Where(a =>
                        a.EmployeeId == employeeId &&
                        a.AppointmentDate.Date == appointmentDate.Date &&
                        !(appointmentDate.AddMinutes(serviceDuration) <= a.AppointmentDate ||
                          appointmentDate >= a.AppointmentDate.AddMinutes(a.Service.Duration)))
                    .ToList();

                foreach (var conflictingAppointment in conflictingAppointments)
                {
                    var unavailableSlot = new UnavailableSlot
                    {
                        EmployeeId = conflictingAppointment.EmployeeId,
                        StartTime = conflictingAppointment.AppointmentDate,
                        EndTime = conflictingAppointment.AppointmentDate.AddMinutes(conflictingAppointment.Service.Duration)
                    };
                    _context.UnavailableSlots.Add(unavailableSlot);

                    _context.Appointments.Remove(conflictingAppointment);
                }

                _context.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandleConflictingAppointments: {ex.Message}");
                return false;
            }
        }


        private bool IsTimeSlotAvailable(int employeeId, DateTime appointmentDate, int serviceId)
        {
            var service = _context.Services.Find(serviceId);
            if (service == null)
            {
                return false;
            }

            int serviceDuration = service.Duration;

            bool isBlocked = _context.UnavailableSlots.Any(us =>
                us.EmployeeId == employeeId &&
                !(appointmentDate.AddMinutes(serviceDuration) <= us.StartTime ||
                  appointmentDate >= us.EndTime));

            return !isBlocked;
        }



        [HttpGet]
        public IActionResult GetEmployeesByServiceAndSalon(int salonId, int serviceId)
        {
            var employees = _context.Employees
                .Where(e => e.SalonId == salonId && e.Expertise != null)
                .Select(e => new { e.EmployeeId, e.Name })
                .ToList();

            return Json(employees);
        }

        [HttpGet]
        public async Task<IActionResult> GetServicesBySalon(int salonId)
        {
            var services = await _context.Services
                .Where(s => s.SalonId == salonId)
                .Select(s => new {
                    serviceId = s.ServiceId,
                    name = s.Name,
                    price = s.Price,
                    duration = s.Duration
                })
                .ToListAsync();

            return Json(services);
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableTimeSlots(int employeeId, DateTime date, int serviceId)
        {
            try
            {
                date = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);

                var service = await _context.Services.FindAsync(serviceId);
                if (service == null) return BadRequest("Service not found");
                int serviceDuration = service.Duration;

                var dayOfWeek = (Models.DayOfWeek)date.DayOfWeek;
                var availability = await _context.EmployeeAvailability
                    .Where(ea => ea.EmployeeId == employeeId && ea.DayOfWeek == dayOfWeek)
                    .ToListAsync();

                if (!availability.Any()) return Json(new List<TimeSlot>());

                var existingAppointments = await _context.Appointments
                    .Include(a => a.Service)
                    .Where(a => a.EmployeeId == employeeId &&
                                a.AppointmentDate.Date == date.Date &&
                                a.Status != AppointmentStatus.Cancelled)
                    .Select(a => new {
                        StartTime = a.AppointmentDate.TimeOfDay,
                        EndTime = a.AppointmentDate.TimeOfDay.Add(TimeSpan.FromMinutes(a.Service.Duration))
                    })
                    .ToListAsync();

                var timeSlots = new List<TimeSlot>();
                foreach (var av in availability)
                {
                    var currentTime = av.StartTime;
                    while (currentTime.Add(TimeSpan.FromMinutes(serviceDuration)) <= av.EndTime)
                    {
                        var slotEndTime = currentTime.Add(TimeSpan.FromMinutes(serviceDuration));

                        bool isAvailable = !existingAppointments.Any(a =>
                            !(slotEndTime <= a.StartTime || currentTime >= a.EndTime)); 

                        if (isAvailable)
                        {
                            timeSlots.Add(new TimeSlot
                            {
                                Time = currentTime,
                                IsAvailable = true
                            });
                        }

                        currentTime = currentTime.Add(TimeSpan.FromMinutes(30));
                    }
                }

                return Json(timeSlots);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Json(new List<TimeSlot>());
            }
        }


        public class TimeSlot
        {
            public TimeSpan Time { get; set; }
            public bool IsAvailable { get; set; }
        }
    }
}