using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.ViewModels.Stock;
using ShoesEcommerce.Models.Stocks;

namespace ShoesEcommerce.Controllers.Admin
{
    [Authorize(Roles = "Admin,Staff")]
    public class AdminStockController : Controller
    {
        private readonly IStockService _stockService;
        private readonly ILogger<AdminStockController> _logger;

        public AdminStockController(IStockService stockService, ILogger<AdminStockController> logger)
        {
            _stockService = stockService;
            _logger = logger;
        }

        // ===== INVENTORY =====
        [HttpGet]
        public async Task<IActionResult> Inventory(string searchTerm = "", string statusFilter = "")
        {
            ViewData["Title"] = "Quản lý Tồn kho";
            try
            {
                IEnumerable<Stock> stocks;
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    stocks = await _stockService.SearchInventoryAsync(searchTerm);
                }
                else if (!string.IsNullOrEmpty(statusFilter))
                {
                    stocks = await _stockService.GetInventoryByStatusAsync(statusFilter);
                }
                else
                {
                    stocks = await _stockService.GetAllInventoryAsync();
                }
                var statsDict = await _stockService.GetInventoryStatsAsync();
                var totalValue = await _stockService.GetTotalStockValueAsync();
                var totalQuantity = await _stockService.GetTotalStockQuantityAsync();
                var viewModel = new InventoryListViewModel
                {
                    Items = stocks.Select(MapToInventoryItemViewModel),
                    SearchTerm = searchTerm,
                    StatusFilter = statusFilter,
                    TotalItems = stocks.Count(),
                    Stats = new InventoryStatsViewModel
                    {
                        TotalProducts = statsDict.GetValueOrDefault("total", 0),
                        InStockProducts = statsDict.GetValueOrDefault("in-stock", 0),
                        LowStockProducts = statsDict.GetValueOrDefault("low-stock", 0),
                        OutOfStockProducts = statsDict.GetValueOrDefault("out-of-stock", 0),
                        TotalValue = totalValue,
                        TotalQuantity = totalQuantity
                    }
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading inventory page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang tồn kho: " + ex.Message;
                return View(new InventoryListViewModel());
            }
        }

