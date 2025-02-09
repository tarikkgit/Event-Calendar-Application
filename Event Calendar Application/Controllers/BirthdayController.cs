using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using EventPlanner.Models;
using Microsoft.EntityFrameworkCore;

namespace EventPlanner.Controllers
{
    public class BirthdayController : Controller
    {
        private readonly MyContext _context;

        public BirthdayController(MyContext context)
        {
            _context = context;
        }

        [HttpGet("birthdaycard/{eventId}")]
        public IActionResult BirthdayCard(int eventId)
        {
            ViewBag.Messages = _context.Messages.Where(m => m.EventId == eventId).ToList();
            ViewBag.CurrentEvent = _context.Events.FirstOrDefault(e => e.EventId == eventId);
            return View(new Message { EventId = eventId }); // Boþ bir Message nesnesi döndürüyoruz.
        }

        [HttpPost("birthdaycard/{eventId}/post")]
        public IActionResult Post(Message newMessage, int eventId)
        {
            if (ModelState.IsValid)
            {
                newMessage.EventId = eventId;
                _context.Add(newMessage);
                _context.SaveChanges();
                return RedirectToAction("BirthdayCard", new { eventId });
            }
            ViewBag.Messages = _context.Messages.Where(m => m.EventId == eventId).ToList();
            ViewBag.CurrentEvent = _context.Events.FirstOrDefault(e => e.EventId == eventId);
            return View("BirthdayCard", newMessage);
        }
    }
}
