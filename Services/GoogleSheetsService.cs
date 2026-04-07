using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using DairyProductApp.Models;

namespace DairyProductApp.Services
{
    public class GoogleSheetsService
    {
        private readonly SheetsService _service;
        private readonly string _spreadsheetId;

        // Sheet/Tab names
        private const string MilkCollectionSheet = "MilkCollection";
        private const string MilkRateSheet = "MilkRates";
        private const string DairyProductSheet = "DairyProducts";
        private const string GheeProductSheet = "GheeProducts";
        private const string SettingsSheet = "Settings";
        private const string PartnerSheet = "Partners";
        private const string TransactionSheet = "Transactions";
        private const string SubscriptionSheet = "Subscriptions";
        private const string OrderSheet = "Orders";
        private const string NotificationSheet = "Notifications";
        private const string ExpenseSheet = "Expenses";

        public GoogleSheetsService(IConfiguration configuration)
        {
            _spreadsheetId = configuration["GoogleSheets:SpreadsheetId"]!;

            GoogleCredential credential;

            // Check if Service Account JSON is provided via environment variable (for cloud deployment)
            var serviceAccountJson = Environment.GetEnvironmentVariable("GOOGLE_SERVICE_ACCOUNT_JSON");
            if (!string.IsNullOrEmpty(serviceAccountJson))
            {
                // Cloud mode: use Service Account from environment variable
                credential = GoogleCredential.FromJson(serviceAccountJson)
                    .CreateScoped(SheetsService.Scope.Spreadsheets);
            }
            else
            {
                // Local mode: use OAuth2 browser login
                var credentialPath = configuration["GoogleSheets:CredentialPath"] ?? "credentials.json";
                using var stream = new FileStream(credentialPath, FileMode.Open, FileAccess.Read);
                var secrets = GoogleClientSecrets.FromStream(stream);

                var oauthCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    secrets.Secrets,
                    new[] { SheetsService.Scope.Spreadsheets },
                    "user",
                    CancellationToken.None,
                    new FileDataStore("RajDairyTokens", true)
                ).Result;

                _service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = oauthCredential,
                    ApplicationName = "Raj Dairy"
                });
                return;
            }

            _service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Raj Dairy"
            });
        }

        // ============ INITIALIZE SHEETS WITH HEADERS ============
        public async Task InitializeSheetsAsync()
        {
            var spreadsheet = await _service.Spreadsheets.Get(_spreadsheetId).ExecuteAsync();
            var existingSheets = spreadsheet.Sheets.Select(s => s.Properties.Title).ToList();

            var sheetsToCreate = new Dictionary<string, IList<object>>
            {
                [MilkCollectionSheet] = new List<object> { "Id", "FarmerName", "MobileNumber", "MilkType", "Quantity", "FatPercentage", "SNFPercentage", "RatePerLiter", "TotalAmount", "Shift", "CollectionDate", "CreatedAt", "Remarks" },
                [MilkRateSheet] = new List<object> { "Id", "MilkType", "MinFat", "MaxFat", "MinSNF", "MaxSNF", "RatePerLiter", "EffectiveFrom", "IsActive" },
                [DairyProductSheet] = new List<object> { "Id", "ProductName", "Category", "Quantity", "Unit", "Price", "ManufacturingDate", "ExpiryDate", "StockQuantity", "Description", "IsActive", "CreatedAt" },
                [GheeProductSheet] = new List<object> { "Id", "BatchNumber", "GheeType", "MilkUsedLiters", "GheeProducedKg", "YieldRate", "PricePerKg", "TotalValue", "StockKg", "ProductionDate", "ExpiryDate", "Quality", "Description", "CreatedAt" },
                [SettingsSheet] = new List<object> { "Key", "Value" },
                [PartnerSheet] = new List<object> { "Id", "Name", "Mobile", "Address", "Type", "AccessCode", "IsActive", "CreatedAt" },
                [TransactionSheet] = new List<object> { "Id", "PartnerId", "PartnerName", "Type", "Item", "Description", "Quantity", "Unit", "Rate", "TotalAmount", "PaymentStatus", "TransactionDate", "CreatedAt", "Remarks" },
                [SubscriptionSheet] = new List<object> { "Id", "PartnerId", "PartnerName", "Product", "DailyQuantity", "Unit", "RatePerUnit", "StartDate", "EndDate", "DeliverySlot", "DeliveryAddress", "Status", "Frequency", "CreatedAt", "Notes" },
                [OrderSheet] = new List<object> { "Id", "OrderNumber", "PartnerId", "PartnerName", "PartnerMobile", "ProductName", "Quantity", "Unit", "Rate", "TotalAmount", "OrderDate", "DeliveryDate", "DeliverySlot", "Status", "PaymentStatus", "DeliveryAddress", "CreatedAt", "Notes" },
                [NotificationSheet] = new List<object> { "Id", "Title", "Message", "Type", "Icon", "Link", "IsRead", "CreatedAt" },
                [ExpenseSheet] = new List<object> { "Id", "Category", "Description", "Amount", "ExpenseDate", "Mode", "Remarks", "CreatedAt" }
            };

            // Create missing sheets
            var requests = new List<Request>();
            foreach (var sheet in sheetsToCreate)
            {
                if (!existingSheets.Contains(sheet.Key))
                {
                    requests.Add(new Request
                    {
                        AddSheet = new AddSheetRequest
                        {
                            Properties = new SheetProperties { Title = sheet.Key }
                        }
                    });
                }
            }

            if (requests.Any())
            {
                await _service.Spreadsheets.BatchUpdate(new BatchUpdateSpreadsheetRequest { Requests = requests }, _spreadsheetId).ExecuteAsync();
            }

            // Add headers to each sheet if empty
            foreach (var sheet in sheetsToCreate)
            {
                var range = $"{sheet.Key}!A1:Z1";
                var response = await _service.Spreadsheets.Values.Get(_spreadsheetId, range).ExecuteAsync();
                if (response.Values == null || response.Values.Count == 0)
                {
                    var headerRow = new ValueRange { Values = new List<IList<object>> { sheet.Value } };
                    var updateRequest = _service.Spreadsheets.Values.Update(headerRow, _spreadsheetId, range);
                    updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                    await updateRequest.ExecuteAsync();
                }
            }
        }

        // ============ GENERIC HELPERS ============
        private async Task<IList<IList<object>>> GetAllRows(string sheetName)
        {
            var range = $"{sheetName}!A2:Z";
            var response = await _service.Spreadsheets.Values.Get(_spreadsheetId, range).ExecuteAsync();
            return response.Values ?? new List<IList<object>>();
        }

        private async Task<int> GetNextId(string sheetName)
        {
            var rows = await GetAllRows(sheetName);
            if (!rows.Any()) return 1;
            return rows.Max(r => int.TryParse(r[0]?.ToString(), out var id) ? id : 0) + 1;
        }

        private async Task AppendRow(string sheetName, IList<object> row)
        {
            var range = $"{sheetName}!A:Z";
            var valueRange = new ValueRange { Values = new List<IList<object>> { row } };
            var request = _service.Spreadsheets.Values.Append(valueRange, _spreadsheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
            await request.ExecuteAsync();
        }

        private async Task UpdateRow(string sheetName, int rowIndex, IList<object> row)
        {
            var range = $"{sheetName}!A{rowIndex + 2}:Z{rowIndex + 2}"; // +2 because 1-based + header
            var valueRange = new ValueRange { Values = new List<IList<object>> { row } };
            var request = _service.Spreadsheets.Values.Update(valueRange, _spreadsheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await request.ExecuteAsync();
        }

        private async Task DeleteRow(string sheetName, int rowIndex)
        {
            // Get sheet ID
            var spreadsheet = await _service.Spreadsheets.Get(_spreadsheetId).ExecuteAsync();
            var sheet = spreadsheet.Sheets.FirstOrDefault(s => s.Properties.Title == sheetName);
            if (sheet == null) return;

            var request = new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request>
                {
                    new Request
                    {
                        DeleteDimension = new DeleteDimensionRequest
                        {
                            Range = new DimensionRange
                            {
                                SheetId = sheet.Properties.SheetId,
                                Dimension = "ROWS",
                                StartIndex = rowIndex + 1, // +1 for header
                                EndIndex = rowIndex + 2
                            }
                        }
                    }
                }
            };
            await _service.Spreadsheets.BatchUpdate(request, _spreadsheetId).ExecuteAsync();
        }

        private string SafeGet(IList<object> row, int index)
        {
            if (index < row.Count && row[index] != null)
                return row[index].ToString() ?? "";
            return "";
        }

        // ============ MILK COLLECTION ============
        public async Task<List<MilkCollection>> GetAllMilkCollections()
        {
            var rows = await GetAllRows(MilkCollectionSheet);
            return rows.Select(r => new MilkCollection
            {
                Id = int.TryParse(SafeGet(r, 0), out var id) ? id : 0,
                FarmerName = SafeGet(r, 1),
                MobileNumber = SafeGet(r, 2),
                MilkType = Enum.TryParse<MilkType>(SafeGet(r, 3), out var mt) ? mt : MilkType.Cow,
                Quantity = decimal.TryParse(SafeGet(r, 4), out var qty) ? qty : 0,
                FatPercentage = decimal.TryParse(SafeGet(r, 5), out var fat) ? fat : 0,
                SNFPercentage = decimal.TryParse(SafeGet(r, 6), out var snf) ? snf : 0,
                RatePerLiter = decimal.TryParse(SafeGet(r, 7), out var rate) ? rate : 0,
                TotalAmount = decimal.TryParse(SafeGet(r, 8), out var total) ? total : 0,
                Shift = Enum.TryParse<Shift>(SafeGet(r, 9), out var shift) ? shift : Shift.Morning,
                CollectionDate = DateTime.TryParse(SafeGet(r, 10), out var cd) ? cd : DateTime.Today,
                CreatedAt = DateTime.TryParse(SafeGet(r, 11), out var ca) ? ca : DateTime.Now,
                Remarks = SafeGet(r, 12)
            }).ToList();
        }

        public async Task<MilkCollection?> GetMilkCollectionById(int id)
        {
            var all = await GetAllMilkCollections();
            return all.FirstOrDefault(m => m.Id == id);
        }

        public async Task AddMilkCollection(MilkCollection m)
        {
            m.Id = await GetNextId(MilkCollectionSheet);
            var row = new List<object>
            {
                m.Id, m.FarmerName, m.MobileNumber, m.MilkType.ToString(),
                m.Quantity, m.FatPercentage, m.SNFPercentage, m.RatePerLiter,
                m.TotalAmount, m.Shift.ToString(), m.CollectionDate.ToString("yyyy-MM-dd"),
                m.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), m.Remarks ?? ""
            };
            await AppendRow(MilkCollectionSheet, row);
        }

        public async Task UpdateMilkCollection(MilkCollection m)
        {
            var rows = await GetAllRows(MilkCollectionSheet);
            var index = rows.ToList().FindIndex(r => SafeGet(r, 0) == m.Id.ToString());
            if (index < 0) return;

            var row = new List<object>
            {
                m.Id, m.FarmerName, m.MobileNumber, m.MilkType.ToString(),
                m.Quantity, m.FatPercentage, m.SNFPercentage, m.RatePerLiter,
                m.TotalAmount, m.Shift.ToString(), m.CollectionDate.ToString("yyyy-MM-dd"),
                m.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), m.Remarks ?? ""
            };
            await UpdateRow(MilkCollectionSheet, index, row);
        }

        public async Task DeleteMilkCollection(int id)
        {
            var rows = await GetAllRows(MilkCollectionSheet);
            var index = rows.ToList().FindIndex(r => SafeGet(r, 0) == id.ToString());
            if (index >= 0) await DeleteRow(MilkCollectionSheet, index);
        }

        // ============ MILK RATES ============
        public async Task<List<MilkRate>> GetAllMilkRates()
        {
            var rows = await GetAllRows(MilkRateSheet);
            return rows.Select(r => new MilkRate
            {
                Id = int.TryParse(SafeGet(r, 0), out var id) ? id : 0,
                MilkType = Enum.TryParse<MilkType>(SafeGet(r, 1), out var mt) ? mt : MilkType.Cow,
                MinFat = decimal.TryParse(SafeGet(r, 2), out var mnf) ? mnf : 0,
                MaxFat = decimal.TryParse(SafeGet(r, 3), out var mxf) ? mxf : 0,
                MinSNF = decimal.TryParse(SafeGet(r, 4), out var mns) ? mns : 0,
                MaxSNF = decimal.TryParse(SafeGet(r, 5), out var mxs) ? mxs : 0,
                RatePerLiter = decimal.TryParse(SafeGet(r, 6), out var rate) ? rate : 0,
                EffectiveFrom = DateTime.TryParse(SafeGet(r, 7), out var ef) ? ef : DateTime.Today,
                IsActive = SafeGet(r, 8).ToLower() == "true"
            }).ToList();
        }

        public async Task<MilkRate?> GetMilkRateById(int id)
        {
            var all = await GetAllMilkRates();
            return all.FirstOrDefault(r => r.Id == id);
        }

        public async Task AddMilkRate(MilkRate r)
        {
            r.Id = await GetNextId(MilkRateSheet);
            var row = new List<object>
            {
                r.Id, r.MilkType.ToString(), r.MinFat, r.MaxFat, r.MinSNF, r.MaxSNF,
                r.RatePerLiter, r.EffectiveFrom.ToString("yyyy-MM-dd"), r.IsActive.ToString()
            };
            await AppendRow(MilkRateSheet, row);
        }

        public async Task UpdateMilkRate(MilkRate r)
        {
            var rows = await GetAllRows(MilkRateSheet);
            var index = rows.ToList().FindIndex(row => SafeGet(row, 0) == r.Id.ToString());
            if (index < 0) return;

            var row = new List<object>
            {
                r.Id, r.MilkType.ToString(), r.MinFat, r.MaxFat, r.MinSNF, r.MaxSNF,
                r.RatePerLiter, r.EffectiveFrom.ToString("yyyy-MM-dd"), r.IsActive.ToString()
            };
            await UpdateRow(MilkRateSheet, index, row);
        }

        public async Task DeleteMilkRate(int id)
        {
            var rows = await GetAllRows(MilkRateSheet);
            var index = rows.ToList().FindIndex(r => SafeGet(r, 0) == id.ToString());
            if (index >= 0) await DeleteRow(MilkRateSheet, index);
        }

        public async Task<MilkRate?> FindMatchingRate(MilkType milkType, decimal fat, decimal snf)
        {
            var rates = await GetAllMilkRates();
            return rates.FirstOrDefault(r =>
                r.IsActive && r.MilkType == milkType
                && fat >= r.MinFat && fat <= r.MaxFat
                && snf >= r.MinSNF && snf <= r.MaxSNF);
        }

        // ============ DAIRY PRODUCTS ============
        public async Task<List<DairyProduct>> GetAllDairyProducts()
        {
            var rows = await GetAllRows(DairyProductSheet);
            return rows.Select(r => new DairyProduct
            {
                Id = int.TryParse(SafeGet(r, 0), out var id) ? id : 0,
                ProductName = SafeGet(r, 1),
                Category = Enum.TryParse<ProductCategory>(SafeGet(r, 2), out var cat) ? cat : ProductCategory.Milk,
                Quantity = decimal.TryParse(SafeGet(r, 3), out var qty) ? qty : 0,
                Unit = Enum.TryParse<ProductUnit>(SafeGet(r, 4), out var unit) ? unit : ProductUnit.Liter,
                Price = decimal.TryParse(SafeGet(r, 5), out var price) ? price : 0,
                ManufacturingDate = DateTime.TryParse(SafeGet(r, 6), out var mfg) ? mfg : DateTime.Today,
                ExpiryDate = DateTime.TryParse(SafeGet(r, 7), out var exp) ? exp : DateTime.Today.AddDays(30),
                StockQuantity = decimal.TryParse(SafeGet(r, 8), out var stock) ? stock : 0,
                Description = SafeGet(r, 9),
                IsActive = SafeGet(r, 10).ToLower() != "false",
                CreatedAt = DateTime.TryParse(SafeGet(r, 11), out var ca) ? ca : DateTime.Now
            }).ToList();
        }

        public async Task<DairyProduct?> GetDairyProductById(int id)
        {
            var all = await GetAllDairyProducts();
            return all.FirstOrDefault(p => p.Id == id);
        }

        public async Task AddDairyProduct(DairyProduct p)
        {
            p.Id = await GetNextId(DairyProductSheet);
            var row = new List<object>
            {
                p.Id, p.ProductName, p.Category.ToString(), p.Quantity, p.Unit.ToString(),
                p.Price, p.ManufacturingDate.ToString("yyyy-MM-dd"), p.ExpiryDate.ToString("yyyy-MM-dd"),
                p.StockQuantity, p.Description ?? "", p.IsActive.ToString(),
                p.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            };
            await AppendRow(DairyProductSheet, row);
        }

        public async Task UpdateDairyProduct(DairyProduct p)
        {
            var rows = await GetAllRows(DairyProductSheet);
            var index = rows.ToList().FindIndex(r => SafeGet(r, 0) == p.Id.ToString());
            if (index < 0) return;

            var row = new List<object>
            {
                p.Id, p.ProductName, p.Category.ToString(), p.Quantity, p.Unit.ToString(),
                p.Price, p.ManufacturingDate.ToString("yyyy-MM-dd"), p.ExpiryDate.ToString("yyyy-MM-dd"),
                p.StockQuantity, p.Description ?? "", p.IsActive.ToString(),
                p.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            };
            await UpdateRow(DairyProductSheet, index, row);
        }

        public async Task DeleteDairyProduct(int id)
        {
            var rows = await GetAllRows(DairyProductSheet);
            var index = rows.ToList().FindIndex(r => SafeGet(r, 0) == id.ToString());
            if (index >= 0) await DeleteRow(DairyProductSheet, index);
        }

        // ============ GHEE PRODUCTS ============
        public async Task<List<GheeProduct>> GetAllGheeProducts()
        {
            var rows = await GetAllRows(GheeProductSheet);
            return rows.Select(r => new GheeProduct
            {
                Id = int.TryParse(SafeGet(r, 0), out var id) ? id : 0,
                BatchNumber = SafeGet(r, 1),
                GheeType = Enum.TryParse<GheeType>(SafeGet(r, 2), out var gt) ? gt : GheeType.CowGhee,
                MilkUsedLiters = decimal.TryParse(SafeGet(r, 3), out var mu) ? mu : 0,
                GheeProducedKg = decimal.TryParse(SafeGet(r, 4), out var gp) ? gp : 0,
                YieldRate = decimal.TryParse(SafeGet(r, 5), out var yr) ? yr : 0,
                PricePerKg = decimal.TryParse(SafeGet(r, 6), out var ppk) ? ppk : 0,
                TotalValue = decimal.TryParse(SafeGet(r, 7), out var tv) ? tv : 0,
                StockKg = decimal.TryParse(SafeGet(r, 8), out var sk) ? sk : 0,
                ProductionDate = DateTime.TryParse(SafeGet(r, 9), out var pd) ? pd : DateTime.Today,
                ExpiryDate = DateTime.TryParse(SafeGet(r, 10), out var ed) ? ed : DateTime.Today.AddMonths(12),
                Quality = Enum.TryParse<QualityGrade>(SafeGet(r, 11), out var q) ? q : QualityGrade.Standard,
                Description = SafeGet(r, 12),
                CreatedAt = DateTime.TryParse(SafeGet(r, 13), out var ca) ? ca : DateTime.Now
            }).ToList();
        }

        public async Task<GheeProduct?> GetGheeProductById(int id)
        {
            var all = await GetAllGheeProducts();
            return all.FirstOrDefault(g => g.Id == id);
        }

        public async Task AddGheeProduct(GheeProduct g)
        {
            g.Id = await GetNextId(GheeProductSheet);
            var row = new List<object>
            {
                g.Id, g.BatchNumber, g.GheeType.ToString(), g.MilkUsedLiters,
                g.GheeProducedKg, g.YieldRate, g.PricePerKg, g.TotalValue, g.StockKg,
                g.ProductionDate.ToString("yyyy-MM-dd"), g.ExpiryDate.ToString("yyyy-MM-dd"),
                g.Quality.ToString(), g.Description ?? "",
                g.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            };
            await AppendRow(GheeProductSheet, row);
        }

        public async Task UpdateGheeProduct(GheeProduct g)
        {
            var rows = await GetAllRows(GheeProductSheet);
            var index = rows.ToList().FindIndex(r => SafeGet(r, 0) == g.Id.ToString());
            if (index < 0) return;

            var row = new List<object>
            {
                g.Id, g.BatchNumber, g.GheeType.ToString(), g.MilkUsedLiters,
                g.GheeProducedKg, g.YieldRate, g.PricePerKg, g.TotalValue, g.StockKg,
                g.ProductionDate.ToString("yyyy-MM-dd"), g.ExpiryDate.ToString("yyyy-MM-dd"),
                g.Quality.ToString(), g.Description ?? "",
                g.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            };
            await UpdateRow(GheeProductSheet, index, row);
        }

        public async Task DeleteGheeProduct(int id)
        {
            var rows = await GetAllRows(GheeProductSheet);
            var index = rows.ToList().FindIndex(r => SafeGet(r, 0) == id.ToString());
            if (index >= 0) await DeleteRow(GheeProductSheet, index);
        }

        // ============ SETTINGS (Profile Photo etc.) ============
        public async Task<string?> GetSetting(string key)
        {
            var rows = await GetAllRows(SettingsSheet);
            var row = rows.FirstOrDefault(r => SafeGet(r, 0) == key);
            return row != null ? SafeGet(row, 1) : null;
        }

        public async Task SaveSetting(string key, string value)
        {
            var rows = await GetAllRows(SettingsSheet);
            var index = rows.ToList().FindIndex(r => SafeGet(r, 0) == key);

            if (index >= 0)
            {
                await UpdateRow(SettingsSheet, index, new List<object> { key, value });
            }
            else
            {
                await AppendRow(SettingsSheet, new List<object> { key, value });
            }
        }

        public async Task DeleteSetting(string key)
        {
            var rows = await GetAllRows(SettingsSheet);
            var index = rows.ToList().FindIndex(r => SafeGet(r, 0) == key);
            if (index >= 0) await DeleteRow(SettingsSheet, index);
        }

        // Profile Photo - stored as base64 in Settings sheet
        public async Task<string?> GetProfilePhoto()
        {
            return await GetSetting("ProfilePhoto");
        }

        public async Task SaveProfilePhoto(string base64Data)
        {
            await SaveSetting("ProfilePhoto", base64Data);
        }

        public async Task DeleteProfilePhoto()
        {
            await DeleteSetting("ProfilePhoto");
        }

        // ============ PARTNERS ============
        public async Task<List<Partner>> GetAllPartners()
        {
            var rows = await GetAllRows(PartnerSheet);
            return rows.Select(r => new Partner
            {
                Id = int.TryParse(SafeGet(r, 0), out var id) ? id : 0,
                Name = SafeGet(r, 1),
                Mobile = SafeGet(r, 2),
                Address = SafeGet(r, 3),
                Type = Enum.TryParse<PartnerType>(SafeGet(r, 4), out var t) ? t : PartnerType.Supplier,
                AccessCode = SafeGet(r, 5),
                IsActive = SafeGet(r, 6).ToLower() != "false",
                CreatedAt = DateTime.TryParse(SafeGet(r, 7), out var ca) ? ca : DateTime.Now
            }).ToList();
        }

        public async Task<Partner?> GetPartnerById(int id)
        {
            var all = await GetAllPartners();
            return all.FirstOrDefault(p => p.Id == id);
        }

        public async Task<Partner?> GetPartnerByAccessCode(string code)
        {
            var all = await GetAllPartners();
            return all.FirstOrDefault(p => p.AccessCode == code && p.IsActive);
        }

        public async Task AddPartner(Partner p)
        {
            p.Id = await GetNextId(PartnerSheet);
            // Generate unique access code
            if (string.IsNullOrEmpty(p.AccessCode))
            {
                p.AccessCode = "RD" + p.Id.ToString("D4");
            }
            var row = new List<object>
            {
                p.Id, p.Name, p.Mobile, p.Address ?? "", p.Type.ToString(),
                p.AccessCode, p.IsActive.ToString(), p.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            };
            await AppendRow(PartnerSheet, row);
        }

        public async Task UpdatePartner(Partner p)
        {
            var rows = await GetAllRows(PartnerSheet);
            var index = rows.ToList().FindIndex(r => SafeGet(r, 0) == p.Id.ToString());
            if (index < 0) return;
            var row = new List<object>
            {
                p.Id, p.Name, p.Mobile, p.Address ?? "", p.Type.ToString(),
                p.AccessCode, p.IsActive.ToString(), p.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            };
            await UpdateRow(PartnerSheet, index, row);
        }

        public async Task DeletePartner(int id)
        {
            var rows = await GetAllRows(PartnerSheet);
            var index = rows.ToList().FindIndex(r => SafeGet(r, 0) == id.ToString());
            if (index >= 0) await DeleteRow(PartnerSheet, index);
        }

        // ============ TRANSACTIONS ============
        public async Task<List<Transaction>> GetAllTransactions()
        {
            var rows = await GetAllRows(TransactionSheet);
            return rows.Select(r => new Transaction
            {
                Id = int.TryParse(SafeGet(r, 0), out var id) ? id : 0,
                PartnerId = int.TryParse(SafeGet(r, 1), out var pid) ? pid : 0,
                PartnerName = SafeGet(r, 2),
                Type = Enum.TryParse<TransactionType>(SafeGet(r, 3), out var tt) ? tt : TransactionType.Received,
                Item = Enum.TryParse<TransactionItem>(SafeGet(r, 4), out var ti) ? ti : TransactionItem.Milk,
                Description = SafeGet(r, 5),
                Quantity = decimal.TryParse(SafeGet(r, 6), out var qty) ? qty : 0,
                Unit = SafeGet(r, 7),
                Rate = decimal.TryParse(SafeGet(r, 8), out var rate) ? rate : 0,
                TotalAmount = decimal.TryParse(SafeGet(r, 9), out var amt) ? amt : 0,
                PaymentStatus = Enum.TryParse<PaymentStatus>(SafeGet(r, 10), out var ps) ? ps : PaymentStatus.Pending,
                TransactionDate = DateTime.TryParse(SafeGet(r, 11), out var td) ? td : DateTime.Today,
                CreatedAt = DateTime.TryParse(SafeGet(r, 12), out var ca) ? ca : DateTime.Now,
                Remarks = SafeGet(r, 13)
            }).ToList();
        }

        public async Task<List<Transaction>> GetTransactionsByPartnerId(int partnerId)
        {
            var all = await GetAllTransactions();
            return all.Where(t => t.PartnerId == partnerId).OrderByDescending(t => t.TransactionDate).ToList();
        }

        public async Task<Transaction?> GetTransactionById(int id)
        {
            var all = await GetAllTransactions();
            return all.FirstOrDefault(t => t.Id == id);
        }

        public async Task AddTransaction(Transaction t)
        {
            t.Id = await GetNextId(TransactionSheet);
            var row = new List<object>
            {
                t.Id, t.PartnerId, t.PartnerName, t.Type.ToString(), t.Item.ToString(),
                t.Description ?? "", t.Quantity, t.Unit, t.Rate, t.TotalAmount,
                t.PaymentStatus.ToString(), t.TransactionDate.ToString("yyyy-MM-dd"),
                t.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), t.Remarks ?? ""
            };
            await AppendRow(TransactionSheet, row);
        }

        public async Task UpdateTransaction(Transaction t)
        {
            var rows = await GetAllRows(TransactionSheet);
            var index = rows.ToList().FindIndex(r => SafeGet(r, 0) == t.Id.ToString());
            if (index < 0) return;
            var row = new List<object>
            {
                t.Id, t.PartnerId, t.PartnerName, t.Type.ToString(), t.Item.ToString(),
                t.Description ?? "", t.Quantity, t.Unit, t.Rate, t.TotalAmount,
                t.PaymentStatus.ToString(), t.TransactionDate.ToString("yyyy-MM-dd"),
                t.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), t.Remarks ?? ""
            };
            await UpdateRow(TransactionSheet, index, row);
        }

        public async Task DeleteTransaction(int id)
        {
            var rows = await GetAllRows(TransactionSheet);
            var index = rows.ToList().FindIndex(r => SafeGet(r, 0) == id.ToString());
            if (index >= 0) await DeleteRow(TransactionSheet, index);
        }

        // ============ SUBSCRIPTIONS ============
        public async Task<List<Subscription>> GetAllSubscriptions()
        {
            var rows = await GetAllRows(SubscriptionSheet);
            return rows.Select(r => new Subscription
            {
                Id = int.TryParse(SafeGet(r, 0), out var id) ? id : 0,
                PartnerId = int.TryParse(SafeGet(r, 1), out var pid) ? pid : 0,
                PartnerName = SafeGet(r, 2),
                Product = Enum.TryParse<SubscriptionProduct>(SafeGet(r, 3), out var sp) ? sp : SubscriptionProduct.CowMilk,
                DailyQuantity = decimal.TryParse(SafeGet(r, 4), out var dq) ? dq : 0,
                Unit = SafeGet(r, 5),
                RatePerUnit = decimal.TryParse(SafeGet(r, 6), out var rpu) ? rpu : 0,
                StartDate = DateTime.TryParse(SafeGet(r, 7), out var sd) ? sd : DateTime.Today,
                EndDate = DateTime.TryParse(SafeGet(r, 8), out var ed) ? (DateTime?)ed : null,
                DeliverySlot = Enum.TryParse<DeliverySlot>(SafeGet(r, 9), out var ds) ? ds : DeliverySlot.Morning,
                DeliveryAddress = SafeGet(r, 10),
                Status = Enum.TryParse<SubscriptionStatus>(SafeGet(r, 11), out var ss) ? ss : SubscriptionStatus.Active,
                Frequency = Enum.TryParse<DeliveryFrequency>(SafeGet(r, 12), out var df) ? df : DeliveryFrequency.Daily,
                CreatedAt = DateTime.TryParse(SafeGet(r, 13), out var ca) ? ca : DateTime.Now,
                Notes = SafeGet(r, 14)
            }).ToList();
        }

        public async Task<Subscription?> GetSubscriptionById(int id)
        {
            var all = await GetAllSubscriptions();
            return all.FirstOrDefault(s => s.Id == id);
        }

        public async Task AddSubscription(Subscription s)
        {
            s.Id = await GetNextId(SubscriptionSheet);
            var row = new List<object>
            {
                s.Id, s.PartnerId, s.PartnerName, s.Product.ToString(), s.DailyQuantity,
                s.Unit, s.RatePerUnit, s.StartDate.ToString("yyyy-MM-dd"),
                s.EndDate?.ToString("yyyy-MM-dd") ?? "", s.DeliverySlot.ToString(),
                s.DeliveryAddress ?? "", s.Status.ToString(), s.Frequency.ToString(),
                s.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), s.Notes ?? ""
            };
            await AppendRow(SubscriptionSheet, row);
        }

        public async Task UpdateSubscription(Subscription s)
        {
            var rows = await GetAllRows(SubscriptionSheet);
            var index = rows.ToList().FindIndex(r => SafeGet(r, 0) == s.Id.ToString());
            if (index < 0) return;
            var row = new List<object>
            {
                s.Id, s.PartnerId, s.PartnerName, s.Product.ToString(), s.DailyQuantity,
                s.Unit, s.RatePerUnit, s.StartDate.ToString("yyyy-MM-dd"),
                s.EndDate?.ToString("yyyy-MM-dd") ?? "", s.DeliverySlot.ToString(),
                s.DeliveryAddress ?? "", s.Status.ToString(), s.Frequency.ToString(),
                s.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), s.Notes ?? ""
            };
            await UpdateRow(SubscriptionSheet, index, row);
        }

        public async Task DeleteSubscription(int id)
        {
            var rows = await GetAllRows(SubscriptionSheet);
            var index = rows.ToList().FindIndex(r => SafeGet(r, 0) == id.ToString());
            if (index >= 0) await DeleteRow(SubscriptionSheet, index);
        }

        // ============ ORDERS ============
        public async Task<List<Order>> GetAllOrders()
        {
            var rows = await GetAllRows(OrderSheet);
            return rows.Select(r => new Order
            {
                Id = int.TryParse(SafeGet(r, 0), out var id) ? id : 0,
                OrderNumber = SafeGet(r, 1),
                PartnerId = int.TryParse(SafeGet(r, 2), out var pid) ? pid : 0,
                PartnerName = SafeGet(r, 3),
                PartnerMobile = SafeGet(r, 4),
                ProductName = SafeGet(r, 5),
                Quantity = decimal.TryParse(SafeGet(r, 6), out var qty) ? qty : 0,
                Unit = SafeGet(r, 7),
                Rate = decimal.TryParse(SafeGet(r, 8), out var rate) ? rate : 0,
                TotalAmount = decimal.TryParse(SafeGet(r, 9), out var amt) ? amt : 0,
                OrderDate = DateTime.TryParse(SafeGet(r, 10), out var od) ? od : DateTime.Today,
                DeliveryDate = DateTime.TryParse(SafeGet(r, 11), out var dd) ? dd : DateTime.Today,
                DeliverySlot = Enum.TryParse<DeliverySlot>(SafeGet(r, 12), out var ds) ? ds : DeliverySlot.Morning,
                Status = Enum.TryParse<OrderStatus>(SafeGet(r, 13), out var os) ? os : OrderStatus.Pending,
                PaymentStatus = Enum.TryParse<PaymentStatus>(SafeGet(r, 14), out var ps) ? ps : PaymentStatus.Pending,
                DeliveryAddress = SafeGet(r, 15),
                CreatedAt = DateTime.TryParse(SafeGet(r, 16), out var ca) ? ca : DateTime.Now,
                Notes = SafeGet(r, 17)
            }).ToList();
        }

        public async Task<Order?> GetOrderById(int id)
        {
            var all = await GetAllOrders();
            return all.FirstOrDefault(o => o.Id == id);
        }

        public async Task AddOrder(Order o)
        {
            o.Id = await GetNextId(OrderSheet);
            o.OrderNumber = "ORD" + DateTime.Now.ToString("yyyyMMdd") + o.Id.ToString("D4");
            var row = new List<object>
            {
                o.Id, o.OrderNumber, o.PartnerId, o.PartnerName, o.PartnerMobile,
                o.ProductName, o.Quantity, o.Unit, o.Rate, o.TotalAmount,
                o.OrderDate.ToString("yyyy-MM-dd"), o.DeliveryDate.ToString("yyyy-MM-dd"),
                o.DeliverySlot.ToString(), o.Status.ToString(), o.PaymentStatus.ToString(),
                o.DeliveryAddress ?? "", o.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), o.Notes ?? ""
            };
            await AppendRow(OrderSheet, row);
        }

        public async Task UpdateOrder(Order o)
        {
            var rows = await GetAllRows(OrderSheet);
            var index = rows.ToList().FindIndex(r => SafeGet(r, 0) == o.Id.ToString());
            if (index < 0) return;
            var row = new List<object>
            {
                o.Id, o.OrderNumber, o.PartnerId, o.PartnerName, o.PartnerMobile,
                o.ProductName, o.Quantity, o.Unit, o.Rate, o.TotalAmount,
                o.OrderDate.ToString("yyyy-MM-dd"), o.DeliveryDate.ToString("yyyy-MM-dd"),
                o.DeliverySlot.ToString(), o.Status.ToString(), o.PaymentStatus.ToString(),
                o.DeliveryAddress ?? "", o.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), o.Notes ?? ""
            };
            await UpdateRow(OrderSheet, index, row);
        }

        public async Task DeleteOrder(int id)
        {
            var rows = await GetAllRows(OrderSheet);
            var index = rows.ToList().FindIndex(r => SafeGet(r, 0) == id.ToString());
            if (index >= 0) await DeleteRow(OrderSheet, index);
        }

        // ============ NOTIFICATIONS ============
        public async Task<List<Notification>> GetAllNotifications()
        {
            var rows = await GetAllRows(NotificationSheet);
            return rows.Select(r => new Notification
            {
                Id = int.TryParse(SafeGet(r, 0), out var id) ? id : 0,
                Title = SafeGet(r, 1),
                Message = SafeGet(r, 2),
                Type = Enum.TryParse<NotificationType>(SafeGet(r, 3), out var nt) ? nt : NotificationType.Info,
                Icon = SafeGet(r, 4),
                Link = SafeGet(r, 5),
                IsRead = SafeGet(r, 6).ToLower() == "true",
                CreatedAt = DateTime.TryParse(SafeGet(r, 7), out var ca) ? ca : DateTime.Now
            }).OrderByDescending(n => n.CreatedAt).ToList();
        }

        public async Task AddNotification(Notification n)
        {
            n.Id = await GetNextId(NotificationSheet);
            var row = new List<object>
            {
                n.Id, n.Title, n.Message, n.Type.ToString(),
                n.Icon ?? "", n.Link ?? "", n.IsRead.ToString(),
                n.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            };
            await AppendRow(NotificationSheet, row);
        }

        public async Task MarkNotificationRead(int id)
        {
            var rows = await GetAllRows(NotificationSheet);
            var index = rows.ToList().FindIndex(r => SafeGet(r, 0) == id.ToString());
            if (index < 0) return;
            var row = rows[index].ToList();
            while (row.Count < 8) row.Add("");
            row[6] = "True";
            await UpdateRow(NotificationSheet, index, row.Cast<object>().ToList());
        }

        public async Task MarkAllNotificationsRead()
        {
            var rows = await GetAllRows(NotificationSheet);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i].ToList();
                while (row.Count < 8) row.Add("");
                if (row[6]?.ToString()?.ToLower() != "true")
                {
                    row[6] = "True";
                    await UpdateRow(NotificationSheet, i, row.Cast<object>().ToList());
                }
            }
        }

        public async Task DeleteNotification(int id)
        {
            var rows = await GetAllRows(NotificationSheet);
            var index = rows.ToList().FindIndex(r => SafeGet(r, 0) == id.ToString());
            if (index >= 0) await DeleteRow(NotificationSheet, index);
        }

        // ============ EXPENSES ============
        public async Task<List<Expense>> GetAllExpenses()
        {
            var rows = await GetAllRows(ExpenseSheet);
            return rows.Select(r => new Expense
            {
                Id = int.TryParse(SafeGet(r, 0), out var id) ? id : 0,
                Category = Enum.TryParse<ExpenseCategory>(SafeGet(r, 1), out var cat) ? cat : ExpenseCategory.Other,
                Description = SafeGet(r, 2),
                Amount = decimal.TryParse(SafeGet(r, 3), out var amt) ? amt : 0,
                ExpenseDate = DateTime.TryParse(SafeGet(r, 4), out var ed) ? ed : DateTime.Today,
                Mode = Enum.TryParse<PaymentMode>(SafeGet(r, 5), out var pm) ? pm : PaymentMode.Cash,
                Remarks = SafeGet(r, 6),
                CreatedAt = DateTime.TryParse(SafeGet(r, 7), out var ca) ? ca : DateTime.Now
            }).ToList();
        }

        public async Task<Expense?> GetExpenseById(int id)
        {
            var all = await GetAllExpenses();
            return all.FirstOrDefault(e => e.Id == id);
        }

        public async Task AddExpense(Expense e)
        {
            e.Id = await GetNextId(ExpenseSheet);
            var row = new List<object>
            {
                e.Id, e.Category.ToString(), e.Description, e.Amount,
                e.ExpenseDate.ToString("yyyy-MM-dd"), e.Mode.ToString(),
                e.Remarks ?? "", e.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            };
            await AppendRow(ExpenseSheet, row);
        }

        public async Task UpdateExpense(Expense e)
        {
            var rows = await GetAllRows(ExpenseSheet);
            var index = rows.ToList().FindIndex(r => SafeGet(r, 0) == e.Id.ToString());
            if (index < 0) return;
            var row = new List<object>
            {
                e.Id, e.Category.ToString(), e.Description, e.Amount,
                e.ExpenseDate.ToString("yyyy-MM-dd"), e.Mode.ToString(),
                e.Remarks ?? "", e.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            };
            await UpdateRow(ExpenseSheet, index, row);
        }

        public async Task DeleteExpense(int id)
        {
            var rows = await GetAllRows(ExpenseSheet);
            var index = rows.ToList().FindIndex(r => SafeGet(r, 0) == id.ToString());
            if (index >= 0) await DeleteRow(ExpenseSheet, index);
        }

        // Helper: Create notification when events happen
        public async Task CreateSystemNotification(string title, string message, NotificationType type, string? link = null)
        {
            var icon = type switch
            {
                NotificationType.Order => "bi-bag-check",
                NotificationType.Payment => "bi-cash-coin",
                NotificationType.Subscription => "bi-calendar-check",
                NotificationType.Stock => "bi-box-seam",
                NotificationType.Warning => "bi-exclamation-triangle",
                NotificationType.Success => "bi-check-circle",
                NotificationType.Danger => "bi-x-circle",
                _ => "bi-info-circle"
            };
            await AddNotification(new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                Icon = icon,
                Link = link
            });
        }
    }
}
