using Amazon.SimpleEmail.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Common.GenericRepository;
using POS.Common.UnitOfWork;
using POS.Data;
using POS.Data.Dto;
using POS.Data.Entities;
using POS.Domain;
using POS.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace POS.Repository
{
    public class InventoryRepository
        : GenericRepository<Inventory, POSDbContext>, IInventoryRepository
    {
        private readonly IPropertyMappingService _propertyMappingService;
        private readonly IInventoryHistoryRepository _inventoryHistoryRepository;
        private readonly IUnitConversationRepository _unitConversationRepository;
        private readonly UserInfoToken _userInfoToken;
        private readonly IDailyStockRepository _dailyStockRepository;

        public InventoryRepository(IUnitOfWork<POSDbContext> uow,
            IPropertyMappingService propertyMappingService,
            IInventoryHistoryRepository inventoryHistoryRepository,
            IUnitConversationRepository unitConversationRepository,
            UserInfoToken userInfoToken,
            IDailyStockRepository dailyStockRepository)
          : base(uow)
        {
            _propertyMappingService = propertyMappingService;
            _inventoryHistoryRepository = inventoryHistoryRepository;
            _unitConversationRepository = unitConversationRepository;
            _userInfoToken = userInfoToken;
            _dailyStockRepository = dailyStockRepository;
        }

        public async Task AddInventory(InventoryDto inventory)
        {

            await _dailyStockRepository.AddDailyStock(inventory);

            var existingInventory = await All.Where(x => x.ProductId == inventory.ProductId
                 && x.LocationId == inventory.LocationId).FirstOrDefaultAsync();
            if (existingInventory == null)
            {
                _inventoryHistoryRepository.Add(new InventoryHistory
                {
                    ProductId = inventory.ProductId,
                    InventorySource = inventory.InventorySource,
                    Stock = inventory.InventorySource == InventorySourceEnum.SalesOrder || inventory.InventorySource ==  InventorySourceEnum.DamageStock ? (-1) * inventory.Stock : inventory.Stock,
                    PricePerUnit = inventory.PricePerUnit,
                    PreviousTotalStock = 0,
                    SalesOrderId = inventory.SalesOrderId,
                    PurchaseOrderId = inventory.PurchaseOrderId,
                    LocationId = inventory.LocationId,
                    DamagedStockId = inventory.DamagedStockId
                });

                var inventoryToAdd = new Inventory
                {
                    ProductId = inventory.ProductId,
                    LocationId = inventory.LocationId
                };

                if (inventory.InventorySource == InventorySourceEnum.PurchaseOrder
                    || inventory.InventorySource == InventorySourceEnum.Direct)
                {
                    inventoryToAdd.Stock = inventory.Stock;
                    inventoryToAdd.AveragePurchasePrice = inventory.PricePerUnit;
                }
                else
                {
                    inventoryToAdd.Stock = (-1) * inventory.Stock;
                    inventoryToAdd.AverageSalesPrice = inventory.PricePerUnit;
                }
                Add(inventoryToAdd);
            }
            else
            {
                if (inventory.InventorySource == InventorySourceEnum.DeletePurchaseOrder)
                {
                    var existingPurchaseInventoryHistory = await _inventoryHistoryRepository
                        .All
                        .FirstOrDefaultAsync(c => inventory.ProductId == c.ProductId
                                && inventory.LocationId == c.LocationId
                                && inventory.PurchaseOrderId.HasValue
                                && c.PurchaseOrderId == inventory.PurchaseOrderId);

                    if (existingPurchaseInventoryHistory != null)
                    {
                        var purchaseOrderTotalStock = _inventoryHistoryRepository
                            .All
                            .Where(c => c.ProductId == inventory.ProductId && c.LocationId == inventory.LocationId
                                && (c.InventorySource == InventorySourceEnum.PurchaseOrder
                                    || c.InventorySource == InventorySourceEnum.Direct
                                    || c.InventorySource == InventorySourceEnum.PurchaseOrderReturn
                                    || c.InventorySource == InventorySourceEnum.StockTransfer))
                            .Sum(c => c.Stock);

                        if (purchaseOrderTotalStock - inventory.Stock == 0)
                        {
                            existingInventory.AveragePurchasePrice = 0;
                        }
                        else
                        {
                            existingInventory.AveragePurchasePrice =
                                ((existingInventory.AveragePurchasePrice * purchaseOrderTotalStock) - (inventory.PricePerUnit * inventory.Stock)) / (purchaseOrderTotalStock - inventory.Stock);
                        }

                        existingInventory.Stock -= inventory.Stock;
                        _inventoryHistoryRepository.Remove(existingPurchaseInventoryHistory);
                    }
                }
                else if (inventory.InventorySource == InventorySourceEnum.DeleteSalesOrder)
                {
                    var existingPurchaseInventoryHistory = await _inventoryHistoryRepository.All
                        .FirstOrDefaultAsync(c => inventory.ProductId == c.ProductId
                            && c.LocationId == inventory.LocationId
                            && inventory.SalesOrderId.HasValue
                            && c.SalesOrderId == inventory.SalesOrderId);

                    if (existingPurchaseInventoryHistory != null)
                    {
                        var salesOrderTotalStock = _inventoryHistoryRepository.All
                            .Where(c => c.ProductId == inventory.ProductId
                                        && c.LocationId == inventory.LocationId
                                        && (c.InventorySource == InventorySourceEnum.SalesOrder
                                                || c.InventorySource == InventorySourceEnum.SalesOrderReturn
                                                || (c.InventorySource == InventorySourceEnum.Direct && c.Stock < 0)
                                                || (c.InventorySource == InventorySourceEnum.StockTransfer && c.Stock < 0))
                            ).Sum(c => c.Stock);

                        if (salesOrderTotalStock + inventory.Stock == 0)
                        {
                            existingInventory.AverageSalesPrice = 0;
                        }
                        else
                        {
                            existingInventory.AverageSalesPrice =
                                ((-1) * (existingInventory.AverageSalesPrice * salesOrderTotalStock) - (inventory.PricePerUnit * inventory.Stock)) / ((-1) * salesOrderTotalStock - inventory.Stock);
                        }

                        existingInventory.Stock += inventory.Stock;

                        _inventoryHistoryRepository.Remove(existingPurchaseInventoryHistory);
                    }
                }
                else if (inventory.InventorySource == InventorySourceEnum.PurchaseOrder)
                {
                    var existingPurchaseInventoryHistory = await _inventoryHistoryRepository
                        .All
                        .FirstOrDefaultAsync(c => inventory.ProductId == c.ProductId
                            && inventory.PurchaseOrderId.HasValue
                            && c.PurchaseOrderId == inventory.PurchaseOrderId
                            && c.LocationId == inventory.LocationId);

                    var purchaseOrderTotalStock = _inventoryHistoryRepository
                            .All
                            .Where(c => c.ProductId == inventory.ProductId
                            && c.LocationId == inventory.LocationId
                            && (c.InventorySource == InventorySourceEnum.PurchaseOrder
                                    || (c.InventorySource == InventorySourceEnum.Direct && c.Stock > 0)
                                    || c.InventorySource == InventorySourceEnum.PurchaseOrderReturn
                                    || (c.InventorySource == InventorySourceEnum.StockTransfer && c.Stock > 0)))
                            .Sum(c => c.Stock);

                    if (existingPurchaseInventoryHistory != null)
                    {
                        if (existingPurchaseInventoryHistory.PricePerUnit != inventory.PricePerUnit)
                        {
                            var stock = purchaseOrderTotalStock - existingPurchaseInventoryHistory.Stock + inventory.Stock;

                            existingInventory.AveragePurchasePrice =
                                Math.Abs((existingInventory.AveragePurchasePrice * purchaseOrderTotalStock - existingPurchaseInventoryHistory.PricePerUnit * existingPurchaseInventoryHistory.Stock + inventory.PricePerUnit * inventory.Stock)
                                / (stock == 0 ? Math.Abs(purchaseOrderTotalStock) : stock));

                            existingPurchaseInventoryHistory.PricePerUnit = inventory.PricePerUnit;
                        }

                        if (existingPurchaseInventoryHistory.Stock != inventory.Stock)
                        {
                            existingInventory.Stock = existingInventory.Stock - existingPurchaseInventoryHistory.Stock + inventory.Stock;
                            existingPurchaseInventoryHistory.Stock = inventory.Stock;
                        }
                        _inventoryHistoryRepository.Update(existingPurchaseInventoryHistory);
                    }
                    else
                    {
                        _inventoryHistoryRepository.Add(new InventoryHistory
                        {
                            ProductId = inventory.ProductId,
                            InventorySource = inventory.InventorySource,
                            Stock = inventory.Stock,
                            PricePerUnit = inventory.PricePerUnit,
                            PreviousTotalStock = existingInventory.Stock,
                            SalesOrderId = inventory.SalesOrderId,
                            PurchaseOrderId = inventory.PurchaseOrderId,
                            LocationId = inventory.LocationId,
                            DamagedStockId = inventory.DamagedStockId
                        });
                        existingInventory.AveragePurchasePrice =
                            (existingInventory.AveragePurchasePrice * purchaseOrderTotalStock + inventory.PricePerUnit * inventory.Stock) / (purchaseOrderTotalStock + inventory.Stock);
                        existingInventory.Stock += inventory.Stock;
                    }
                }
                else if (inventory.InventorySource == InventorySourceEnum.Direct)
                {
                    if (inventory.Stock > 0)
                    {
                        var purchaseOrderTotalStock = _inventoryHistoryRepository.All.Where(ih =>
                            ih.ProductId == inventory.ProductId &&
                            ih.LocationId == inventory.LocationId &&
                            (
                                ih.InventorySource == InventorySourceEnum.PurchaseOrder
                                || (ih.InventorySource == InventorySourceEnum.Direct && ih.Stock > 0)
                                || ih.InventorySource == InventorySourceEnum.PurchaseOrderReturn
                                || (ih.InventorySource == InventorySourceEnum.StockTransfer && ih.Stock > 0)
                            )
                        ).Sum(ih => ih.Stock);

                        if (purchaseOrderTotalStock + inventory.Stock == 0)
                        {
                            existingInventory.AveragePurchasePrice = 0;
                        }
                        else
                        {
                            existingInventory.AveragePurchasePrice =
                                (existingInventory.AveragePurchasePrice * purchaseOrderTotalStock + inventory.PricePerUnit * inventory.Stock) /
                                (purchaseOrderTotalStock + inventory.Stock);
                        }
                    }
                    else
                    {
                        var salesOrderTotalStock = _inventoryHistoryRepository.All.Where(ih =>
                            ih.ProductId == inventory.ProductId
                            && ih.LocationId == inventory.LocationId
                            && (
                                ih.InventorySource == InventorySourceEnum.SalesOrder
                                || ih.InventorySource == InventorySourceEnum.SalesOrderReturn
                                || (ih.InventorySource == InventorySourceEnum.Direct && ih.Stock < 0)
                                || (ih.InventorySource == InventorySourceEnum.StockTransfer && ih.Stock < 0)
                            )
                        ).Sum(ih => ih.Stock);

                        if (salesOrderTotalStock + inventory.Stock == 0)
                        {
                            existingInventory.AverageSalesPrice = 0;
                        }
                        else
                        {
                            existingInventory.AverageSalesPrice = Math.Abs(
                                (existingInventory.AverageSalesPrice * salesOrderTotalStock + inventory.PricePerUnit * inventory.Stock) /
                                (salesOrderTotalStock + inventory.Stock)
                            );
                        }
                    }

                    _inventoryHistoryRepository.Add(new InventoryHistory
                    {
                        ProductId = inventory.ProductId,
                        LocationId = inventory.LocationId,
                        InventorySource = inventory.InventorySource,
                        Stock = inventory.Stock,
                        PricePerUnit = inventory.PricePerUnit,
                        PreviousTotalStock = existingInventory.Stock
                    });

                    existingInventory.Stock += inventory.Stock;
                }
                else if (inventory.InventorySource == InventorySourceEnum.PurchaseOrderReturn)
                {
                    existingInventory.Stock = existingInventory.Stock - inventory.Stock;
                    _inventoryHistoryRepository.Add(new InventoryHistory
                    {
                        ProductId = inventory.ProductId,
                        InventorySource = inventory.InventorySource,
                        Stock = (-1) * inventory.Stock,
                        PricePerUnit = inventory.PricePerUnit,
                        PreviousTotalStock = existingInventory.Stock,
                        SalesOrderId = inventory.SalesOrderId,
                        PurchaseOrderId = inventory.PurchaseOrderId,
                        LocationId = inventory.LocationId
                    });
                }
                else if (inventory.InventorySource == InventorySourceEnum.SalesOrderReturn)
                {
                    existingInventory.Stock = existingInventory.Stock + inventory.Stock;
                    _inventoryHistoryRepository.Add(new InventoryHistory
                    {
                        ProductId = inventory.ProductId,
                        InventorySource = inventory.InventorySource,
                        Stock = inventory.Stock,
                        PricePerUnit = inventory.PricePerUnit,
                        PreviousTotalStock = existingInventory.Stock,
                        SalesOrderId = inventory.SalesOrderId,
                        PurchaseOrderId = inventory.PurchaseOrderId,
                        LocationId = inventory.LocationId
                    });
                }
                else if (inventory.InventorySource == InventorySourceEnum.DamageStock)
                {
                    existingInventory.Stock = existingInventory.Stock - inventory.Stock;
                    _inventoryHistoryRepository.Add(new InventoryHistory
                    {
                        ProductId = inventory.ProductId,
                        InventorySource = inventory.InventorySource,
                        Stock = (-1) * inventory.Stock,
                        PricePerUnit = existingInventory.AveragePurchasePrice,
                        PreviousTotalStock = existingInventory.Stock,
                        SalesOrderId = inventory.SalesOrderId,
                        PurchaseOrderId = inventory.PurchaseOrderId,
                        LocationId = inventory.LocationId,
                        DamagedStockId = inventory.DamagedStockId
                    });
                }
                else
                {
                    var existingSalesInventoryHistory = await _inventoryHistoryRepository
                                            .All
                                            .FirstOrDefaultAsync(c => inventory.ProductId == c.ProductId
                                                    && c.LocationId == inventory.LocationId
                                                    && inventory.SalesOrderId.HasValue
                                                    && c.SalesOrderId == inventory.SalesOrderId);

                    var salesOrderTotalStock = _inventoryHistoryRepository
                        .All
                        .Where(c => c.ProductId == inventory.ProductId
                            && c.LocationId == inventory.LocationId
                            && (c.InventorySource == InventorySourceEnum.SalesOrder
                                || c.InventorySource == InventorySourceEnum.SalesOrderReturn
                                || (c.InventorySource == InventorySourceEnum.Direct && c.Stock < 0)
                                || (c.InventorySource == InventorySourceEnum.StockTransfer && c.Stock < 0)))
                        .Sum(c => c.Stock);

                    if (existingSalesInventoryHistory != null)
                    {
                        var stock = salesOrderTotalStock - existingSalesInventoryHistory.Stock + inventory.Stock;
                        if (existingSalesInventoryHistory.PricePerUnit != inventory.PricePerUnit)
                        {
                            existingInventory.AverageSalesPrice =
                            Math.Abs((existingInventory.AverageSalesPrice * salesOrderTotalStock - ((-1) * existingSalesInventoryHistory.Stock) * existingSalesInventoryHistory.PricePerUnit + inventory.PricePerUnit * inventory.Stock)
                            / (stock == 0 ? Math.Abs(salesOrderTotalStock) : stock));
                            existingSalesInventoryHistory.PricePerUnit = inventory.PricePerUnit;
                        }

                        if (existingSalesInventoryHistory.Stock != inventory.Stock)
                        {
                            existingInventory.Stock = existingInventory.Stock + ((-1) * existingSalesInventoryHistory.Stock) - inventory.Stock;
                            existingSalesInventoryHistory.Stock = (-1) * inventory.Stock;
                        }
                        _inventoryHistoryRepository.Update(existingSalesInventoryHistory);
                    }
                    else
                    {
                        _inventoryHistoryRepository.Add(new InventoryHistory
                        {
                            ProductId = inventory.ProductId,
                            InventorySource = inventory.InventorySource,
                            Stock = (-1) * inventory.Stock,
                            PricePerUnit = inventory.PricePerUnit,
                            PreviousTotalStock = existingInventory.Stock,
                            SalesOrderId = inventory.SalesOrderId,
                            PurchaseOrderId = inventory.PurchaseOrderId,
                            LocationId = inventory.LocationId
                        });
                        salesOrderTotalStock = Math.Abs(salesOrderTotalStock);
                        existingInventory.AverageSalesPrice =
                             Math.Abs((existingInventory.AverageSalesPrice * salesOrderTotalStock + inventory.PricePerUnit * inventory.Stock) / (salesOrderTotalStock + inventory.Stock));
                        existingInventory.Stock -= inventory.Stock;
                    }

                }
                Update(existingInventory);
            }
        }

        public async Task CreateStockTransfer(InventoryDto inventory)
        {

            var toLocationInventory = await All.Where(i => i.ProductId == inventory.ProductId
                    && i.LocationId == inventory.ToLocationId).FirstOrDefaultAsync();

            if (toLocationInventory == null)
            {
                var history = new InventoryHistory
                {
                    ProductId = inventory.ProductId,
                    LocationId = inventory.ToLocationId.Value,
                    InventorySource = inventory.InventorySource,
                    Stock = inventory.Stock,
                    PricePerUnit = inventory.PricePerUnit,
                    PreviousTotalStock = 0,
                    StockTransferId = inventory.StockTransferId
                };
                _inventoryHistoryRepository.Add(history);

                var newInventory = new Inventory
                {
                    ProductId = inventory.ProductId,
                    LocationId = inventory.ToLocationId.Value,
                    Stock = inventory.Stock,
                    AveragePurchasePrice = inventory.PricePerUnit
                };
                Add(newInventory);
            }
            else
            {
                var existingHistory = await _inventoryHistoryRepository.All.Where(h =>
                    h.LocationId == inventory.ToLocationId &&
                    h.ProductId == inventory.ProductId &&
                    h.StockTransferId != null &&
                    h.StockTransferId == inventory.StockTransferId).FirstOrDefaultAsync();

                var purchaseOrderTotalStock = await _inventoryHistoryRepository
                    .All
                    .Where(h => h.ProductId == inventory.ProductId && h.LocationId == inventory.ToLocationId &&
                        (h.InventorySource == InventorySourceEnum.PurchaseOrder ||
                         h.InventorySource == InventorySourceEnum.Direct ||
                         h.InventorySource == InventorySourceEnum.PurchaseOrderReturn ||
                         (h.InventorySource == InventorySourceEnum.StockTransfer && h.Stock > 0)))
                    .SumAsync(h => h.Stock);

                if (existingHistory != null)
                {
                    if (existingHistory.PricePerUnit != inventory.PricePerUnit)
                    {
                        var stock = purchaseOrderTotalStock - existingHistory.Stock + inventory.Stock;
                        toLocationInventory.AveragePurchasePrice = Math.Abs((toLocationInventory.AveragePurchasePrice * purchaseOrderTotalStock
                            - existingHistory.PricePerUnit * existingHistory.Stock
                            + inventory.PricePerUnit * inventory.Stock) / (stock == 0 ? Math.Abs(purchaseOrderTotalStock) : stock));
                        existingHistory.PricePerUnit = inventory.PricePerUnit;
                    }

                    if (existingHistory.Stock != inventory.Stock)
                    {
                        toLocationInventory.Stock = toLocationInventory.Stock - existingHistory.Stock + inventory.Stock;
                        existingHistory.Stock = inventory.Stock;
                    }
                    _inventoryHistoryRepository.Update(existingHistory);
                }
                else
                {
                    _inventoryHistoryRepository.Add(new InventoryHistory
                    {
                        ProductId = inventory.ProductId,
                        LocationId = inventory.ToLocationId.Value,
                        InventorySource = inventory.InventorySource,
                        Stock = inventory.Stock,
                        PricePerUnit = inventory.PricePerUnit,
                        PreviousTotalStock = toLocationInventory.Stock,
                        StockTransferId = inventory.StockTransferId
                    });

                    var stock = (purchaseOrderTotalStock + inventory.Stock) == 0 ? Math.Abs(purchaseOrderTotalStock) : (purchaseOrderTotalStock + inventory.Stock);
                    toLocationInventory.AveragePurchasePrice = (toLocationInventory.AveragePurchasePrice * purchaseOrderTotalStock
                        + inventory.PricePerUnit * inventory.Stock) / stock;
                    toLocationInventory.Stock += inventory.Stock;
                }
                Update(toLocationInventory);
            }

            var fromLocationInventory = await All
                .Where(i => i.ProductId == inventory.ProductId
                        && i.LocationId == inventory.FromLocationId)
                .FirstOrDefaultAsync();

            if (fromLocationInventory == null)
            {
                _inventoryHistoryRepository.Add(new InventoryHistory
                {
                    ProductId = inventory.ProductId,
                    LocationId = inventory.FromLocationId.Value,
                    InventorySource = inventory.InventorySource,
                    Stock = -inventory.Stock,
                    PricePerUnit = inventory.PricePerUnit,
                    PreviousTotalStock = 0,
                    StockTransferId = inventory.StockTransferId
                });

                var newInventory = new Inventory
                {
                    ProductId = inventory.ProductId,
                    LocationId = inventory.FromLocationId.Value,
                    Stock = -inventory.Stock,
                    AveragePurchasePrice = inventory.PricePerUnit
                };
                Add(newInventory);
            }
            else
            {
                var existingSalesHistory = await _inventoryHistoryRepository.All
                    .Where(h => h.LocationId == inventory.FromLocationId
                        && h.ProductId == inventory.ProductId
                        && h.StockTransferId != null
                        && h.StockTransferId == inventory.StockTransferId)
                    .FirstOrDefaultAsync();

                var salesOrderTotalStock = await _inventoryHistoryRepository
                    .All
                    .Where(h => h.ProductId == inventory.ProductId
                        && h.LocationId == inventory.FromLocationId
                        && (h.InventorySource == InventorySourceEnum.SalesOrder
                            || h.InventorySource == InventorySourceEnum.SalesOrderReturn
                            || (h.InventorySource == InventorySourceEnum.StockTransfer && h.Stock < 0)))
                    .SumAsync(h => h.Stock);

                if (existingSalesHistory != null)
                {
                    var stock = salesOrderTotalStock - existingSalesHistory.Stock + inventory.Stock;

                    if (existingSalesHistory.PricePerUnit != inventory.PricePerUnit)
                    {
                        fromLocationInventory.AverageSalesPrice = Math.Abs((fromLocationInventory.AverageSalesPrice * salesOrderTotalStock
                            - (-existingSalesHistory.Stock) * existingSalesHistory.PricePerUnit
                            + inventory.PricePerUnit * inventory.Stock) / (stock == 0 ? Math.Abs(salesOrderTotalStock) : stock));
                        existingSalesHistory.PricePerUnit = inventory.PricePerUnit;
                    }

                    if (existingSalesHistory.Stock != inventory.Stock)
                    {
                        fromLocationInventory.Stock = fromLocationInventory.Stock + (-existingSalesHistory.Stock) - inventory.Stock;
                        existingSalesHistory.Stock = -inventory.Stock;
                    }
                    _inventoryHistoryRepository.Update(existingSalesHistory);
                }
                else
                {
                    _inventoryHistoryRepository.Add(new InventoryHistory
                    {
                        ProductId = inventory.ProductId,
                        LocationId = inventory.FromLocationId.Value,
                        InventorySource = inventory.InventorySource,
                        Stock = -inventory.Stock,
                        PricePerUnit = inventory.PricePerUnit,
                        PreviousTotalStock = fromLocationInventory.Stock,
                        StockTransferId = inventory.StockTransferId
                    });

                    salesOrderTotalStock = Math.Abs(salesOrderTotalStock);
                    fromLocationInventory.AverageSalesPrice = salesOrderTotalStock + inventory.Stock == 0
                        ? 0
                        : Math.Abs((fromLocationInventory.AverageSalesPrice * salesOrderTotalStock
                            + inventory.PricePerUnit * inventory.Stock) / (salesOrderTotalStock + inventory.Stock));
                    fromLocationInventory.Stock -= inventory.Stock;
                }
                Update(fromLocationInventory);
            }
        }

        public async Task DeleteStockTransfer(InventoryDto inventory)
        {
            var toLocationInventory = await All.Where(i => i.ProductId == inventory.ProductId
            && i.LocationId == inventory.ToLocationId)
                .FirstOrDefaultAsync();

            if (toLocationInventory != null)
            {
                var existingPurchaseInventoryHistory = await _inventoryHistoryRepository.All.Where(h =>
                    h.LocationId == inventory.ToLocationId
                    && h.ProductId == inventory.ProductId
                    && h.StockTransferId != null
                    && h.StockTransferId == inventory.StockTransferId)
                .FirstOrDefaultAsync();

                if (existingPurchaseInventoryHistory != null)
                {
                    var purchaseOrderTotalStock = _inventoryHistoryRepository
                        .All
                        .Where(h => h.ProductId == inventory.ProductId
                        && h.LocationId == inventory.ToLocationId
                        && (h.InventorySource == InventorySourceEnum.PurchaseOrder
                            || h.InventorySource == InventorySourceEnum.Direct
                            || h.InventorySource == InventorySourceEnum.PurchaseOrderReturn
                            || (h.InventorySource == InventorySourceEnum.StockTransfer && h.Stock > 0)))
                        .Sum(h => h.Stock);

                    if (purchaseOrderTotalStock - inventory.Stock == 0)
                    {
                        toLocationInventory.AveragePurchasePrice = 0;
                    }
                    else
                    {
                        toLocationInventory.AveragePurchasePrice =
                            ((toLocationInventory.AveragePurchasePrice * purchaseOrderTotalStock)
                             - (inventory.PricePerUnit * inventory.Stock)) / (purchaseOrderTotalStock - inventory.Stock);
                    }

                    toLocationInventory.Stock -= inventory.Stock;
                    _inventoryHistoryRepository.Delete(existingPurchaseInventoryHistory);
                    Update(toLocationInventory);
                }
            }

            var fromLocationInventory = All.Where(i => i.ProductId == inventory.ProductId
                && i.LocationId == inventory.FromLocationId).FirstOrDefault();

            if (fromLocationInventory != null)
            {
                var existingSalesOrderInventoryHistory = _inventoryHistoryRepository
                    .All
                    .Where(h =>
                        h.LocationId == inventory.FromLocationId &&
                        h.ProductId == inventory.ProductId &&
                        h.StockTransferId != null &&
                        h.StockTransferId == inventory.StockTransferId)
                    .FirstOrDefault();

                if (existingSalesOrderInventoryHistory != null)
                {
                    var salesOrderTotalStock = _inventoryHistoryRepository
                        .All
                        .Where(h => h.ProductId == inventory.ProductId
                        && h.LocationId == inventory.FromLocationId
                        && (h.InventorySource == InventorySourceEnum.SalesOrder
                            || h.InventorySource == InventorySourceEnum.SalesOrderReturn
                            || (h.InventorySource == InventorySourceEnum.StockTransfer && h.Stock < 0)))
                        .Sum(h => h.Stock);

                    if (salesOrderTotalStock + inventory.Stock == 0)
                    {
                        fromLocationInventory.AverageSalesPrice = 0;
                    }
                    else
                    {
                        fromLocationInventory.AverageSalesPrice =
                            (-1 * (fromLocationInventory.AverageSalesPrice * salesOrderTotalStock)
                             - (inventory.PricePerUnit * inventory.Stock))
                            / (-1 * salesOrderTotalStock - inventory.Stock);
                    }

                    fromLocationInventory.Stock += inventory.Stock;
                    _inventoryHistoryRepository.Delete(existingSalesOrderInventoryHistory);
                    Update(fromLocationInventory);
                }
            }
        }

        public async Task<InventoryList> GetInventories(InventoryResource inventoryResource)
        {
            var collectionBeforePaging =
                AllIncluding(c => c.Product, u => u.Product.Unit).ApplySort(inventoryResource.OrderBy,
                _propertyMappingService.GetPropertyMapping<InventoryDto, Inventory>());

            collectionBeforePaging = collectionBeforePaging.Where(c => c.LocationId == inventoryResource.LocationId);

            if (!string.IsNullOrWhiteSpace(inventoryResource.ProductName))
            {
                // trim & ignore casing
                var genreForWhereClause = inventoryResource.ProductName
                    .Trim().ToLowerInvariant();
                var name = Uri.UnescapeDataString(genreForWhereClause);
                var encodingName = WebUtility.UrlDecode(name);
                var ecapestring = Regex.Unescape(encodingName);
                encodingName = encodingName.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_").Replace("[", @"\[").Replace(" ", "%");
                collectionBeforePaging = collectionBeforePaging
                    .Where(a => EF.Functions.Like(a.Product.Name, $"{encodingName}%"));
            }

            var inventoryList = new InventoryList();
            return await inventoryList.Create(collectionBeforePaging, inventoryResource.Skip, inventoryResource.PageSize);
        }

        public InventoryDto ConvertStockAndPriceToBaseUnit(InventoryDto inventory)
        {
            var unit = _unitConversationRepository.AllIncluding(c => c.Parent)
                .FirstOrDefault(c => c.Id == inventory.UnitId);

            if (unit.ParentId.HasValue && unit.Operator.HasValue && unit.Value.HasValue)
            {
                switch (unit.Operator)
                {
                    case Operator.Plush:
                        inventory.Stock = inventory.Stock + unit.Value.Value;
                        inventory.PricePerUnit = inventory.PricePerUnit - unit.Value.Value;
                        break;
                    case Operator.Minus:
                        inventory.Stock = inventory.Stock - unit.Value.Value;
                        inventory.PricePerUnit = inventory.PricePerUnit + unit.Value.Value;
                        break;
                    case Operator.Multiply:
                        inventory.Stock = inventory.Stock * unit.Value.Value;
                        inventory.PricePerUnit = inventory.PricePerUnit / unit.Value.Value;
                        break;
                    case Operator.Divide:
                        inventory.Stock = inventory.Stock / unit.Value.Value;
                        inventory.PricePerUnit = inventory.PricePerUnit * unit.Value.Value;
                        break;
                    default:
                        break;
                }
            }

            return inventory;
        }
        public decimal ConvertStockAndPriceBaseUnitToUnit(Guid UnitId, Inventory inventory)
        {
            var unit = _unitConversationRepository.AllIncluding(c => c.Parent)
                .FirstOrDefault(c => c.Id == UnitId);
            decimal stock = 0;

            if (unit.Operator.HasValue && unit.Value.HasValue)
            {
                switch (unit.Operator)
                {
                    case Operator.Plush:
                        stock = inventory.Stock - unit.Value.Value;
                        break;
                    case Operator.Minus:
                        stock = inventory.Stock + unit.Value.Value;
                        break;
                    case Operator.Multiply:
                        stock = Math.Round(inventory.Stock / unit.Value.Value, 2);
                        break;
                    case Operator.Divide:
                        stock = Math.Round(inventory.Stock * unit.Value.Value, 2);
                        break;
                    default:
                        break;
                }
            }
            return stock;
        }

        public async Task<StockAlertList> GetStockAlertsAsync(StockAlertResource stockAlertResource)
        {
            var collectionBeforePaging =
               AllIncluding(c => c.Product, u => u.Product.Unit, l => l.Location)
               .ApplySort(stockAlertResource.OrderBy, _propertyMappingService.GetPropertyMapping<InventoryDto, Inventory>());

            collectionBeforePaging = collectionBeforePaging.Where(c => c.Product.AlertQuantity.HasValue
                    && c.Stock <= c.Product.AlertQuantity);

            if (stockAlertResource.LocationId.HasValue)
            {
                collectionBeforePaging = collectionBeforePaging.Where(c => c.LocationId == stockAlertResource.LocationId);
            }
            else
            {
                collectionBeforePaging = collectionBeforePaging.Where(l => _userInfoToken.LocationIds.Contains(l.LocationId));
            }

            if (!string.IsNullOrWhiteSpace(stockAlertResource.ProductName))
            {
                // trim & ignore casing
                var genreForWhereClause = stockAlertResource.ProductName
                    .Trim().ToLowerInvariant();
                var name = Uri.UnescapeDataString(genreForWhereClause);
                var encodingName = WebUtility.UrlDecode(name);
                var ecapestring = Regex.Unescape(encodingName);
                encodingName = encodingName.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_").Replace("[", @"\[").Replace(" ", "%");
                collectionBeforePaging = collectionBeforePaging
                    .Where(a => EF.Functions.Like(a.Product.Name, $"{encodingName}%"));
            }

            var stockAlerts = new StockAlertList();

            return await stockAlerts.Create(collectionBeforePaging, stockAlertResource.Skip, stockAlertResource.PageSize);
        }
    }
}
