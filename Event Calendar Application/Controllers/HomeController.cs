using System;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using EventPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Net.Mail;
using System.Text;

namespace EventPlanner.Controllers
{
    public class HomeController : Controller
    {
        private readonly MyContext _context;
        private bool IsUserInAnotherEventOnSameDay(int userId, DateTime eventDate)
        {
            var eventsOnSameDay = _context.Links
                .Include(l => l.Event)
                .Where(l => l.UserId == userId && l.Event.ScheduledAt.Date == eventDate.Date)
                .ToList();

            return eventsOnSameDay.Any();
        }


        public HomeController(MyContext context)
        {
            _context = context;
        }

        [HttpGet("Dashboard")]
        public IActionResult Dashboard()
        {
            var loggedUser = LoggedUser();
            if (loggedUser == null)
            {
                return Redirect("/");
            }
            ViewBag.Events = _context.Events
                .Include(u => u.Creator)
                .Include(g => g.Guests).ThenInclude(us => us.User)
                .Where(t => t.Creator == loggedUser)
                .OrderBy(time => time.ScheduledAt);
            ViewBag.JoinedEvent = _context.Links
                .Include(u => u.User)
                .Include(g => g.Event).ThenInclude(i => i.Creator)
                .Where(t => t.UserId == loggedUser.UserId)
                .OrderBy(time => time.Event.ScheduledAt);
            Console.WriteLine(ViewBag.JoinedEvent);
            ViewBag.CurrentUser = loggedUser; // Güncellenen ViewBag değişkeni
            ViewBag.Me = _context.Users
                .Include(t => t.FreeTimes)
                .FirstOrDefault(u => u.UserId == (int)HttpContext.Session.GetInt32("LoggedUser"));
            ViewBag.LastMonth = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month - 1);
            ViewBag.Month = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
            ViewBag.StartCal = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            return View();
        }



        [HttpGet("event/new")]
        public IActionResult NewEvent()
        {
            if (LoggedUser() == null)
            {
                return Redirect("/");
            }
            // get current user
            ViewBag.BestDates = BestDate();
            ViewBag.CurrentUser = LoggedUser();
            return View();
        }
        [HttpPost("event/create")]
        public IActionResult CreateEvent(Event newEvent)
        {
            var currentUser = LoggedUser();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Landing");
            }