        // ===== STOCK AUDIT =====
        [HttpGet]
        public async Task<IActionResult> Check(string searchTerm = "", DateTime? startDate = null, DateTime? endDate = null)
        {
            ViewData["Title"] = "Kiểm kho";
            try
            {
                var stocks = string.IsNullOrEmpty(searchTerm)
                    ? await _stockService.GetStocksForAuditAsync()
                    : await _stockService.SearchInventoryAsync(searchTerm);
                var auditHistory = await _stockService.GetAuditHistoryAsync(startDate, endDate);
                var auditStats = CalculateAuditStats(auditHistory);
                var viewModel = new StockAuditListViewModel
                {
                    Items = stocks.Select(MapToStockAuditItemViewModel),
                    SearchTerm = searchTerm,
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalItems = stocks.Count(),
                    Stats = auditStats
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading stock audit page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang kiểm kho: " + ex.Message;
                return View(new StockAuditListViewModel());
            }
        }

        // ===== STOCK IMPORT =====
        [HttpGet]
        public async Task<IActionResult> Import(string searchTerm = "", string statusFilter = "", int supplierId = 0, DateTime? startDate = null, DateTime? endDate = null)
        {
            ViewData["Title"] = "Quản lý Nhập hàng";
            try
            {
                IEnumerable<StockEntry> stockEntries;
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    stockEntries = await _stockService.SearchStockEntriesAsync(searchTerm);
                }
                else if (supplierId > 0)
                {
                    stockEntries = await _stockService.GetStockEntriesBySupplierAsync(supplierId);
                }
                else if (startDate.HasValue && endDate.HasValue)
                {
                    stockEntries = await _stockService.GetStockEntriesByDateRangeAsync(startDate.Value, endDate.Value);
                }
                else if (statusFilter == "unprocessed")
                {
                    stockEntries = await _stockService.GetUnprocessedEntriesAsync();
                }
                else
                {
                    stockEntries = await _stockService.GetAllStockEntriesAsync();
                }
                var suppliers = await _stockService.GetSuppliersForDropdownAsync();
                var viewModel = new StockImportListViewModel
                {
                    Items = stockEntries.Select(MapToStockImportItemViewModel),
                    SearchTerm = searchTerm,
                    StatusFilter = statusFilter,
                    SupplierId = supplierId,
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalItems = stockEntries.Count(),
                    Suppliers = suppliers.Select(s => new SupplierSelectViewModel
                    {
                        Id = (int)s.GetType().GetProperty("Id").GetValue(s),
                        Name = s.GetType().GetProperty("Name").GetValue(s)?.ToString() ?? "",
                        ContactInfo = s.GetType().GetProperty("ContactInfo")?.GetValue(s)?.ToString() ?? ""
                    })
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading stock import page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang nhập hàng: " + ex.Message;
                return View(new StockImportListViewModel());
            }
        }

        // ===== AUDIT PERFORMANCE =====
        [HttpGet]
        public async Task<IActionResult> PerformAudit(int productVariantId)
        {
            try
            {
                var stock = await _stockService.GetCurrentStockAsync(productVariantId);
                if (stock == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin tồn kho";
                    return RedirectToAction(nameof(Check));
                }
                var viewModel = new PerformAuditViewModel
                {
                    ProductVariantId = productVariantId,
                    ProductName = stock.ProductVariant?.Product?.Name ?? "",
                    Color = stock.ProductVariant?.Color ?? "",
                    Size = stock.ProductVariant?.Size ?? "",
                    SystemQuantity = stock.AvailableQuantity
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading audit form for ProductVariant {ProductVariantId}", productVariantId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction(nameof(Check));
            }
        }

        // ===== CREATE IMPORT =====
        [HttpGet]
        public async Task<IActionResult> CreateImport()
        {
            ViewData["Title"] = "Tạo phiếu nhập hàng";
            try
            {
                var suppliers = await _stockService.GetSuppliersForDropdownAsync();
                var productVariants = await _stockService.GetProductVariantsForDropdownAsync();
                var viewModel = new CreateStockImportViewModel
                {
                    Suppliers = suppliers.Select(s => new SupplierSelectViewModel
                    {
                        Id = (int)s.GetType().GetProperty("Id").GetValue(s),
                        Name = s.GetType().GetProperty("Name").GetValue(s)?.ToString() ?? "",
                        ContactInfo = s.GetType().GetProperty("ContactInfo")?.GetValue(s)?.ToString() ?? ""
                    }),
                    ProductVariants = productVariants.Select(pv => new ProductVariantSelectViewModel
                    {
                        Id = (int)pv.GetType().GetProperty("Id").GetValue(pv),
                        DisplayName = pv.GetType().GetProperty("DisplayName")?.GetValue(pv)?.ToString() ?? "",
                        ProductName = pv.GetType().GetProperty("ProductName")?.GetValue(pv)?.ToString() ?? "",
                        Color = pv.GetType().GetProperty("Color")?.GetValue(pv)?.ToString() ?? "",
                        Size = pv.GetType().GetProperty("Size")?.GetValue(pv)?.ToString() ?? "",
                        Price = (decimal)(pv.GetType().GetProperty("Price")?.GetValue(pv) ?? 0),
                        CurrentStock = (int)(pv.GetType().GetProperty("CurrentStock")?.GetValue(pv) ?? 0),
                        ImageUrl = pv.GetType().GetProperty("ImageUrl")?.GetValue(pv)?.ToString() ?? ""
                    })
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create import page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction(nameof(Import));
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateImport(CreateStockImportViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload dropdowns
                var suppliers = await _stockService.GetSuppliersForDropdownAsync();
                var productVariants = await _stockService.GetProductVariantsForDropdownAsync();

                model.Suppliers = suppliers.Select(s => new SupplierSelectViewModel
                {
                    Id = (int)s.GetType().GetProperty("Id").GetValue(s),
                    Name = s.GetType().GetProperty("Name").GetValue(s)?.ToString() ?? ""
                });
                model.ProductVariants = productVariants.Select(pv => new ProductVariantSelectViewModel
                {
                    Id = (int)pv.GetType().GetProperty("Id").GetValue(pv),
                    DisplayName = pv.GetType().GetProperty("DisplayName")?.GetValue(pv)?.ToString() ?? ""
                });
                return View(model);
            }

            try
            {
                var stockEntry = await _stockService.CreateStockEntryAsync(
                    model.ProductVariantId,
                    model.SupplierId,
                    model.QuantityReceived,
                    model.UnitCost,
                    model.BatchNumber,
                    model.Notes,
                    model.ReceivedBy);

                TempData["SuccessMessage"] = "Tạo phiếu nhập hàng thành công";
                return RedirectToAction(nameof(Import));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating stock import");
                ModelState.AddModelError("", "Lỗi tạo phiếu nhập hàng: " + ex.Message);

                // Reload dropdowns
                var suppliers = await _stockService.GetSuppliersForDropdownAsync();
                var productVariants = await _stockService.GetProductVariantsForDropdownAsync();

                model.Suppliers = suppliers.Select(s => new SupplierSelectViewModel
                {
                    Id = (int)s.GetType().GetProperty("Id").GetValue(s),
                    Name = s.GetType().GetProperty("Name").GetValue(s)?.ToString() ?? ""
                });
                model.ProductVariants = productVariants.Select(pv => new ProductVariantSelectViewModel
                {
                    Id = (int)pv.GetType().GetProperty("Id").GetValue(pv),
                    DisplayName = pv.GetType().GetProperty("DisplayName")?.GetValue(pv)?.ToString() ?? ""
                });
                return View(model);
            }
        }

        // ===== PROCESS STOCK ENTRY =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessImport(int id)
        {
            var success = await _stockService.ProcessStockEntryAsync(id, User.Identity.Name ?? "Admin");
            if (success)
            {
                TempData["SuccessMessage"] = "Phiếu nhập đã được xử lý và tồn kho đã cập nhật.";
            }
            else
            {
                TempData["ErrorMessage"] = "Xử lý phiếu nhập thất bại hoặc phiếu đã được xử lý.";
            }
            return RedirectToAction("Import");
        }

        // ===== PERFORM STOCK AUDIT =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PerformAudit(int productVariantId, int actualQuantity, string notes)
        {
            var success = await _stockService.PerformStockAuditAsync(productVariantId, actualQuantity, User.Identity.Name ?? "Admin", notes);
            if (success)
            {
                TempData["SuccessMessage"] = "Kiểm kho thành công. Tồn kho đã được điều chỉnh.";
            }
            else
            {
                TempData["ErrorMessage"] = "Kiểm kho thất bại.";
            }
            return RedirectToAction("Check");
        }

        // ===== HELPER METHODS =====
        private InventoryItemViewModel MapToInventoryItemViewModel(Stock stock)
        {
            return new InventoryItemViewModel
            {
                Id = stock.Id,
                ProductVariantId = stock.ProductVariantId,
                ProductId = stock.ProductVariant?.Product?.Id.ToString() ?? "",
                ProductName = stock.ProductVariant?.Product?.Name ?? "",
                Color = stock.ProductVariant?.Color ?? "",
                Size = stock.ProductVariant?.Size ?? "",
                Price = stock.ProductVariant?.Price ?? 0,
                AvailableQuantity = stock.AvailableQuantity,
                ReservedQuantity = stock.ReservedQuantity,
                TotalQuantity = stock.TotalQuantity,
                Status = GetStockStatus(stock),
                LastUpdated = stock.LastUpdated,
                LastUpdatedBy = stock.LastUpdatedBy,
                ImageUrl = stock.ProductVariant?.ImageUrl ?? "",
                CategoryName = stock.ProductVariant?.Product?.Category?.Name ?? "",
                BrandName = stock.ProductVariant?.Product?.Brand?.Name ?? ""
            };
        }
        private StockAuditItemViewModel MapToStockAuditItemViewModel(Stock stock)
        {
            return new StockAuditItemViewModel
            {
                Id = stock.Id,
                ProductVariantId = stock.ProductVariantId,
                ProductId = stock.ProductVariant?.Product?.Id.ToString() ?? "",
                ProductName = stock.ProductVariant?.Product?.Name ?? "",
                Color = stock.ProductVariant?.Color ?? "",
                Size = stock.ProductVariant?.Size ?? "",
                SystemQuantity = stock.AvailableQuantity,
                ActualQuantity = 0,
                Difference = 0,
                Status = "Chưa kiểm tra",
                ImageUrl = stock.ProductVariant?.ImageUrl ?? ""
            };
        }
        private StockImportItemViewModel MapToStockImportItemViewModel(StockEntry stockEntry)
        {
            return new StockImportItemViewModel
            {
                Id = stockEntry.Id,
                ImportId = $"NH{stockEntry.Id:000}",
                EntryDate = stockEntry.EntryDate,
                SupplierName = stockEntry.Supplier?.Name ?? "",
                ProductName = stockEntry.ProductVariant?.Product?.Name ?? "",
                Color = stockEntry.ProductVariant?.Color ?? "",
                Size = stockEntry.ProductVariant?.Size ?? "",
                QuantityReceived = stockEntry.QuantityReceived,
                UnitCost = stockEntry.UnitCost,
                TotalCost = stockEntry.TotalCost,
                Status = stockEntry.IsProcessed ? "Đã xử lý" : "Chưa xử lý",
                ReceivedBy = stockEntry.ReceivedBy,
                BatchNumber = stockEntry.BatchNumber,
                Notes = stockEntry.Notes,
                IsProcessed = stockEntry.IsProcessed
            };
        }
        private string GetStockStatus(Stock? stock)
        {
            if (stock == null) return "Không xác định";
            if (stock.IsOutOfStock) return "Hết hàng";
            if (stock.IsLowStock) return "Sắp hết";
            return "Còn hàng";
        }
        private StockAuditStatsViewModel CalculateAuditStats(IEnumerable<StockTransaction> auditHistory)
        {
            var audits = auditHistory.Where(t => t.Type == StockTransactionType.Adjustment).ToList();
            var totalAudited = audits.Count;
            var correctCount = audits.Count(a => a.QuantityChange == 0);
            var overCount = audits.Count(a => a.QuantityChange > 0);
            var underCount = audits.Count(a => a.QuantityChange < 0);
            return new StockAuditStatsViewModel
            {
                TotalAudited = totalAudited,
                CorrectCount = correctCount,
                OverCount = overCount,
                UnderCount = underCount,
                AccuracyRate = totalAudited > 0 ? (decimal)correctCount / totalAudited * 100 : 0
            };
        }
    }
}