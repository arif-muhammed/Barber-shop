using Hairdresser_Website.Data;
using Hairdresser_Website.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Hairdresser_Website.Controllers
{
    [Authorize(Roles = "admin")]

    public class ServiceController : Controller
    {
        private readonly SalonDbContext _context;

        public ServiceController(SalonDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var services = _context.Services
                .Include(s => s.Salon)
                .ToList();

            return View(services);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Salons = new SelectList(_context.Salons, "SalonId", "Name");
            return View();
        }

        [HttpPost]
        public IActionResult Create(Service service)
        {
            ModelState.Remove("Salon");

            if (ModelState.IsValid)
            {
                _context.Services.Add(service);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Service has been successfully added.";
                return RedirectToAction("Index");
            }

            ViewBag.Salons = new SelectList(_context.Salons, "SalonId", "Name", service.SalonId);
            return View(service);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var service = _context.Services.FirstOrDefault(s => s.ServiceId == id);

            if (service == null) return NotFound();

            ViewBag.Salons = new SelectList(_context.Salons, "SalonId", "Name", service.SalonId);
            return View(service);
        }

        [HttpPost]
        public IActionResult Edit(Service service)
        {
            ModelState.Remove("Salon");

            if (ModelState.IsValid)
            {
                var existingService = _context.Services.FirstOrDefault(s => s.ServiceId == service.ServiceId);

                if (existingService == null) return NotFound();

                existingService.Name = service.Name;
                existingService.Price = service.Price;
                existingService.Duration = service.Duration;
                existingService.SalonId = service.SalonId;

                _context.SaveChanges();
                TempData["SuccessMessage"] = "Service has been successfully updated.";
                return RedirectToAction("Index");
            }

            ViewBag.Salons = new SelectList(_context.Salons, "SalonId", "Name", service.SalonId);
            return View(service);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var service = _context.Services
                .Include(s => s.Salon)
                .FirstOrDefault(s => s.ServiceId == id);

            if (service == null) return NotFound();

            return View(service);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            var service = _context.Services.FirstOrDefault(s => s.ServiceId == id);

            if (service != null)
            {
                _context.Services.Remove(service);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Service has been successfully deleted.";
            }
            else
            {
                TempData["ErrorMessage"] = "Service not found.";
            }

            return RedirectToAction("Index");
        }
    }
}
