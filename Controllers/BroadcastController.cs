using Microsoft.AspNetCore.Mvc;
using DairyProductApp.Models;
using DairyProductApp.Services;
using DairyProductApp.Filters;

namespace DairyProductApp.Controllers
{
    [AdminOnly]
    public class BroadcastController : Controller
    {
        private readonly GoogleSheetsService _sheets;

        public BroadcastController(GoogleSheetsService sheets)
        {
            _sheets = sheets;
        }

        public async Task<IActionResult> Index()
        {
            var partners = await _sheets.GetAllPartners();
            ViewBag.Partners = partners.Where(p => p.IsActive).OrderBy(p => p.Name).ToList();
            ViewBag.TotalPartners = partners.Count(p => p.IsActive);
            ViewBag.Suppliers = partners.Count(p => p.IsActive && (p.Type == PartnerType.Supplier || p.Type == PartnerType.Both));
            ViewBag.Buyers = partners.Count(p => p.IsActive && (p.Type == PartnerType.Buyer || p.Type == PartnerType.Both));
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(string message, string sendTo, int[]? selectedPartners)
        {
            var partners = await _sheets.GetAllPartners();
            var targets = new List<Partner>();

            switch (sendTo)
            {
                case "all":
                    targets = partners.Where(p => p.IsActive).ToList();
                    break;
                case "suppliers":
                    targets = partners.Where(p => p.IsActive && (p.Type == PartnerType.Supplier || p.Type == PartnerType.Both)).ToList();
                    break;
                case "buyers":
                    targets = partners.Where(p => p.IsActive && (p.Type == PartnerType.Buyer || p.Type == PartnerType.Both)).ToList();
                    break;
                case "selected":
                    if (selectedPartners != null)
                        targets = partners.Where(p => selectedPartners.Contains(p.Id)).ToList();
                    break;
            }

            if (!targets.Any())
            {
                TempData["Error"] = "Koi partner select nahi hua!";
                return RedirectToAction(nameof(Index));
            }

            // Build WhatsApp broadcast URL list
            var encodedMsg = System.Net.WebUtility.UrlEncode(message);
            var broadcastLinks = targets.Select(p =>
            {
                var mobile = p.Mobile;
                if (!mobile.StartsWith("+")) mobile = "91" + mobile;
                return new { p.Name, p.Mobile, Url = $"https://wa.me/{mobile}?text={encodedMsg}" };
            }).ToList();

            TempData["Success"] = $"{targets.Count} partners ko message ready hai!";
            TempData["BroadcastLinks"] = System.Text.Json.JsonSerializer.Serialize(broadcastLinks);
            TempData["BroadcastMessage"] = message;
            TempData["BroadcastCount"] = targets.Count.ToString();

            return RedirectToAction(nameof(Index));
        }

        // Quick Templates
        [HttpGet]
        public IActionResult GetTemplates()
        {
            var templates = new[]
            {
                new { title = "Rate Change", text = "📢 *Raj Dairy - Rate Update*\n\nNamaste! Aapko suchit kiya jata hai ki kal se milk ka rate badal gaya hai.\n\n🐄 Cow Milk: ₹__/L\n🐃 Buffalo Milk: ₹__/L\n\nDhanyavaad!\n- Raj Dairy" },
                new { title = "Payment Done", text = "💰 *Raj Dairy - Payment*\n\nNamaste! Aapki payment ₹___ kar di gayi hai.\n\nApna hisab dekhein:\n🔗 rajdairy.onrender.com/Public/Login\n\nDhanyavaad!\n- Raj Dairy" },
                new { title = "Holiday Notice", text = "📢 *Raj Dairy - Notice*\n\nNamaste! Aapko suchit kiya jata hai ki ___ ko dairy band rahegi.\n\n___ se collection phir se shuru hoga.\n\nDhanyavaad!\n- Raj Dairy" },
                new { title = "Collection Time", text = "⏰ *Raj Dairy - Collection Time*\n\nNamaste! Kal se collection ka time badal gaya hai:\n\n🌅 Morning: __ baje se __ baje tak\n🌆 Evening: __ baje se __ baje tak\n\nDhanyavaad!\n- Raj Dairy" },
                new { title = "Festival Greetings", text = "🎉 *Raj Dairy*\n\nAap sabhi ko ___ ki hardik shubhkamnayein!\n\nAapka saath hamesha rahega.\n\n🙏 Dhanyavaad!\n- Raj Dairy Parivar" },
                new { title = "New Product", text = "🆕 *Raj Dairy - Naya Product*\n\nNamaste! Hum le aaye hain aapke liye:\n\n🥛 ___\n💰 Price: ₹___\n\nOrder karne ke liye sampark karein!\n- Raj Dairy" }
            };
            return Json(templates);
        }
    }
}
