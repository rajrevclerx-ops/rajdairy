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
                [SettingsSheet] = new List<object> { "Key", "Value" }
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
    }
}
