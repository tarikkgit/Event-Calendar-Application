using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using EventPlanner.Models;
using Microsoft.Extensions.Logging;

namespace EventPlanner.Controllers
{
    public class LandingController : Controller
    {
        private readonly MyContext _context;
        private readonly ILogger<LandingController> _logger;

        public LandingController(MyContext context, ILogger<LandingController> logger)
        {
            _context = context;
            _logger = logger;
        }
        private User GetLoggedUser()
        {
            var loggedUserId = HttpContext.Session.GetInt32("LoggedUser");
            if (loggedUserId == null)
            {
                _logger.LogWarning("No user logged in. Session value is null.");
                return null;
            }
            var user = _context.Users.FirstOrDefault(u => u.UserId == loggedUserId);
            if (user == null)
            {
                _logger.LogWarning("User not found in database. UserId: {loggedUserId}", loggedUserId);
            }
            else
            {
                _logger.LogInformation("GetLoggedUser: Found user with UserId: {UserId}", user.UserId);
            }
            return user;
        }


        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet("register")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost("registration")]
        public IActionResult Registration(User user)
        {
            if (ModelState.IsValid)
            {
                if (_context.Users.Any(u => u.Email == user.Email))
                {
                    ModelState.AddModelError("Email", "Email is already in use");
                    return View("Register");
                }

                PasswordHasher<User> Hasher = new PasswordHasher<User>();
                user.Password = Hasher.HashPassword(user, user.Password);
                _context.Add(user);
                _context.SaveChanges();
                HttpContext.Session.SetInt32("LoggedUser", user.UserId);
                _logger.LogInformation("User registered and logged in. UserId: {user.UserId}", user.UserId);
                return Redirect("/Dashboard");
            }
            return View("Register");
        }
        [HttpGet("profile")]
        public IActionResult Profile()
        {
            var loggedUser = GetLoggedUser();
            if (loggedUser == null)
            {
                return RedirectToAction("Login", "Landing");
            }

            var userInDb = _context.Users.FirstOrDefault(u => u.UserId == loggedUser.UserId);
            return View(userInDb);
        }


        [HttpPost("logging")]
        public IActionResult Logging(LoginUser userSubmission)
        {
            if (ModelState.IsValid)
            {
                var userInDb = _context.Users.FirstOrDefault(u => u.Email == userSubmission.Email);
                if (userInDb == null)
                {
                    ModelState.AddModelError("Email", "Invalid Email/Password");
                    return View("Login");
                }

                var hasher = new PasswordHasher<LoginUser>();
                var result = hasher.VerifyHashedPassword(userSubmission, userInDb.Password, userSubmission.LoginPassword);
                if (result == 0)
                {
                    ModelState.AddModelError("Email", "Invalid Email/Password");
                    return View("Login");
                }
                HttpContext.Session.SetInt32("LoggedUser", userInDb.UserId);
                _logger.LogInformation("User logged in. UserId: {userInDb.UserId}", userInDb.UserId);
                return Redirect("/Dashboard");
            }
            return View("Login");
        }
        [HttpGet("forgotpassword")]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost("forgotpassword")]
        public IActionResult ForgotPassword(string email, string currentPassword, string newPassword)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                ViewBag.Message = "No user found with the provided email address.";
                return View();
            }

            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.Password, currentPassword);

            if (result == PasswordVerificationResult.Failed)
            {
                ViewBag.Message = "Incorrect current password.";
                return View();
            }

            user.Password = hasher.HashPassword(user, newPassword);
            _context.SaveChanges();

            ViewBag.Message = "Password updated successfully.";
            return View();
        }

        [HttpGet("editprofile")]
        public IActionResult EditProfile()
        {
            var user = GetLoggedUser();
            if (user == null)
            {
                return RedirectToAction("Login", "Landing");
            }
            return View(user);
        }
        [HttpPost("updateprofile")]
        public IActionResult UpdateProfile(User user)
        {
            _logger.LogInformation("UpdateProfile method called.");
            var loggedUser = GetLoggedUser();
            _logger.LogInformation("LoggedUser: {LoggedUserId}, User: {UserId}", loggedUser?.UserId, user.UserId);

            if (loggedUser == null || loggedUser.UserId != user.UserId)
            {
                _logger.LogWarning("User not logged in or UserId mismatch. LoggedUser: {LoggedUserId}, UserId: {UserId}", loggedUser?.UserId, user.UserId);
                return RedirectToAction("Login", "Landing");
            }

            if (ModelState.IsValid)
            {
                _logger.LogInformation("ModelState is valid.");
                var userInDb = _context.Users.FirstOrDefault(u => u.UserId == user.UserId);
                if (userInDb != null)
                {
                    userInDb.FirstName = user.FirstName;
                    userInDb.LastName = user.LastName;
                    userInDb.Email = user.Email;
                    userInDb.Address = user.Address;
                    userInDb.ProfilePictureUrl = user.ProfilePictureUrl;
                    userInDb.UpdatedAt = DateTime.Now;

                    if (!string.IsNullOrEmpty(user.Password)) // If Password is provided, update it
                    {
                        var hasher = new PasswordHasher<User>();
                        userInDb.Password = hasher.HashPassword(user, user.Password);
                    }

                    _context.SaveChanges();
                    _logger.LogInformation("User profile updated successfully. UserId: {UserId}", user.UserId);
                    return RedirectToAction("Dashboard", "Home");
                }
                else
                {
                    _logger.LogWarning("User not found in database. UserId: {UserId}", user.UserId);
                }
            }
            else
            {
                _logger.LogWarning("ModelState is invalid. Errors:");
                foreach (var modelState in ModelState)
                {
                    foreach (var error in modelState.Value.Errors)
                    {
                        _logger.LogWarning("Property: {PropertyKey}, Error: {ErrorMessage}", modelState.Key, error.ErrorMessage);
                    }
                }
            }
            return View("EditProfile", user);
        }


        [HttpPost("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Landing");
        }
    }
}
