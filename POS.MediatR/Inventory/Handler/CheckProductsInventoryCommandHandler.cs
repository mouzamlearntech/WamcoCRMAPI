using Amazon.Runtime.Telemetry;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Data.Dto;
using POS.Helper;
using POS.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.MediatR.Inventory.Handler
{
    public class CheckProductsInventoryCommandHandler(IProductRepository productRepository, IInventoryRepository inventoryRepository) : IRequestHandler<CheckProductsInventoryCommand, ServiceResponse<List<ProductUnitQuantityDto>>>
    {
        public async Task<ServiceResponse<List<ProductUnitQuantityDto>>> Handle(CheckProductsInventoryCommand request, CancellationToken cancellationToken)
        {
            var productInventoryUnits = productRepository.All
                .Include(x => x.Inventories.Where(c => c.LocationId == request.LocationId))
                .Include(x => x.Unit)
                .Where(x => request.ProductIds.Select(c => c.ProductId).Contains(x.Id))
                 .Select(x => new ProductInventoryStockDto
                 {
                     Name = x.Name,
                     Id = x.Id,
                     UnitId = x.UnitId,
                     Inventories = x.Inventories,
                     UnitName = x.Unit.Name,
                     ParentUnitId = x.Unit.ParentId
                 }).ToList();

            List<ProductUnitQuantityDto> productInventoryStockDtos = new List<ProductUnitQuantityDto>();

            foreach (var item in productInventoryUnits)
            {
                decimal stock = item.Inventories.FirstOrDefault() != null ? item.Inventories.FirstOrDefault().Stock : 0;
                if (request.ProductIds.Any(c => c.ProductId == item.Id) && !request.ProductIds.Any(c => c.UnitId == item.UnitId) &&  item.Inventories.Count>0)
                {
                    var unitId = request.ProductIds.Where(c => c.ProductId == item.Id).FirstOrDefault().UnitId;
                    stock = inventoryRepository.ConvertStockAndPriceBaseUnitToUnit(unitId, item.Inventories.FirstOrDefault());
                } 
                productInventoryStockDtos.Add(new ProductUnitQuantityDto
                {
                    ProductId = item.Id,
                    Name= item.Name,
                    Stock = stock,
                    UnitId = item.UnitId
                });
            }
            return ServiceResponse<List<ProductUnitQuantityDto>>.ReturnResultWith200(productInventoryStockDtos);

        }
    }
}
