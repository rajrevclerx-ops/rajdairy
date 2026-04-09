using Microsoft.AspNetCore.Mvc;
using DairyProductApp.Models;
using DairyProductApp.Services;
using DairyProductApp.Filters;

namespace DairyProductApp.Controllers
{
    [AdminOnly]
    public class SubscriptionController : Controller
    {
        private readonly GoogleSheetsService _sheets;

        public SubscriptionController(GoogleSheetsService sheets)
        {
            _sheets = sheets;
        }

        public async Task<IActionResult> Index(string? status, string? search)
        {
            var subscriptions = await _sheets.GetAllSubscriptions();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<SubscriptionStatus>(status, out var s))
                subscriptions = subscriptions.Where(x => x.Status == s).ToList();

            if (!string.IsNullOrEmpty(search))
                subscriptions = subscriptions.Where(x =>
                    x.PartnerName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

            ViewBag.Status = status;
            ViewBag.Search = search;
            return View(subscriptions.OrderByDescending(x => x.CreatedAt).ToList());
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Partners = await _sheets.GetPartnersByUser(HttpContext.Session.GetString("AdminUsername") ?? "", HttpContext.Session.GetString("AdminRole") ?? "Admin");
            return View(new Subscription());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Subscription model)
        {
            if (ModelState.IsValid)
            {
                var partner = await _sheets.GetPartnerById(model.PartnerId);
                if (partner != null) model.PartnerName = partner.Name;

                await _sheets.AddSubscription(model);
                await _sheets.CreateSystemNotification(
                    "New Subscription",
                    $"{model.PartnerName} ne {model.Product} ka subscription liya - {model.DailyQuantity} {model.Unit}/day",
                    NotificationType.Subscription,
                    "/Subscription");
                TempData["Success"] = "Subscription successfully create ho gaya!";
                return RedirectToAction("Index");
            }
            ViewBag.Partners = await _sheets.GetPartnersByUser(HttpContext.Session.GetString("AdminUsername") ?? "", HttpContext.Session.GetString("AdminRole") ?? "Admin");
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var sub = await _sheets.GetSubscriptionById(id);
            if (sub == null) return NotFound();
            ViewBag.Partners = await _sheets.GetPartnersByUser(HttpContext.Session.GetString("AdminUsername") ?? "", HttpContext.Session.GetString("AdminRole") ?? "Admin");
            return View(sub);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Subscription model)
        {
            if (ModelState.IsValid)
            {
                var partner = await _sheets.GetPartnerById(model.PartnerId);
                if (partner != null) model.PartnerName = partner.Name;

                await _sheets.UpdateSubscription(model);
                TempData["Success"] = "Subscription update ho gaya!";
                return RedirectToAction("Index");
            }
            ViewBag.Partners = await _sheets.GetPartnersByUser(HttpContext.Session.GetString("AdminUsername") ?? "", HttpContext.Session.GetString("AdminRole") ?? "Admin");
            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var sub = await _sheets.GetSubscriptionById(id);
            if (sub == null) return NotFound();
            return View(sub);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, string newStatus)
        {
            var sub = await _sheets.GetSubscriptionById(id);
            if (sub == null) return NotFound();

            if (Enum.TryParse<SubscriptionStatus>(newStatus, out var status))
            {
                sub.Status = status;
                await _sheets.UpdateSubscription(sub);
                TempData["Success"] = $"Subscription {status} ho gaya!";
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id)
        {
            var sub = await _sheets.GetSubscriptionById(id);
            if (sub == null) return NotFound();
            return View(sub);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _sheets.DeleteSubscription(id);
            TempData["Success"] = "Subscription delete ho gaya!";
            return RedirectToAction("Index");
        }
    }
}