            if (ModelState.IsValid)
            {
                if (IsUserInAnotherEventOnSameDay(currentUser.UserId, newEvent.ScheduledAt))
                {
                    ModelState.AddModelError("", "You are already part of another event on the same day.");
                    return View("NewEvent", newEvent);
                }

                newEvent.Creator = currentUser;
                newEvent.UserId = currentUser.UserId;
                _context.Add(newEvent);
                _context.SaveChanges();
                // Etkinlik oluşturan kişinin puanını artır
                UpdateUserPointsAndAchievements(currentUser.UserId, 20);
                return RedirectToAction("Dashboard");
            }
            return View("NewEvent", newEvent);
        }
       

        [HttpGet("event/{EventId}")]
        public IActionResult DisplayEvent(int EventId)
        {
            // see if user is logged in
            ViewBag.CurrentUser = LoggedUser();
            if (ViewBag.CurrentUser == null) // send back to index if not logged in.
                return Redirect("/");

            User CurrentUser = _context.Users.Include(u => u.Friends).FirstOrDefault(u => u.UserId == HttpContext.Session.GetInt32("LoggedUser"));
            List<User> FriendsOfUser = new List<User>();

            foreach (var item in CurrentUser.Friends)
            {
                FriendsOfUser.Add(_context.Users.FirstOrDefault(u => u.UserId == item.TargetId));
                Console.WriteLine("Added to list.");
            }
            ViewBag.Friends = FriendsOfUser;
            ViewBag.CheckLink = _context.Links.FirstOrDefault(u => u.UserId == LoggedUser().UserId && u.EventId == EventId);
            ViewBag.CurrentUser = CurrentUser;
            Event CurrentEvent = _context.Events.Include(u => u.Guests).ThenInclude(u => u.User).FirstOrDefault(e => e.EventId == EventId);
            List<User> UsersAtEvent = new List<User>();
            foreach (var guest in CurrentEvent.Guests)
            {
                User userToAdd = _context.Users.FirstOrDefault(u => u.UserId == guest.UserId);
                UsersAtEvent.Add(userToAdd);
            }
            ViewBag.UsersAtEvent = UsersAtEvent;
            ViewBag.CurrentEvent = CurrentEvent;
            return View();
        }

        [HttpGet("event/join/{eventId}")]
        public IActionResult JoinEvent(int eventId)
        {
            var currentUser = LoggedUser();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Landing");
            }

            var eventToJoin = _context.Events.FirstOrDefault(e => e.EventId == eventId);
            if (eventToJoin == null)
            {
                TempData["Error"] = "Event not found.";
                return RedirectToAction("Dashboard");
            }

            if (IsUserInAnotherEventOnSameDay(currentUser.UserId, eventToJoin.ScheduledAt))
            {
                TempData["Error"] = "You are already part of another event on the same day.";
                return RedirectToAction("Dashboard");
            }

            var link = new Link
            {
                UserId = currentUser.UserId,
                EventId = eventId
            };

            _context.Add(link);
            _context.SaveChanges();
            // Kullanıcı puanlarını ve başarılarını güncelle
            UpdateUserPointsAndAchievements(currentUser.UserId, 10); // Puan ekleme
            return RedirectToAction("Dashboard");
        }





        [HttpGet("invitation")]
        public IActionResult Invitation()
        {
            ViewBag.RequestInvites = _context.Invites.Include(u => u.User).Include(e => e.Event).Where(d => d.TargetId == LoggedUser().UserId);
            ViewBag.Invites = _context.RequestedInvites.Include(u => u.User).Include(e => e.Event).Where(d => d.Requester == LoggedUser().UserId);
            return View();
        }

        

        private void UpdateUserPointsAndAchievements(int userId, int pointsChange)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
            if (user != null)
            {
                user.Points += pointsChange;

                // Başarılar: Örnek olarak puan tabanlı başarılar ekleyebilirsiniz
                if (user.Points >= 100)
                {
                    user.Achievements = "Gold Member";
                }
                else if (user.Points >= 50)
                {
                    user.Achievements = "Silver Member";
                }
                else
                {
                    user.Achievements = "Bronze Member";
                }

                _context.SaveChanges();
            }
        }




        [HttpGet("event/leave/{eventId}")]
        public IActionResult LeaveEvent(int eventId)
        {
            var currentUser = LoggedUser();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Landing");
            }

            var link = _context.Links.FirstOrDefault(l => l.EventId == eventId && l.UserId == currentUser.UserId);
            if (link != null)
            {
                _context.Links.Remove(link);
                _context.SaveChanges();

                UpdateUserPointsAndAchievements(currentUser.UserId, -10); // Puanı düşür
            }

            return RedirectToAction("Dashboard");
        }



        [HttpGet("event/delete/{eventId}")]
        public IActionResult DeleteEvent(int eventId)
        {
            var currentUser = LoggedUser();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Landing");
            }

            var eventToDelete = _context.Events
                .Include(e => e.Guests)
                .FirstOrDefault(e => e.EventId == eventId);

            if (eventToDelete == null || eventToDelete.UserId != currentUser.UserId)
            {
                TempData["Error"] = "Event not found or you do not have permission to delete this event.";
                return RedirectToAction("Dashboard");
            }

            var participants = eventToDelete.Guests.Select(g => g.UserId).ToList();

            _context.Events.Remove(eventToDelete);
            _context.SaveChanges();

            // Katılımcıların puanlarını düşür
            foreach (var userId in participants)
            {
                UpdateUserPointsAndAchievements(userId, -10); // Puanı düşür
            }

            // Etkinlik oluşturan kişinin puanını azalt
            UpdateUserPointsAndAchievements(currentUser.UserId, -20); // Puanı azalt

            return RedirectToAction("Dashboard");
        }


        [HttpGet("event/edit/{eventId}")]
        public IActionResult EditEvent(int eventId)
        {
            var currentUser = LoggedUser();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Landing");
            }

            var eventToEdit = _context.Events.FirstOrDefault(e => e.EventId == eventId);
            if (eventToEdit == null || eventToEdit.UserId != currentUser.UserId)
            {
                TempData["Error"] = "Event not found or you do not have permission to edit this event.";
                return RedirectToAction("Dashboard");
            }

            return View(eventToEdit);
        }




        [HttpPost("event/update")]
        public IActionResult UpdateEvent(Event updatedEvent)
        {
            var currentUser = LoggedUser();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Landing");
            }

            var eventInDb = _context.Events.Include(e => e.Creator).FirstOrDefault(e => e.EventId == updatedEvent.EventId);
            if (eventInDb == null || eventInDb.Creator.UserId != currentUser.UserId)
            {
                return RedirectToAction("Dashboard");
            }

            if (ModelState.IsValid)
            {
                eventInDb.Title = updatedEvent.Title;
                eventInDb.ScheduledAt = updatedEvent.ScheduledAt;
                eventInDb.Duration = updatedEvent.Duration;
                eventInDb.EndAt = updatedEvent.ScheduledAt.AddHours(updatedEvent.Duration);
                eventInDb.Description = updatedEvent.Description;
                eventInDb.ParticipationCriteria = updatedEvent.ParticipationCriteria;

                _context.SaveChanges();
                return RedirectToAction("DisplayEvent", new { EventId = updatedEvent.EventId });
            }

            return View("EditEvent", updatedEvent);
        }

        private User LoggedUser()
        {
            int? userId = HttpContext.Session.GetInt32("LoggedUser"); if (userId == null) { return null; }
            return _context.Users.FirstOrDefault(u => u.UserId == userId);
        }

            public Dictionary<DateTime, int> BestDate() // this will return null if the user isn't logged in.
        {
            List<DateTime> GoodTimes = new List<DateTime>();
            foreach (Friend fr in _context.Friends.Include(u => u.User).ThenInclude(g => g.FreeTimes).Where(t => t.TargetId == LoggedUser().UserId && t.Status == 2))
            {
                foreach (Time gt in fr.User.FreeTimes)
                {
                    GoodTimes.Add(gt.StartAt);
                }
            }
            var q = GoodTimes.GroupBy(x => x).Select(g => new { Value = g.Key, Count = g.Count() }).OrderByDescending(x => x.Count);
            Dictionary<DateTime, int> BestTimes = new Dictionary<DateTime, int>();
            foreach (var x in q)
            {
                BestTimes.Add(x.Value, x.Count);
            }
            foreach (var l in BestTimes)
            {
                Console.WriteLine(l.Key);
                Console.WriteLine(l.Value);
            }
            return BestTimes;
        }

        [HttpGet("/link/{linkId}/delete")]
        public IActionResult DeleteLink(int linkId)
        {
            Console.WriteLine("Delete link " + linkId);
            Link LinkToDel = _context.Links.FirstOrDefault(l => l.LinkId == linkId);
            _context.Remove(LinkToDel);
            _context.SaveChanges();
            return RedirectToAction("Dashboard");
        }

        [HttpGet("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Redirect("/");
        }

        public void SendReminder(Reminder reminder)
        {
            MailMessage message = new MailMessage(reminder.from, reminder.to);
            message.Subject = reminder.MesssageSubject;
            message.Body = reminder.MessageBody;
            message.BodyEncoding = Encoding.UTF8;
            message.IsBodyHtml = true;
            SmtpClient client = new SmtpClient("smtp.gmail.com", 587); // Gmail smtp    
            System.Net.NetworkCredential basicCredential1 = new System.Net.NetworkCredential(reminder.from, reminder.PW);
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.Credentials = basicCredential1;
            try
            {
                client.Send(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

public void CheckForReminders()
        {
            //this function will look up each reminder that is due to be sent
            List<Reminder> remindersToSend = _context.Reminders
                .Include(r => r.Event)
                .Where(r => r.TimeToSendReminder < DateTime.Now)
                .ToList();
                Console.WriteLine($"There are {remindersToSend.Count} to send");
            foreach (Reminder item in remindersToSend)
            {
                SendReminder(item);
                var RemToDel = _context.Reminders.First(r => r.ReminderId == item.ReminderId);
                _context.Remove(item);
                _context.SaveChanges();
            }
        }
    }
}