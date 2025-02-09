using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using EventPlanner.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace EventPlanner.Controllers
{
    public class AdminController : Controller
    {
        private readonly MyContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(MyContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("admin/dashboard")]
        public IActionResult Dashboard()
        {
            ViewBag.Users = _context.Users.ToList();
            ViewBag.Events = _context.Events.ToList();

            _logger.LogInformation("Admin dashboard accessed.");
            return View();
        }

        [HttpGet("admin/edituser/{userId}")]
        public IActionResult EditUser(int userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [HttpPost("admin/edituser/{userId}")]
        public IActionResult EditUser(int userId, User updatedUser)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = updatedUser.FirstName;
            user.LastName = updatedUser.LastName;
            user.Email = updatedUser.Email;
            user.Points = updatedUser.Points;

            _context.SaveChanges();

            return RedirectToAction("Dashboard");
        }

        [HttpGet("admin/deleteuser/{userId}")]
        public IActionResult DeleteUser(int userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            _context.SaveChanges();

            return RedirectToAction("Dashboard");
        }

        [HttpGet("admin/approveevent/{eventId}")]
        public IActionResult ApproveEvent(int eventId)
        {
            var eventItem = _context.Events.FirstOrDefault(e => e.EventId == eventId);
            if (eventItem == null)
            {
                return NotFound();
            }

            eventItem.IsApproved = true; // Onay durumunu güncelleme
            _context.SaveChanges();

            return RedirectToAction("Dashboard");
        }

        [HttpGet("admin/deleteevent/{eventId}")]
        public IActionResult DeleteEvent(int eventId)
        {
            var eventItem = _context.Events.FirstOrDefault(e => e.EventId == eventId);
            if (eventItem == null)
            {
                return NotFound();
            }

            _context.Events.Remove(eventItem);
            _context.SaveChanges();

            return RedirectToAction("Dashboard");
        }
    }
}
