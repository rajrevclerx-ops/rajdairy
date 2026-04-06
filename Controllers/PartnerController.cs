using Microsoft.AspNetCore.Mvc;
using DairyProductApp.Models;
using DairyProductApp.Services;
using DairyProductApp.Filters;

namespace DairyProductApp.Controllers
{
    [AdminOnly]
    public class PartnerController : Controller
    {
        private readonly GoogleSheetsService _sheets;

        public PartnerController(GoogleSheetsService sheets)
        {
            _sheets = sheets;
        }

        public async Task<IActionResult> Index()
        {
            var partners = await _sheets.GetAllPartners();
            return View(partners.OrderBy(p => p.Name).ToList());
        }

        public IActionResult Create()
        {
            return View(new Partner());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Partner partner)
        {
            if (ModelState.IsValid)
            {
                partner.CreatedAt = DateTime.Now;
                await _sheets.AddPartner(partner);
                TempData["Success"] = $"Partner '{partner.Name}' add ho gaya! Access Code: {partner.AccessCode}";
                return RedirectToAction(nameof(Index));
            }
            return View(partner);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var partner = await _sheets.GetPartnerById(id.Value);
            if (partner == null) return NotFound();
            return View(partner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Partner partner)
        {
            if (id != partner.Id) return NotFound();
            if (ModelState.IsValid)
            {
                await _sheets.UpdatePartner(partner);
                TempData["Success"] = "Partner update ho gaya!";
                return RedirectToAction(nameof(Index));
            }
            return View(partner);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var partner = await _sheets.GetPartnerById(id.Value);
            if (partner == null) return NotFound();
            return View(partner);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _sheets.DeletePartner(id);
            TempData["Success"] = "Partner delete ho gaya!";
            return RedirectToAction(nameof(Index));
        }

        // Partner ka Ledger (hisab-kitab)
        public async Task<IActionResult> Ledger(int? id)
        {
            if (id == null) return NotFound();
            var partner = await _sheets.GetPartnerById(id.Value);
            if (partner == null) return NotFound();

            var transactions = await _sheets.GetTransactionsByPartnerId(id.Value);

            var model = new PartnerLedgerViewModel
            {
                Partner = partner,
                Transactions = transactions,
                TotalReceived = transactions.Where(t => t.Type == TransactionType.Received).Sum(t => t.TotalAmount),
                TotalGiven = transactions.Where(t => t.Type == TransactionType.Given).Sum(t => t.TotalAmount),
                TotalPending = transactions.Where(t => t.PaymentStatus == PaymentStatus.Pending).Sum(t => t.TotalAmount),
                Balance = transactions.Where(t => t.Type == TransactionType.Given).Sum(t => t.TotalAmount)
                        - transactions.Where(t => t.Type == TransactionType.Received).Sum(t => t.TotalAmount)
            };

            return View(model);
        }
    }
}
