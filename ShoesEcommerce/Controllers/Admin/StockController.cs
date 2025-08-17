using Microsoft.AspNetCore.Mvc;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.ViewModels.Stock;
using ShoesEcommerce.Models.Stocks;

namespace ShoesEcommerce.Controllers.Admin
{
    public class StockController : Controller
    {
        private readonly IStockService _stockService;
        private readonly ILogger<StockController> _logger;

        public StockController(IStockService stockService, ILogger<StockController> logger)
        {
            _stockService = stockService;
            _logger = logger;
        }

        // ===== T?N KHO (INVENTORY) =====
        
        [HttpGet]
        public async Task<IActionResult> Inventory(string searchTerm = "", string statusFilter = "")
        {
            ViewData["Title"] = "Qu?n lý T?n kho";

            try
            {
                // Get inventory based on filters
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

                // Get stats
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
                TempData["ErrorMessage"] = "Có l?i x?y ra khi t?i trang t?n kho: " + ex.Message;
                return View(new InventoryListViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStock(int productVariantId, int newQuantity, string reason, string adjustedBy)
        {
            try
            {
                var success = await _stockService.AdjustStockAsync(productVariantId, newQuantity, reason, adjustedBy);
                
                if (success)
                {
                    var updatedStock = await _stockService.GetCurrentStockAsync(productVariantId);
                    return Json(new StockUpdateResponse
                    {
                        Success = true,
                        Message = "C?p nh?t t?n kho thành công",
                        NewAvailableQuantity = updatedStock?.AvailableQuantity ?? 0,
                        NewTotalQuantity = updatedStock?.TotalQuantity ?? 0,
                        NewStatus = GetStockStatus(updatedStock)
                    });
                }
                else
                {
                    return Json(new StockUpdateResponse
                    {
                        Success = false,
                        Message = "Không th? c?p nh?t t?n kho"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for ProductVariant {ProductVariantId}", productVariantId);
                return Json(new StockUpdateResponse
                {
                    Success = false,
                    Message = "L?i c?p nh?t t?n kho: " + ex.Message
                });
            }
        }

        // ===== KI?M KHO (STOCK AUDIT) =====

        [HttpGet]
        public async Task<IActionResult> Check(string searchTerm = "", DateTime? startDate = null, DateTime? endDate = null)
        {
            ViewData["Title"] = "Ki?m kho";

            try
            {
                var stocks = string.IsNullOrEmpty(searchTerm)
                    ? await _stockService.GetStocksForAuditAsync()
                    : await _stockService.SearchInventoryAsync(searchTerm);

                // Get audit history for stats
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
                TempData["ErrorMessage"] = "Có l?i x?y ra khi t?i trang ki?m kho: " + ex.Message;
                return View(new StockAuditListViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> PerformAudit(int productVariantId)
        {
            try
            {
                var stock = await _stockService.GetCurrentStockAsync(productVariantId);
                if (stock == null)
                {
                    TempData["ErrorMessage"] = "Không tìm th?y thông tin t?n kho";
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
                TempData["ErrorMessage"] = "Có l?i x?y ra: " + ex.Message;
                return RedirectToAction(nameof(Check));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PerformAudit(PerformAuditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var success = await _stockService.PerformStockAuditAsync(
                    model.ProductVariantId, 
                    model.ActualQuantity, 
                    model.AuditedBy, 
                    model.Notes);

                if (success)
                {
                    TempData["SuccessMessage"] = "Ki?m kho thành công";
                    return RedirectToAction(nameof(Check));
                }
                else
                {
                    ModelState.AddModelError("", "Không th? th?c hi?n ki?m kho");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing audit for ProductVariant {ProductVariantId}", model.ProductVariantId);
                ModelState.AddModelError("", "L?i ki?m kho: " + ex.Message);
                return View(model);
            }
        }

        // ===== NH?P HÀNG (STOCK IMPORT) =====

        [HttpGet]
        public async Task<IActionResult> Import(string searchTerm = "", string statusFilter = "", int supplierId = 0, DateTime? startDate = null, DateTime? endDate = null)
        {
            ViewData["Title"] = "Qu?n lý Nh?p hàng";

            try
            {
                IEnumerable<StockEntry> stockEntries;

                // Apply filters
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
                TempData["ErrorMessage"] = "Có l?i x?y ra khi t?i trang nh?p hàng: " + ex.Message;
                return View(new StockImportListViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateImport()
        {
            ViewData["Title"] = "T?o phi?u nh?p hàng";

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
                TempData["ErrorMessage"] = "Có l?i x?y ra: " + ex.Message;
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

                TempData["SuccessMessage"] = "T?o phi?u nh?p hàng thành công";
                return RedirectToAction(nameof(Import));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating stock import");
                ModelState.AddModelError("", "L?i t?o phi?u nh?p hàng: " + ex.Message);
                
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

        [HttpPost]
        public async Task<IActionResult> ProcessImport(int id, string processedBy)
        {
            try
            {
                var success = await _stockService.ProcessStockEntryAsync(id, processedBy);
                
                if (success)
                {
                    return Json(new AjaxResponse
                    {
                        Success = true,
                        Message = "X? lý phi?u nh?p hàng thành công"
                    });
                }
                else
                {
                    return Json(new AjaxResponse
                    {
                        Success = false,
                        Message = "Không th? x? lý phi?u nh?p hàng"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing stock import {Id}", id);
                return Json(new AjaxResponse
                {
                    Success = false,
                    Message = "L?i x? lý phi?u nh?p hàng: " + ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditImport(int id)
        {
            try
            {
                var stockEntry = await _stockService.GetStockEntryByIdAsync(id);
                if (stockEntry == null)
                {
                    TempData["ErrorMessage"] = "Không tìm th?y phi?u nh?p hàng";
                    return RedirectToAction(nameof(Import));
                }

                var viewModel = new EditStockImportViewModel
                {
                    Id = stockEntry.Id,
                    QuantityReceived = stockEntry.QuantityReceived,
                    UnitCost = stockEntry.UnitCost,
                    BatchNumber = stockEntry.BatchNumber,
                    Notes = stockEntry.Notes,
                    ProductName = stockEntry.ProductVariant?.Product?.Name ?? "",
                    Color = stockEntry.ProductVariant?.Color ?? "",
                    Size = stockEntry.ProductVariant?.Size ?? "",
                    SupplierName = stockEntry.Supplier?.Name ?? "",
                    EntryDate = stockEntry.EntryDate,
                    IsProcessed = stockEntry.IsProcessed
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit import page for {Id}", id);
                TempData["ErrorMessage"] = "Có l?i x?y ra: " + ex.Message;
                return RedirectToAction(nameof(Import));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditImport(EditStockImportViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var success = await _stockService.UpdateStockEntryAsync(
                    model.Id,
                    model.QuantityReceived,
                    model.UnitCost,
                    model.BatchNumber,
                    model.Notes);

                if (success)
                {
                    TempData["SuccessMessage"] = "C?p nh?t phi?u nh?p hàng thành công";
                    return RedirectToAction(nameof(Import));
                }
                else
                {
                    ModelState.AddModelError("", "Không th? c?p nh?t phi?u nh?p hàng");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock import {Id}", model.Id);
                ModelState.AddModelError("", "L?i c?p nh?t phi?u nh?p hàng: " + ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImport(int id)
        {
            try
            {
                var success = await _stockService.DeleteStockEntryAsync(id);
                
                if (success)
                {
                    return Json(new AjaxResponse
                    {
                        Success = true,
                        Message = "Xóa phi?u nh?p hàng thành công"
                    });
                }
                else
                {
                    return Json(new AjaxResponse
                    {
                        Success = false,
                        Message = "Không th? xóa phi?u nh?p hàng"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting stock import {Id}", id);
                return Json(new AjaxResponse
                {
                    Success = false,
                    Message = "L?i xóa phi?u nh?p hàng: " + ex.Message
                });
            }
        }

        // ===== DEBUG ENDPOINTS =====
        
        [HttpGet]
        public async Task<IActionResult> TestSuppliers()
        {
            try
            {
                var suppliers = await _stockService.GetSuppliersForDropdownAsync();
                return Json(new
                {
                    success = true,
                    count = suppliers.Count(),
                    suppliers = suppliers.Select(s => new
                    {
                        Id = s.GetType().GetProperty("Id")?.GetValue(s),
                        Name = s.GetType().GetProperty("Name")?.GetValue(s)?.ToString(),
                        ContactInfo = s.GetType().GetProperty("ContactInfo")?.GetValue(s)?.ToString()
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestProductVariants()
        {
            try
            {
                var productVariants = await _stockService.GetProductVariantsForDropdownAsync();
                return Json(new
                {
                    success = true,
                    count = productVariants.Count(),
                    productVariants = productVariants.Take(5).Select(pv => new
                    {
                        Id = pv.GetType().GetProperty("Id")?.GetValue(pv),
                        DisplayName = pv.GetType().GetProperty("DisplayName")?.GetValue(pv)?.ToString()
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
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
                ActualQuantity = 0, // Will be filled during audit
                Difference = 0,
                Status = "Ch?a ki?m tra",
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
                Status = stockEntry.IsProcessed ? "?ã x? lý" : "Ch?a x? lý",
                ReceivedBy = stockEntry.ReceivedBy,
                BatchNumber = stockEntry.BatchNumber,
                Notes = stockEntry.Notes,
                IsProcessed = stockEntry.IsProcessed
            };
        }

        private string GetStockStatus(Stock? stock)
        {
            if (stock == null) return "Không xác ??nh";
            if (stock.IsOutOfStock) return "H?t hàng";
            if (stock.IsLowStock) return "S?p h?t";
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