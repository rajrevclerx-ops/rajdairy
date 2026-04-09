using Microsoft.AspNetCore.Mvc;
using DairyProductApp.Models;
using DairyProductApp.Services;
using DairyProductApp.Filters;

namespace DairyProductApp.Controllers
{
    [AdminOnly]
    public class GheeProductController : Controller
    {
        private readonly GoogleSheetsService _sheets;

        public GheeProductController(GoogleSheetsService sheets)
        {
            _sheets = sheets;
        }

        private string Username => HttpContext.Session.GetString("AdminUsername") ?? "";
        private string Role => HttpContext.Session.GetString("AdminRole") ?? "Admin";

        public async Task<IActionResult> Index(GheeType? gheeType, QualityGrade? quality)
        {
            var all = await _sheets.GetGheeProductsByUser(Username, Role);

            if (gheeType.HasValue)
                all = all.Where(g => g.GheeType == gheeType.Value).ToList();
            if (quality.HasValue)
                all = all.Where(g => g.Quality == quality.Value).ToList();

            ViewBag.GheeType = gheeType;
            ViewBag.Quality = quality;

            var products = all.OrderByDescending(g => g.ProductionDate).ToList();
            return View(products);
        }

        public IActionResult Create()
        {
            return View(new GheeProduct());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GheeProduct gheeProduct)
        {
            if (ModelState.IsValid)
            {
                if (gheeProduct.MilkUsedLiters > 0)
                {
                    gheeProduct.YieldRate = Math.Round((gheeProduct.GheeProducedKg / gheeProduct.MilkUsedLiters) * 100, 2);
                }
                gheeProduct.TotalValue = gheeProduct.GheeProducedKg * gheeProduct.PricePerKg;
                gheeProduct.StockKg = gheeProduct.GheeProducedKg;
                gheeProduct.CreatedAt = DateTime.Now;
                gheeProduct.CreatedBy = Username;

                await _sheets.AddGheeProduct(gheeProduct);
                TempData["Success"] = "Ghee production record successfully add ho gaya!";
                return RedirectToAction(nameof(Index));
            }
            return View(gheeProduct);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var gheeProduct = await _sheets.GetGheeProductById(id.Value);
            if (gheeProduct == null) return NotFound();
            return View(gheeProduct);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, GheeProduct gheeProduct)
        {
            if (id != gheeProduct.Id) return NotFound();

            if (ModelState.IsValid)
            {
                if (gheeProduct.MilkUsedLiters > 0)
                {
                    gheeProduct.YieldRate = Math.Round((gheeProduct.GheeProducedKg / gheeProduct.MilkUsedLiters) * 100, 2);
                }
                gheeProduct.TotalValue = gheeProduct.GheeProducedKg * gheeProduct.PricePerKg;

                await _sheets.UpdateGheeProduct(gheeProduct);
                TempData["Success"] = "Ghee production record update ho gaya!";
                return RedirectToAction(nameof(Index));
            }
            return View(gheeProduct);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var gheeProduct = await _sheets.GetGheeProductById(id.Value);
            if (gheeProduct == null) return NotFound();
            return View(gheeProduct);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var gheeProduct = await _sheets.GetGheeProductById(id.Value);
            if (gheeProduct == null) return NotFound();
            return View(gheeProduct);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _sheets.DeleteGheeProduct(id);
            TempData["Success"] = "Ghee production record delete ho gaya!";
            return RedirectToAction(nameof(Index));
        }
    }
}
