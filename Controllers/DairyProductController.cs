using Microsoft.AspNetCore.Mvc;
using DairyProductApp.Models;
using DairyProductApp.Services;
using DairyProductApp.Filters;

namespace DairyProductApp.Controllers
{
    [AdminOnly]
    public class DairyProductController : Controller
    {
        private readonly GoogleSheetsService _sheets;

        public DairyProductController(GoogleSheetsService sheets)
        {
            _sheets = sheets;
        }

        private string Username => HttpContext.Session.GetString("AdminUsername") ?? "";
        private string Role => HttpContext.Session.GetString("AdminRole") ?? "Admin";

        public async Task<IActionResult> Index(string? search, ProductCategory? category)
        {
            var all = await _sheets.GetDairyProductsByUser(Username, Role);

            if (!string.IsNullOrEmpty(search))
                all = all.Where(p => p.ProductName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            if (category.HasValue)
                all = all.Where(p => p.Category == category.Value).ToList();

            ViewBag.Search = search;
            ViewBag.Category = category;

            var products = all.OrderBy(p => p.Category).ThenBy(p => p.ProductName).ToList();
            return View(products);
        }

        public IActionResult Create()
        {
            return View(new DairyProduct());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DairyProduct product)
        {
            if (ModelState.IsValid)
            {
                product.CreatedAt = DateTime.Now;
                product.CreatedBy = Username;
                await _sheets.AddDairyProduct(product);
                TempData["Success"] = "Product successfully add ho gaya!";
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var product = await _sheets.GetDairyProductById(id.Value);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DairyProduct product)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                await _sheets.UpdateDairyProduct(product);
                TempData["Success"] = "Product successfully update ho gaya!";
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var product = await _sheets.GetDairyProductById(id.Value);
            if (product == null) return NotFound();
            return View(product);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var product = await _sheets.GetDairyProductById(id.Value);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _sheets.DeleteDairyProduct(id);
            TempData["Success"] = "Product delete ho gaya!";
            return RedirectToAction(nameof(Index));
        }
    }
}
