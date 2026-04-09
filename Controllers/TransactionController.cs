using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using DairyProductApp.Models;
using DairyProductApp.Services;
using DairyProductApp.Filters;

namespace DairyProductApp.Controllers
{
    [AdminOnly]
    public class TransactionController : Controller
    {
        private readonly GoogleSheetsService _sheets;

        public TransactionController(GoogleSheetsService sheets)
        {
            _sheets = sheets;
        }

        public async Task<IActionResult> Index(int? partnerId)
        {
            var transactions = await _sheets.GetAllTransactions();
            if (partnerId.HasValue)
            {
                transactions = transactions.Where(t => t.PartnerId == partnerId.Value).ToList();
                ViewBag.PartnerId = partnerId;
                var partner = await _sheets.GetPartnerById(partnerId.Value);
                ViewBag.PartnerName = partner?.Name;
            }
            return View(transactions.OrderByDescending(t => t.TransactionDate).ToList());
        }

        public async Task<IActionResult> Create(int? partnerId)
        {
            await LoadPartnerDropdown();
            var txn = new Transaction();
            if (partnerId.HasValue)
            {
                txn.PartnerId = partnerId.Value;
                var partner = await _sheets.GetPartnerById(partnerId.Value);
                if (partner != null) txn.PartnerName = partner.Name;
            }
            return View(txn);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Transaction transaction)
        {
            if (ModelState.IsValid)
            {
                var partner = await _sheets.GetPartnerById(transaction.PartnerId);
                if (partner != null) transaction.PartnerName = partner.Name;

                transaction.TotalAmount = transaction.Quantity * transaction.Rate;
                transaction.CreatedAt = DateTime.Now;

                await _sheets.AddTransaction(transaction);
                TempData["Success"] = "Transaction add ho gayi!";
                return RedirectToAction("Ledger", "Partner", new { id = transaction.PartnerId });
            }
            await LoadPartnerDropdown();
            return View(transaction);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var txn = await _sheets.GetTransactionById(id.Value);
            if (txn == null) return NotFound();
            await LoadPartnerDropdown();
            return View(txn);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Transaction transaction)
        {
            if (id != transaction.Id) return NotFound();
            if (ModelState.IsValid)
            {
                var partner = await _sheets.GetPartnerById(transaction.PartnerId);
                if (partner != null) transaction.PartnerName = partner.Name;
                transaction.TotalAmount = transaction.Quantity * transaction.Rate;

                await _sheets.UpdateTransaction(transaction);
                TempData["Success"] = "Transaction update ho gayi!";
                return RedirectToAction("Ledger", "Partner", new { id = transaction.PartnerId });
            }
            await LoadPartnerDropdown();
            return View(transaction);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int partnerId)
        {
            await _sheets.DeleteTransaction(id);
            TempData["Success"] = "Transaction delete ho gayi!";
            return RedirectToAction("Ledger", "Partner", new { id = partnerId });
        }

        private async Task LoadPartnerDropdown()
        {
            var partners = await _sheets.GetPartnersByUser(HttpContext.Session.GetString("AdminUsername") ?? "", HttpContext.Session.GetString("AdminRole") ?? "Admin");
            ViewBag.Partners = new SelectList(partners.Where(p => p.IsActive).OrderBy(p => p.Name), "Id", "Name");
        }
    }
}
