using Microsoft.AspNetCore.Mvc;
using DairyProductApp.Services;

namespace DairyProductApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly GoogleSheetsService _sheets;

        public AccountController(GoogleSheetsService sheets)
        {
            _sheets = sheets;
        }

        public IActionResult Login()
        {
            // Already logged in? Go to dashboard
            if (HttpContext.Session.GetString("AdminLoggedIn") == "true")
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            // Get admin credentials from Google Sheets settings
            var savedUsername = await _sheets.GetSetting("AdminUsername") ?? "admin";
            var savedPassword = await _sheets.GetSetting("AdminPassword") ?? "rajdairy123";

            if (username == savedUsername && password == savedPassword)
            {
                HttpContext.Session.SetString("AdminLoggedIn", "true");
                HttpContext.Session.SetString("AdminUsername", username);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Galat username ya password!";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "Successfully logout ho gaye!";
            return RedirectToAction(nameof(Login));
        }

        // Change password page
        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
                return RedirectToAction(nameof(Login));
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newUsername, string newPassword, string confirmPassword)
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
                return RedirectToAction(nameof(Login));

            var savedPassword = await _sheets.GetSetting("AdminPassword") ?? "rajdairy123";

            if (currentPassword != savedPassword)
            {
                ViewBag.Error = "Current password galat hai!";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "New password aur confirm password match nahi karte!";
                return View();
            }

            if (newPassword.Length < 4)
            {
                ViewBag.Error = "Password kam se kam 4 characters ka hona chahiye!";
                return View();
            }

            if (!string.IsNullOrEmpty(newUsername))
                await _sheets.SaveSetting("AdminUsername", newUsername);

            await _sheets.SaveSetting("AdminPassword", newPassword);

            TempData["Success"] = "Username/Password successfully change ho gaya!";
            return RedirectToAction("Settings", "Admin");
        }
    }
}
