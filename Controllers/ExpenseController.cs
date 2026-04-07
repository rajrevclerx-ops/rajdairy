using Microsoft.AspNetCore.Mvc;
using DairyProductApp.Models;
using DairyProductApp.Services;
using DairyProductApp.Filters;

namespace DairyProductApp.Controllers
{
    [AdminOnly]
    public class ExpenseController : Controller
    {
        private readonly GoogleSheetsService _sheets;

        public ExpenseController(GoogleSheetsService sheets)
        {
            _sheets = sheets;
        }

        public async Task<IActionResult> Index(DateTime? from, DateTime? to)
        {
            var expenses = await _sheets.GetAllExpenses();
            from ??= DateTime.Today.AddDays(-30);
            to ??= DateTime.Today;

            expenses = expenses.Where(e => e.ExpenseDate >= from && e.ExpenseDate <= to)
                .OrderByDescending(e => e.ExpenseDate).ToList();

            ViewBag.From = from.Value.ToString("yyyy-MM-dd");
            ViewBag.To = to.Value.ToString("yyyy-MM-dd");
            ViewBag.TotalExpenses = expenses.Sum(e => e.Amount);

            return View(expenses);
        }

        public IActionResult Create()
        {
            return View(new Expense());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Expense expense)
        {
            if (ModelState.IsValid)
            {
                expense.CreatedAt = DateTime.Now;
                await _sheets.AddExpense(expense);
                TempData["Success"] = "Expense add ho gaya!";
                return RedirectToAction(nameof(Index));
            }
            return View(expense);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var expense = await _sheets.GetExpenseById(id.Value);
            if (expense == null) return NotFound();
            return View(expense);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Expense expense)
        {
            if (id != expense.Id) return NotFound();
            if (ModelState.IsValid)
            {
                await _sheets.UpdateExpense(expense);
                TempData["Success"] = "Expense update ho gaya!";
                return RedirectToAction(nameof(Index));
            }
            return View(expense);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _sheets.DeleteExpense(id);
            TempData["Success"] = "Expense delete ho gaya!";
            return RedirectToAction(nameof(Index));
        }

        // Profit & Loss Report
        public async Task<IActionResult> ProfitLoss(DateTime? from, DateTime? to)
        {
            from ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            to ??= DateTime.Today;

            var collections = (await _sheets.GetAllMilkCollections())
                .Where(m => m.CollectionDate >= from && m.CollectionDate <= to).ToList();
            var orders = (await _sheets.GetAllOrders())
                .Where(o => o.OrderDate >= from && o.OrderDate <= to).ToList();
            var transactions = (await _sheets.GetAllTransactions())
                .Where(t => t.TransactionDate >= from && t.TransactionDate <= to).ToList();
            var expenses = (await _sheets.GetAllExpenses())
                .Where(e => e.ExpenseDate >= from && e.ExpenseDate <= to).ToList();

            var model = new ProfitLossViewModel
            {
                FromDate = from.Value,
                ToDate = to.Value,
                MilkSalesIncome = transactions.Where(t => t.Type == TransactionType.Given && t.Item == TransactionItem.Milk).Sum(t => t.TotalAmount)
                    + orders.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount),
                GheeSalesIncome = transactions.Where(t => t.Type == TransactionType.Given && t.Item == TransactionItem.Ghee).Sum(t => t.TotalAmount),
                ProductSalesIncome = transactions.Where(t => t.Type == TransactionType.Given && (t.Item != TransactionItem.Milk && t.Item != TransactionItem.Ghee && t.Item != TransactionItem.Cash)).Sum(t => t.TotalAmount),
                OtherIncome = transactions.Where(t => t.Type == TransactionType.Received && t.Item == TransactionItem.Cash).Sum(t => t.TotalAmount),
                RecentExpenses = expenses.OrderByDescending(e => e.ExpenseDate).Take(10).ToList()
            };

            // Group expenses by category
            var grouped = expenses.GroupBy(e => e.Category).Select(g => new CategoryExpense
            {
                Category = g.Key.ToString(),
                Amount = g.Sum(x => x.Amount)
            }).OrderByDescending(c => c.Amount).ToList();

            var totalExp = grouped.Sum(g => g.Amount);
            foreach (var g in grouped)
            {
                g.Percentage = totalExp > 0 ? (g.Amount / totalExp) * 100 : 0;
            }
            model.ExpensesByCategory = grouped;

            return View(model);
        }
    }
}
