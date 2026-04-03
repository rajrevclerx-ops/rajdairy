using Microsoft.AspNetCore.Mvc;
using DairyProductApp.Services;

namespace DairyProductApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly GoogleSheetsService _sheets;
        private readonly IWebHostEnvironment _env;

        public AdminController(GoogleSheetsService sheets, IWebHostEnvironment env)
        {
            _sheets = sheets;
            _env = env;
        }

        public async Task<IActionResult> Profile()
        {
            var collections = await _sheets.GetAllMilkCollections();
            var products = await _sheets.GetAllDairyProducts();
            var ghee = await _sheets.GetAllGheeProducts();
            var rates = await _sheets.GetAllMilkRates();

            ViewBag.TotalCollections = collections.Count;
            ViewBag.TotalFarmers = collections.Select(c => c.FarmerName).Distinct().Count();
            ViewBag.TotalProducts = products.Count;
            ViewBag.TotalGhee = ghee.Count;
            ViewBag.TotalRates = rates.Count;
            ViewBag.TotalMilk = collections.Sum(c => c.Quantity);
            ViewBag.TotalRevenue = collections.Sum(c => c.TotalAmount);
            ViewBag.TotalGheeStock = ghee.Sum(g => g.StockKg);

            // Check if profile photo exists
            ViewBag.ProfilePhoto = GetProfilePhotoPath();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhoto(IFormFile photo)
        {
            if (photo != null && photo.Length > 0)
            {
                // Validate file type
                var allowedTypes = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();

                if (!allowedTypes.Contains(extension))
                {
                    TempData["Error"] = "Sirf JPG, PNG, GIF, WEBP images allowed hain!";
                    return RedirectToAction(nameof(Profile));
                }

                // Max 5MB
                if (photo.Length > 5 * 1024 * 1024)
                {
                    TempData["Error"] = "Photo 5MB se chhoti honi chahiye!";
                    return RedirectToAction(nameof(Profile));
                }

                // Delete old photos
                var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "profile");
                Directory.CreateDirectory(uploadDir);
                foreach (var oldFile in Directory.GetFiles(uploadDir, "admin-photo.*"))
                {
                    System.IO.File.Delete(oldFile);
                }

                // Save new photo
                var fileName = $"admin-photo{extension}";
                var filePath = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }

                TempData["Success"] = "Profile photo successfully update ho gayi!";
            }
            else
            {
                TempData["Error"] = "Please ek photo select karein!";
            }

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemovePhoto()
        {
            var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "profile");
            foreach (var file in Directory.GetFiles(uploadDir, "admin-photo.*"))
            {
                System.IO.File.Delete(file);
            }
            TempData["Success"] = "Profile photo remove ho gayi!";
            return RedirectToAction(nameof(Profile));
        }

        public async Task<IActionResult> Settings()
        {
            var rates = await _sheets.GetAllMilkRates();
            var products = await _sheets.GetAllDairyProducts();

            ViewBag.ActiveRates = rates.Count(r => r.IsActive);
            ViewBag.InactiveRates = rates.Count(r => !r.IsActive);
            ViewBag.ActiveProducts = products.Count(p => p.IsActive);
            ViewBag.InactiveProducts = products.Count(p => !p.IsActive);
            ViewBag.ExpiredProducts = products.Count(p => p.ExpiryDate < DateTime.Today);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            var tokenPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RajDairyTokens");

            if (Directory.Exists(tokenPath))
            {
                Directory.Delete(tokenPath, true);
            }

            TempData["Success"] = "Successfully logout ho gaye! App restart karein aur phir se login karein.";
            return RedirectToAction("LoggedOut");
        }

        public IActionResult LoggedOut()
        {
            return View();
        }

        private string? GetProfilePhotoPath()
        {
            var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "profile");
            if (!Directory.Exists(uploadDir)) return null;

            var photoFile = Directory.GetFiles(uploadDir, "admin-photo.*").FirstOrDefault();
            if (photoFile == null) return null;

            return $"/uploads/profile/{Path.GetFileName(photoFile)}";
        }
    }
}
