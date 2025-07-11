using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POS.Common.GenericRepository;
using POS.Common.UnitOfWork;
using POS.Data;
using POS.Data.Dto;
using POS.Data.Entities;
using POS.Data.Resources;
using POS.Domain;

namespace POS.Repository
{
    public class DailyStockRepository : GenericRepository<DailyStock, POSDbContext>, IDailyStockRepository
    {
        private readonly IPropertyMappingService _propertyMappingService;
        public DailyStockRepository(IUnitOfWork<POSDbContext> uow, IPropertyMappingService propertyMappingService)
          : base(uow)
        {
            _propertyMappingService = propertyMappingService;
        }

        public async Task<DailyStockList> GetAllDailyStocks(DailyStockResource dailyStockResource)
        {
            var collectionBeforePaging = AllIncluding(c => c.Product, cs => cs.Location).ApplySort(dailyStockResource.OrderBy,
                _propertyMappingService.GetPropertyMapping<DailyStockDto, DailyStock>());

            if (!dailyStockResource.DailyStockDate.HasValue)
            {
                dailyStockResource.DailyStockDate = DateTime.UtcNow;
            }

            if (dailyStockResource.ProductId.HasValue)
            {
                collectionBeforePaging = collectionBeforePaging
                    .Where(c => c.Product.Id == dailyStockResource.ProductId);
            }

            if (dailyStockResource.DailyStockDate.HasValue)
            {
                collectionBeforePaging = collectionBeforePaging
                    .Where(a => a.DailyStockDate >= dailyStockResource.DailyStockDate && a.DailyStockDate < dailyStockResource.DailyStockDate.Value.AddDays(1));
            }

            if (dailyStockResource.LocationId.HasValue)
            {
                collectionBeforePaging = collectionBeforePaging
                    .Where(a => a.LocationId == dailyStockResource.LocationId);
            }


            var salesOrders = new DailyStockList();
            return await salesOrders
                .Create(collectionBeforePaging, dailyStockResource.Skip, dailyStockResource.PageSize);
        }

