using DairyProductApp.Models;

namespace DairyProductApp.Services
{
    /// <summary>
    /// Centralized data filtering - Admin sees own data, SuperAdmin sees all
    /// </summary>
    public class DataFilterService
    {
        private readonly GoogleSheetsService _sheets;

        public DataFilterService(GoogleSheetsService sheets)
        {
            _sheets = sheets;
        }

        // Get current user's partner names (for filtering milk collections etc.)
        private async Task<HashSet<string>> GetUserPartnerNames(string username, string role)
        {
            var partners = await _sheets.GetPartnersByUser(username, role);
            return partners.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private async Task<HashSet<int>> GetUserPartnerIds(string username, string role)
        {
            var partners = await _sheets.GetPartnersByUser(username, role);
            return partners.Select(p => p.Id).ToHashSet();
        }

        // ============ FILTERED DATA METHODS ============

        public async Task<List<MilkCollection>> GetMilkCollections(string username, string role)
        {
            var all = await _sheets.GetAllMilkCollections();
            if (role == "SuperAdmin") return all;
            var names = await GetUserPartnerNames(username, role);
            return all.Where(m => names.Contains(m.FarmerName)).ToList();
        }

        public async Task<List<Transaction>> GetTransactions(string username, string role)
        {
            var all = await _sheets.GetAllTransactions();
            if (role == "SuperAdmin") return all;
            var ids = await GetUserPartnerIds(username, role);
            return all.Where(t => ids.Contains(t.PartnerId)).ToList();
        }

        public async Task<List<Order>> GetOrders(string username, string role)
        {
            var all = await _sheets.GetAllOrders();
            if (role == "SuperAdmin") return all;
            var ids = await GetUserPartnerIds(username, role);
            return all.Where(o => ids.Contains(o.PartnerId)).ToList();
        }

        public async Task<List<Subscription>> GetSubscriptions(string username, string role)
        {
            var all = await _sheets.GetAllSubscriptions();
            if (role == "SuperAdmin") return all;
            var ids = await GetUserPartnerIds(username, role);
            return all.Where(s => ids.Contains(s.PartnerId)).ToList();
        }

        public async Task<List<GheeProduct>> GetGheeProducts(string username, string role)
        {
            var all = await _sheets.GetAllGheeProducts();
            if (role == "SuperAdmin") return all;
            // Filter by batch number prefix matching username (simple approach)
            // For now, show all ghee to all admins since ghee is shared resource
            return all;
        }

        public async Task<List<DairyProduct>> GetDairyProducts(string username, string role)
        {
            var all = await _sheets.GetAllDairyProducts();
            // Products are shared across dairy - all admins see same products
            return all;
        }

        public async Task<List<Expense>> GetExpenses(string username, string role)
        {
            var all = await _sheets.GetAllExpenses();
            if (role == "SuperAdmin") return all;
            // Filter expenses by description containing username or show all
            // For now expenses are shared - admin should see all expenses of their dairy
            return all;
        }

        public async Task<List<Notification>> GetNotifications(string username, string role)
        {
            var all = await _sheets.GetAllNotifications();
            // Notifications are shared
            return all;
        }
    }
}
