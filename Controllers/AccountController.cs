using Microsoft.AspNetCore.Mvc;
using DairyProductApp.Models;
using DairyProductApp.Services;
using DairyProductApp.Filters;

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
            if (HttpContext.Session.GetString("AdminLoggedIn") == "true")
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _sheets.ValidateAdminLogin(username, password);

            if (user != null)
            {
                HttpContext.Session.SetString("AdminLoggedIn", "true");
                HttpContext.Session.SetString("AdminUsername", user.Username);
                HttpContext.Session.SetString("AdminFullName", user.FullName);
                HttpContext.Session.SetString("AdminRole", user.Role.ToString());
                HttpContext.Session.SetString("AdminDairyName", user.DairyName);
                HttpContext.Session.SetString("AdminUserId", user.Id.ToString());

                // Update last login
                await _sheets.UpdateLastLogin(user.Id);

                // Log activity
                await _sheets.LogActivity(user.Username, "Login", $"{user.FullName} logged in");

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Galat username ya password!";
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            var username = HttpContext.Session.GetString("AdminUsername") ?? "Unknown";
            await _sheets.LogActivity(username, "Logout", $"{username} logged out");

            HttpContext.Session.Clear();
            TempData["Success"] = "Successfully logout ho gaye!";
            return RedirectToAction(nameof(Login));
        }

        [AdminOnly]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = int.Parse(HttpContext.Session.GetString("AdminUserId") ?? "0");
            var user = await _sheets.GetAdminUserById(userId);

            if (user == null)
            {
                ViewBag.Error = "User nahi mila!";
                return View();
            }

            if (currentPassword != user.Password)
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

            user.Password = newPassword;
            await _sheets.UpdateAdminUser(user);
            await _sheets.LogActivity(user.Username, "Password Changed", $"{user.FullName} ne password change kiya");

            TempData["Success"] = "Password successfully change ho gaya!";
            return RedirectToAction("Settings", "Admin");
        }

        // ============ USER MANAGEMENT (SuperAdmin Only) ============
        [AdminOnly]
        public async Task<IActionResult> Users()
        {
            var role = HttpContext.Session.GetString("AdminRole");
            if (role != "SuperAdmin")
            {
                TempData["Error"] = "Sirf SuperAdmin users manage kar sakta hai!";
                return RedirectToAction("Index", "Home");
            }

            var users = await _sheets.GetAllAdminUsers();
            return View(users.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToList());
        }

        [AdminOnly]
        public IActionResult CreateUser()
        {
            var role = HttpContext.Session.GetString("AdminRole");
            if (role != "SuperAdmin")
                return RedirectToAction("Index", "Home");
            return View(new AdminUser());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public async Task<IActionResult> CreateUser(AdminUser newUser)
        {
            var role = HttpContext.Session.GetString("AdminRole");
            if (role != "SuperAdmin")
                return RedirectToAction("Index", "Home");

            if (ModelState.IsValid)
            {
                // Check duplicate username
                var existing = await _sheets.GetAllAdminUsers();
                if (existing.Any(u => u.Username.Equals(newUser.Username, StringComparison.OrdinalIgnoreCase)))
                {
                    ViewBag.Error = "Yeh username pehle se hai! Doosra username choose karein.";
                    return View(newUser);
                }

                newUser.CreatedAt = DateTime.Now;
                await _sheets.AddAdminUser(newUser);

                var adminName = HttpContext.Session.GetString("AdminFullName") ?? "Admin";
                await _sheets.LogActivity(adminName, "User Created", $"New user '{newUser.Username}' ({newUser.FullName}) created with role {newUser.Role}");

                TempData["Success"] = $"User '{newUser.Username}' successfully banaya! Ab woh login kar sakta hai.";
                return RedirectToAction(nameof(Users));
            }
            return View(newUser);
        }

        [AdminOnly]
        public async Task<IActionResult> EditUser(int? id)
        {
            var role = HttpContext.Session.GetString("AdminRole");
            if (role != "SuperAdmin")
                return RedirectToAction("Index", "Home");

            if (id == null) return NotFound();
            var user = await _sheets.GetAdminUserById(id.Value);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public async Task<IActionResult> EditUser(int id, AdminUser editedUser)
        {
            var role = HttpContext.Session.GetString("AdminRole");
            if (role != "SuperAdmin")
                return RedirectToAction("Index", "Home");

            if (id != editedUser.Id) return NotFound();

            if (ModelState.IsValid)
            {
                await _sheets.UpdateAdminUser(editedUser);
                var adminName = HttpContext.Session.GetString("AdminFullName") ?? "Admin";
                await _sheets.LogActivity(adminName, "User Updated", $"User '{editedUser.Username}' updated");

                TempData["Success"] = "User update ho gaya!";
                return RedirectToAction(nameof(Users));
            }
            return View(editedUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var role = HttpContext.Session.GetString("AdminRole");
            if (role != "SuperAdmin")
                return RedirectToAction("Index", "Home");

            var user = await _sheets.GetAdminUserById(id);
            if (user != null && user.Role != AdminRole.SuperAdmin) // Can't delete SuperAdmin
            {
                await _sheets.DeleteAdminUser(id);
                var adminName = HttpContext.Session.GetString("AdminFullName") ?? "Admin";
                await _sheets.LogActivity(adminName, "User Deleted", $"User '{user.Username}' deleted");
                TempData["Success"] = "User delete ho gaya!";
            }
            else
            {
                TempData["Error"] = "SuperAdmin ko delete nahi kar sakte!";
            }
            return RedirectToAction(nameof(Users));
        }

        // Activity Log
        [AdminOnly]
        public async Task<IActionResult> ActivityLog()
        {
            var role = HttpContext.Session.GetString("AdminRole");
            if (role != "SuperAdmin")
            {
                TempData["Error"] = "Sirf SuperAdmin activity log dekh sakta hai!";
                return RedirectToAction("Index", "Home");
            }

            var logs = await _sheets.GetRecentActivity(100);
            return View(logs);
        }
    }
}