        public async Task AddDailyStock(InventoryDto inventory)
        {
            var currentDate = DateTime.UtcNow.Date;
            //Get Current Damage stock on today date
            var dailyStock = await All.Where(x => x.ProductId == inventory.ProductId && x.LocationId == inventory.LocationId && x.DailyStockDate.Date == currentDate.Date).FirstOrDefaultAsync();
            if (dailyStock != null)
            {
                if (inventory.InventorySource == InventorySourceEnum.SalesOrder)
                {
                    dailyStock.ClosingStock -= inventory.Stock;
                    dailyStock.QuantitySold += inventory.Stock;
                }
                else if (inventory.InventorySource == InventorySourceEnum.PurchaseOrder)
                {
                    dailyStock.ClosingStock += inventory.Stock;
                    dailyStock.QuantityPurchased += inventory.Stock;
                }
                else if (inventory.InventorySource == InventorySourceEnum.PurchaseOrderReturn)
                {
                    dailyStock.ClosingStock -= inventory.Stock;
                    dailyStock.QuantityPurchasedReturned += inventory.Stock;
                }
                else if (inventory.InventorySource == InventorySourceEnum.SalesOrderReturn)
                {
                    dailyStock.ClosingStock += inventory.Stock;
                    dailyStock.QuantitySoldReturned += inventory.Stock;
                }
                else if (inventory.InventorySource == InventorySourceEnum.DeletePurchaseOrder)
                {
                    dailyStock.ClosingStock -= inventory.Stock;
                }
                else if (inventory.InventorySource == InventorySourceEnum.DeleteSalesOrder)
                {
                    dailyStock.ClosingStock += inventory.Stock;
                }
                else if (inventory.InventorySource == InventorySourceEnum.Direct)
                {
                    dailyStock.ClosingStock += inventory.Stock;
                    dailyStock.QuantityAdjusted += inventory.Stock;
                }
                else if (inventory.InventorySource == InventorySourceEnum.DamageStock)
                {
                    dailyStock.ClosingStock -= inventory.Stock;
                    dailyStock.QuantityDamaged += inventory.Stock;
                }
                else if (inventory.InventorySource == InventorySourceEnum.StockTransfer)
                {
                    dailyStock.ClosingStock += inventory.Stock;
                    dailyStock.QuantityToTransfter += inventory.Stock;
                    var dailyStockFromLocation = await All.Where(x => x.ProductId == inventory.ProductId && x.LocationId == inventory.FromLocationId && x.DailyStockDate.Date == currentDate.Date).FirstOrDefaultAsync();
                    if (dailyStockFromLocation != null)
                    {
                        dailyStockFromLocation.ClosingStock -= inventory.Stock;
                        dailyStockFromLocation.QuantityFromTransfter += inventory.Stock;
                        Update(dailyStockFromLocation);
                    }
                    else
                    {
                        var oldDailyStockFromLocation = await All.Where(x => x.ProductId == inventory.ProductId && x.LocationId == inventory.FromLocationId).OrderByDescending(c => c.DailyStockDate).FirstOrDefaultAsync();
                        var dialStockForFromLocation = new DailyStock
                        {
                            Id = Guid.NewGuid(),
                            ProductId = inventory.ProductId,
                            LocationId = inventory.FromLocationId.Value,
                            OpeningStock = oldDailyStockFromLocation != null ? oldDailyStockFromLocation.ClosingStock : 0,
                            ClosingStock = oldDailyStockFromLocation != null ? oldDailyStockFromLocation.ClosingStock - inventory.Stock : -inventory.Stock,
                            QuantitySold = 0,
                            QuantityPurchased = 0,
                            QuantityDamaged = 0,
                            QuantitySoldReturned = 0,
                            QuantityPurchasedReturned = 0,
                            QuantityAdjusted = 0,
                            QuantityToTransfter = 0,
                            QuantityFromTransfter = inventory.Stock,
                            DailyStockDate = currentDate,
                            LastUpdateDate = currentDate
                        };
                        Add(dialStockForFromLocation);
                    }
                }
                dailyStock.LastUpdateDate = currentDate;
                Update(dailyStock);
            }
            else
            {
                var oldDailyStock = await All.Where(x => x.ProductId == inventory.ProductId && x.LocationId == inventory.LocationId).OrderByDescending(c => c.DailyStockDate).FirstOrDefaultAsync();
                var newDailStock = new DailyStock
                {
                    Id = Guid.NewGuid(),
                    ProductId = inventory.ProductId,
                    LocationId = inventory.LocationId,
                    OpeningStock = oldDailyStock != null ? oldDailyStock.ClosingStock : 0,
                    ClosingStock = oldDailyStock != null ? oldDailyStock.ClosingStock : 0,
                    QuantitySold = 0,
                    QuantityPurchased = 0,
                    QuantityDamaged = 0,
                    QuantitySoldReturned = 0,
                    QuantityPurchasedReturned = 0,
                    QuantityAdjusted = 0,
                    QuantityToTransfter = 0,
                    QuantityFromTransfter = 0,
                    DailyStockDate = currentDate,
                    LastUpdateDate = currentDate
                };

                if (inventory.InventorySource == InventorySourceEnum.SalesOrder)
                {
                    newDailStock.ClosingStock -= inventory.Stock;
                    newDailStock.QuantitySold = inventory.Stock;
                }
                else if (inventory.InventorySource == InventorySourceEnum.PurchaseOrder)
                {
                    newDailStock.ClosingStock += inventory.Stock;
                    newDailStock.QuantityPurchased = inventory.Stock;
                }
                else if (inventory.InventorySource == InventorySourceEnum.PurchaseOrderReturn)
                {
                    newDailStock.ClosingStock -= inventory.Stock;
                    newDailStock.QuantityPurchasedReturned = inventory.Stock;
                }
                else if (inventory.InventorySource == InventorySourceEnum.SalesOrderReturn)
                {
                    newDailStock.ClosingStock += inventory.Stock;
                    newDailStock.QuantitySoldReturned = inventory.Stock;
                }
                else if (inventory.InventorySource == InventorySourceEnum.DeletePurchaseOrder)
                {
                    newDailStock.ClosingStock -= inventory.Stock;
                }
                else if (inventory.InventorySource == InventorySourceEnum.DeleteSalesOrder)
                {
                    newDailStock.ClosingStock += inventory.Stock;
                }
                else if (inventory.InventorySource == InventorySourceEnum.Direct)
                {
                    newDailStock.ClosingStock += inventory.Stock;
                    newDailStock.QuantityAdjusted = inventory.Stock;
                }
                else if (inventory.InventorySource == InventorySourceEnum.DamageStock)
                {
                    newDailStock.ClosingStock -= inventory.Stock;
                    newDailStock.QuantityDamaged = inventory.Stock;
                }
                else if (inventory.InventorySource == InventorySourceEnum.StockTransfer)
                {
                    newDailStock.ClosingStock += inventory.Stock;
                    newDailStock.QuantityToTransfter = inventory.Stock;
                    var dailyStockFromLocation = await All.Where(x => x.ProductId == inventory.ProductId && x.LocationId == inventory.FromLocationId && x.DailyStockDate.Date == currentDate.Date).FirstOrDefaultAsync();
                    if (dailyStockFromLocation != null)
                    {
                        dailyStockFromLocation.ClosingStock -= inventory.Stock;
                        dailyStockFromLocation.QuantityFromTransfter += inventory.Stock;
                        Update(dailyStockFromLocation);
                    }
                    else
                    {
                        var oldDailyStockFromLocation = await All.Where(x => x.ProductId == inventory.ProductId && x.LocationId == inventory.FromLocationId).OrderByDescending(c => c.DailyStockDate).FirstOrDefaultAsync();
                        var dialStockForFromLocation = new DailyStock
                        {
                            Id = Guid.NewGuid(),
                            ProductId = inventory.ProductId,
                            LocationId = inventory.FromLocationId.Value,
                            OpeningStock = oldDailyStockFromLocation != null ? oldDailyStockFromLocation.ClosingStock : 0,
                            ClosingStock = oldDailyStockFromLocation != null ? oldDailyStockFromLocation.ClosingStock - inventory.Stock : 0,
                            QuantitySold = 0,
                            QuantityPurchased = 0,
                            QuantityDamaged = 0,
                            QuantitySoldReturned = 0,
                            QuantityPurchasedReturned = 0,
                            QuantityAdjusted = 0,
                            QuantityToTransfter = 0,
                            QuantityFromTransfter = inventory.Stock,
                            DailyStockDate = currentDate,
                            LastUpdateDate = currentDate
                        };
                        Add(dialStockForFromLocation);
                    }
                }
                newDailStock.LastUpdateDate = currentDate;
                Add(newDailStock);
            }
        }
    }
}
