using Microsoft.AspNetCore.Mvc;
using DairyProductApp.Models;
using DairyProductApp.Services;

namespace DairyProductApp.Controllers
{
    public class MilkCollectionController : Controller
    {
        private readonly GoogleSheetsService _sheets;

        public MilkCollectionController(GoogleSheetsService sheets)
        {
            _sheets = sheets;
        }

        public async Task<IActionResult> Index(string? searchFarmer, MilkType? milkType, DateTime? fromDate, DateTime? toDate)
        {
            var all = await _sheets.GetAllMilkCollections();

            if (!string.IsNullOrEmpty(searchFarmer))
                all = all.Where(m => m.FarmerName.Contains(searchFarmer, StringComparison.OrdinalIgnoreCase)).ToList();
            if (milkType.HasValue)
                all = all.Where(m => m.MilkType == milkType.Value).ToList();
            if (fromDate.HasValue)
                all = all.Where(m => m.CollectionDate >= fromDate.Value).ToList();
            if (toDate.HasValue)
                all = all.Where(m => m.CollectionDate <= toDate.Value).ToList();

            ViewBag.SearchFarmer = searchFarmer;
            ViewBag.MilkType = milkType;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            var collections = all
                .OrderByDescending(m => m.CollectionDate)
                .ThenByDescending(m => m.CreatedAt)
                .ToList();

            return View(collections);
        }

        public IActionResult Create()
        {
            return View(new MilkCollection());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MilkCollection milkCollection)
        {
            if (ModelState.IsValid)
            {
                var rate = await _sheets.FindMatchingRate(milkCollection.MilkType, milkCollection.FatPercentage, milkCollection.SNFPercentage);
                if (rate != null)
                {
                    milkCollection.RatePerLiter = rate.RatePerLiter;
                }

                milkCollection.TotalAmount = milkCollection.Quantity * milkCollection.RatePerLiter;
                milkCollection.CreatedAt = DateTime.Now;

                await _sheets.AddMilkCollection(milkCollection);
                TempData["Success"] = "Milk collection successfully add ho gayi!";
                return RedirectToAction(nameof(Index));
            }
            return View(milkCollection);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var milkCollection = await _sheets.GetMilkCollectionById(id.Value);
            if (milkCollection == null) return NotFound();
            return View(milkCollection);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MilkCollection milkCollection)
        {
            if (id != milkCollection.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var rate = await _sheets.FindMatchingRate(milkCollection.MilkType, milkCollection.FatPercentage, milkCollection.SNFPercentage);
                if (rate != null)
                {
                    milkCollection.RatePerLiter = rate.RatePerLiter;
                }

                milkCollection.TotalAmount = milkCollection.Quantity * milkCollection.RatePerLiter;

                await _sheets.UpdateMilkCollection(milkCollection);
                TempData["Success"] = "Milk collection successfully update ho gayi!";
                return RedirectToAction(nameof(Index));
            }
            return View(milkCollection);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var milkCollection = await _sheets.GetMilkCollectionById(id.Value);
            if (milkCollection == null) return NotFound();
            return View(milkCollection);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var milkCollection = await _sheets.GetMilkCollectionById(id.Value);
            if (milkCollection == null) return NotFound();
            return View(milkCollection);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _sheets.DeleteMilkCollection(id);
            TempData["Success"] = "Milk collection delete ho gayi!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetRate(MilkType milkType, decimal fat, decimal snf)
        {
            var rate = await _sheets.FindMatchingRate(milkType, fat, snf);
            return Json(new { ratePerLiter = rate?.RatePerLiter ?? 0 });
        }
    }
}
