using System;
using System.Linq.Dynamic.Core;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using POS.Helper;
using POS.MediatR.Stock.Commands;
using POS.Repository;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using POS.Data.Entities;
using POS.Common.UnitOfWork;
using POS.Domain;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace POS.MediatR.Stock.Handlers
{
    public class DailyStockSchedulerQueryHandler
        (IDailyStockRepository dailyStockRepository,
         IUnitOfWork<POSDbContext> uow,
         ILogger<DailyStockSchedulerQueryHandler> logger)
        : IRequestHandler<DailyStockSchedulerQuery, bool>
    {
        public async Task<bool> Handle(DailyStockSchedulerQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var now = DateTime.UtcNow;
                var startDate = new DateTime(now.Year, now.Month, now.Day - 1, 0, 0, 0);
                var endDate = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
                var yesterdaysDailyStock = await dailyStockRepository
                    .All
                    .Where(d => d.DailyStockDate >= startDate && d.DailyStockDate < endDate)
                    .ToListAsync();

                var dailyStockToBeAdded = new List<DailyStock>();
                foreach (var dailyStock in yesterdaysDailyStock)
                {
                    var existingStock = await dailyStockRepository.All
                        .FirstOrDefaultAsync(c => c.ProductId == dailyStock.ProductId
                        && c.DailyStockDate >= endDate
                        && c.LocationId == dailyStock.LocationId);

                    if (existingStock == null)
                    {
                        dailyStockToBeAdded.Add(new DailyStock
                        {
                            Id = Guid.NewGuid(),
                            ProductId = dailyStock.ProductId,
                            DailyStockDate = endDate,
                            OpeningStock = dailyStock.ClosingStock,
                            ClosingStock = dailyStock.ClosingStock,
                            LastUpdateDate = endDate,
                            LocationId = dailyStock.LocationId,
                            QuantityAdjusted = 0,
                            QuantityDamaged = 0,
                            QuantityFromTransfter = 0,
                            QuantityPurchased = 0,
                            QuantityPurchasedReturned = 0,
                            QuantitySold = 0,
                            QuantitySoldReturned = 0,
                            QuantityToTransfter = 0,
                        });
                    }
                }

                if (dailyStockToBeAdded.Count > 0)
                {
                    dailyStockRepository.AddRange(dailyStockToBeAdded);
                    await uow.SaveAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "error in daily stock scheduler");
                return true;
            }
        }
    }
}
