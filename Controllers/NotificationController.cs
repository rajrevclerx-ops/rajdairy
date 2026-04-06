using Microsoft.AspNetCore.Mvc;
using DairyProductApp.Services;
using DairyProductApp.Filters;

namespace DairyProductApp.Controllers
{
    [AdminOnly]
    public class NotificationController : Controller
    {
        private readonly GoogleSheetsService _sheets;

        public NotificationController(GoogleSheetsService sheets)
        {
            _sheets = sheets;
        }

        public async Task<IActionResult> Index()
        {
            var notifications = await _sheets.GetAllNotifications();
            return View(notifications);
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var notifications = await _sheets.GetAllNotifications();
            var unread = notifications.Count(n => !n.IsRead);
            var recent = notifications.Take(5).Select(n => new
            {
                n.Id, n.Title, n.Message, n.Icon, n.Link, n.IsRead,
                Type = n.Type.ToString(),
                Time = GetTimeAgo(n.CreatedAt)
            });
            return Json(new { unreadCount = unread, notifications = recent });
        }

        [HttpPost]
        public async Task<IActionResult> MarkRead(int id)
        {
            await _sheets.MarkNotificationRead(id);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            await _sheets.MarkAllNotificationsRead();
            TempData["Success"] = "Sab notifications read ho gayi!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _sheets.DeleteNotification(id);
            return Json(new { success = true });
        }

        private static string GetTimeAgo(DateTime dateTime)
        {
            var span = DateTime.Now - dateTime;
            if (span.TotalMinutes < 1) return "Abhi abhi";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min pehle";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} ghante pehle";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays} din pehle";
            return dateTime.ToString("dd MMM yyyy");
        }
    }
}
