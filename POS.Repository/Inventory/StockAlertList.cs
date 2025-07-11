using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POS.Data.Dto;
using POS.Data;

namespace POS.Repository
{

    public class StockAlertList : List<StockAlertDto>
    {
        public StockAlertList()
        {
        }

        public int Skip { get; private set; }
        public int TotalPages { get; private set; }
        public int PageSize { get; private set; }
        public int TotalCount { get; private set; }

        public StockAlertList(List<StockAlertDto> items, int count, int skip, int pageSize)
        {
            TotalCount = count;
            PageSize = pageSize;
            Skip = skip;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            AddRange(items);
        }

        public async Task<StockAlertList> Create(IQueryable<Inventory> source, int skip, int pageSize)
        {
            var count = await GetCount(source);
            var dtoList = await GetDtos(source, skip, pageSize);
            var dtoPageList = new StockAlertList(dtoList, count, skip, pageSize);
            return dtoPageList;
        }

        public async Task<int> GetCount(IQueryable<Inventory> source)
        {
            return await source.AsNoTracking().CountAsync();
        }

        public async Task<List<StockAlertDto>> GetDtos(IQueryable<Inventory> source, int skip, int pageSize)
        {

            var entities = await source
               .Skip(skip)
               .Take(pageSize)
               .AsNoTracking()
               .Select(c => new StockAlertDto
               {
                   ProductId = c.ProductId,
                   ProductName = c.Product.Name,
                   Stock = c.Stock,
                   BusinessLocation = c.Location.Name,
                   Unit = c.Product.Unit.Name
               }).ToListAsync();

            return entities;

        }
    }
}
