using Microsoft.AspNetCore.Mvc;
using DairyProductApp.Services;
using DairyProductApp.Filters;

namespace DairyProductApp.Controllers
{
    [AdminOnly]
    public class AdminController : Controller
    {
        private readonly GoogleSheetsService _sheets;

        public AdminController(GoogleSheetsService sheets)
        {
            _sheets = sheets;
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

            // Get profile photo from Google Sheets
            ViewBag.ProfilePhoto = await _sheets.GetProfilePhoto();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhoto(IFormFile photo)
        {
            if (photo != null && photo.Length > 0)
            {
                var allowedTypes = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();

                if (!allowedTypes.Contains(extension))
                {
                    TempData["Error"] = "Sirf JPG, PNG, GIF, WEBP images allowed hain!";
                    return RedirectToAction(nameof(Profile));
                }

                if (photo.Length > 5 * 1024 * 1024)
                {
                    TempData["Error"] = "Photo 5MB se chhoti honi chahiye!";
                    return RedirectToAction(nameof(Profile));
                }

                var base64 = await ImageToBase64(photo);
                if (base64 == null)
                {
                    TempData["Error"] = "Photo 30KB se chhoti honi chahiye! Online compress karo (tinypng.com) phir upload karo.";
                    return RedirectToAction(nameof(Profile));
                }
                await _sheets.SaveProfilePhoto(base64);
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
        public async Task<IActionResult> RemovePhoto()
        {
            await _sheets.DeleteProfilePhoto();
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

            // Get UPI and QR settings
            ViewBag.UpiId = await _sheets.GetSetting("UpiId") ?? "";
            ViewBag.UpiName = await _sheets.GetSetting("UpiName") ?? "Raj Dairy";
            ViewBag.QrCode = await _sheets.GetSetting("QrCode");

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveUpi(string upiId, string upiName)
        {
            await _sheets.SaveSetting("UpiId", upiId ?? "");
            await _sheets.SaveSetting("UpiName", upiName ?? "Raj Dairy");
            TempData["Success"] = "UPI details save ho gayi!";
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadQr(IFormFile qrImage)
        {
            if (qrImage != null && qrImage.Length > 0)
            {
                var extension = Path.GetExtension(qrImage.FileName).ToLowerInvariant();
                if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension))
                {
                    TempData["Error"] = "Sirf JPG, PNG, WEBP allowed hain!";
                    return RedirectToAction(nameof(Settings));
                }

                var base64 = await ImageToBase64(qrImage);
                if (base64 == null)
                {
                    TempData["Error"] = "Image 30KB se chhoti honi chahiye! Phone se photo lene ki jagah screenshot lo ya online compress karo (tinypng.com).";
                    return RedirectToAction(nameof(Settings));
                }
                await _sheets.SaveSetting("QrCode", base64);
                TempData["Success"] = "QR Code upload ho gaya!";
            }
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveQr()
        {
            await _sheets.DeleteSetting("QrCode");
            TempData["Success"] = "QR Code remove ho gaya!";
            return RedirectToAction(nameof(Settings));
        }

        // API to get profile photo (used by Layout)
        [HttpGet]
        public async Task<IActionResult> GetPhoto()
        {
            var photo = await _sheets.GetProfilePhoto();
            return Json(new { photo = photo ?? "" });
        }

        // Convert image to base64 - reject if too large for Google Sheets
        private async Task<string?> ImageToBase64(IFormFile file, int maxKB = 30)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var mime = ext == ".png" ? "image/png" : "image/jpeg";

            // Check if base64 will fit in Google Sheets cell (~50K char limit)
            if (bytes.Length > maxKB * 1024)
            {
                return null; // Too large
            }

            return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
        }
    }
}
