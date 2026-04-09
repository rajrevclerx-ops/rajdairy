using Microsoft.AspNetCore.Mvc;
using DairyProductApp.Models;
using DairyProductApp.Services;
using DairyProductApp.Filters;

namespace DairyProductApp.Controllers
{
    [AdminOnly]
    public class OrderController : Controller
    {
        private readonly GoogleSheetsService _sheets;
        private readonly DataFilterService _filter;

        private string Username => HttpContext.Session.GetString("AdminUsername") ?? "";
        private string Role => HttpContext.Session.GetString("AdminRole") ?? "Admin";

        public OrderController(GoogleSheetsService sheets, DataFilterService filter)
        {
            _sheets = sheets;
            _filter = filter;
        }

        public async Task<IActionResult> Index(string? status, string? search, string? dateFrom, string? dateTo)
        {
            var orders = await _filter.GetOrders(Username, Role);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var s))
                orders = orders.Where(x => x.Status == s).ToList();

            if (!string.IsNullOrEmpty(search))
                orders = orders.Where(x =>
                    x.PartnerName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.OrderNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.ProductName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

            if (DateTime.TryParse(dateFrom, out var from))
                orders = orders.Where(x => x.OrderDate >= from).ToList();
            if (DateTime.TryParse(dateTo, out var to))
                orders = orders.Where(x => x.OrderDate <= to).ToList();

            ViewBag.Status = status;
            ViewBag.Search = search;
            return View(orders.OrderByDescending(x => x.CreatedAt).ToList());
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Partners = await _sheets.GetPartnersByUser(HttpContext.Session.GetString("AdminUsername") ?? "", HttpContext.Session.GetString("AdminRole") ?? "Admin");
            ViewBag.Products = await _sheets.GetAllDairyProducts();
            return View(new Order());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order model)
        {
            if (ModelState.IsValid)
            {
                var partner = await _sheets.GetPartnerById(model.PartnerId);
                if (partner != null)
                {
                    model.PartnerName = partner.Name;
                    model.PartnerMobile = partner.Mobile;
                }
                model.TotalAmount = model.Quantity * model.Rate;

                await _sheets.AddOrder(model);
                await _sheets.CreateSystemNotification(
                    "New Order Received",
                    $"{model.PartnerName} ne {model.ProductName} ka order diya - {model.Quantity} {model.Unit}",
                    NotificationType.Order,
                    "/Order");
                TempData["Success"] = $"Order #{model.OrderNumber} successfully create ho gaya!";
                return RedirectToAction("Index");
            }
            ViewBag.Partners = await _sheets.GetPartnersByUser(HttpContext.Session.GetString("AdminUsername") ?? "", HttpContext.Session.GetString("AdminRole") ?? "Admin");
            ViewBag.Products = await _sheets.GetAllDairyProducts();
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var order = await _sheets.GetOrderById(id);
            if (order == null) return NotFound();
            ViewBag.Partners = await _sheets.GetPartnersByUser(HttpContext.Session.GetString("AdminUsername") ?? "", HttpContext.Session.GetString("AdminRole") ?? "Admin");
            ViewBag.Products = await _sheets.GetAllDairyProducts();
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order model)
        {
            if (ModelState.IsValid)
            {
                var partner = await _sheets.GetPartnerById(model.PartnerId);
                if (partner != null)
                {
                    model.PartnerName = partner.Name;
                    model.PartnerMobile = partner.Mobile;
                }
                model.TotalAmount = model.Quantity * model.Rate;

                await _sheets.UpdateOrder(model);
                TempData["Success"] = "Order update ho gaya!";
                return RedirectToAction("Index");
            }
            ViewBag.Partners = await _sheets.GetPartnersByUser(HttpContext.Session.GetString("AdminUsername") ?? "", HttpContext.Session.GetString("AdminRole") ?? "Admin");
            ViewBag.Products = await _sheets.GetAllDairyProducts();
            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _sheets.GetOrderById(id);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            var order = await _sheets.GetOrderById(id);
            if (order == null) return NotFound();

            if (Enum.TryParse<OrderStatus>(newStatus, out var status))
            {
                order.Status = status;
                await _sheets.UpdateOrder(order);

                if (status == OrderStatus.Delivered)
                {
                    await _sheets.CreateSystemNotification(
                        "Order Delivered",
                        $"Order #{order.OrderNumber} {order.PartnerName} ko deliver ho gaya",
                        NotificationType.Success);
                }
                TempData["Success"] = $"Order status {status} ho gaya!";
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id)
        {
            var order = await _sheets.GetOrderById(id);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _sheets.DeleteOrder(id);
            TempData["Success"] = "Order delete ho gaya!";
            return RedirectToAction("Index");
        }

        // Invoice View
        public async Task<IActionResult> Invoice(int id)
        {
            var order = await _sheets.GetOrderById(id);
            if (order == null) return NotFound();
            return View(order);
        }
    }
}
