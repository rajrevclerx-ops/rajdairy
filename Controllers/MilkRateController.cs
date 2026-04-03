using Microsoft.AspNetCore.Mvc;
using DairyProductApp.Models;
using DairyProductApp.Services;

namespace DairyProductApp.Controllers
{
    public class MilkRateController : Controller
    {
        private readonly GoogleSheetsService _sheets;

        public MilkRateController(GoogleSheetsService sheets)
        {
            _sheets = sheets;
        }

        public async Task<IActionResult> Index()
        {
            var rates = (await _sheets.GetAllMilkRates())
                .OrderBy(r => r.MilkType)
                .ThenBy(r => r.MinFat)
                .ToList();
            return View(rates);
        }

        public IActionResult Create()
        {
            return View(new MilkRate());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MilkRate milkRate)
        {
            if (ModelState.IsValid)
            {
                await _sheets.AddMilkRate(milkRate);
                TempData["Success"] = "Milk rate successfully add ho gayi!";
                return RedirectToAction(nameof(Index));
            }
            return View(milkRate);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var milkRate = await _sheets.GetMilkRateById(id.Value);
            if (milkRate == null) return NotFound();
            return View(milkRate);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MilkRate milkRate)
        {
            if (id != milkRate.Id) return NotFound();

            if (ModelState.IsValid)
            {
                await _sheets.UpdateMilkRate(milkRate);
                TempData["Success"] = "Milk rate successfully update ho gayi!";
                return RedirectToAction(nameof(Index));
            }
            return View(milkRate);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _sheets.DeleteMilkRate(id);
            TempData["Success"] = "Milk rate delete ho gayi!";
            return RedirectToAction(nameof(Index));
        }
    }
}
