using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POS.Data.Dto;
using POS.Data.Entities;

namespace POS.Repository
{
    public class DailyStockList : List<DailyStockDto>
    {
        public DailyStockList()
        {

        }
        public int Skip { get; private set; }
        public int TotalPages { get; private set; }
        public int PageSize { get; private set; }
        public int TotalCount { get; private set; }

        public DailyStockList(List<DailyStockDto> items, int count, int skip, int pageSize)
        {
            TotalCount = count;
            PageSize = pageSize;
            Skip = skip;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            AddRange(items);
        }

        public async Task<DailyStockList> Create(IQueryable<DailyStock> source, int skip, int pageSize)
        {
            var count = await GetCount(source);
            var dtoList = await GetDtos(source, skip, pageSize);
            var dtoPageList = new DailyStockList(dtoList, count, skip, pageSize);
            return dtoPageList;
        }

        public async Task<int> GetCount(IQueryable<DailyStock> source)
        {
            try
            {
                return await source.AsNoTracking().CountAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task<List<DailyStockDto>> GetDtos(IQueryable<DailyStock> source, int skip, int pageSize)
        {
            var entities = await source
                    .Skip(skip)
                    .Take(pageSize)
                    .AsNoTracking()
                    .Select(cs => new DailyStockDto
                    {
                        Id = cs.Id,
                        ClosingStock = cs.ClosingStock,
                        DailyStockDate = cs.DailyStockDate,
                        LastUpdateDate = cs.LastUpdateDate,
                        QuantityAdjusted = cs.QuantityAdjusted,
                        QuantityDamaged = cs.QuantityDamaged,
                        QuantityFromTransfter = cs.QuantityFromTransfter,
                        QuantityPurchased = cs.QuantityPurchased,
                        QuantityPurchasedReturned = cs.QuantityPurchasedReturned,
                        QuantitySold = cs.QuantitySold,
                        QuantitySoldReturned = cs.QuantitySoldReturned,
                        QuantityToTransfter = cs.QuantityToTransfter,
                        OpeningStock = cs.OpeningStock,
                        LocationId = cs.Location != null ? cs.Location.Name : null,
                        ProductId = cs.Product != null ? cs.Product.Name : null,
                    })
                    .ToListAsync();
            return entities;
        }
    }
}
