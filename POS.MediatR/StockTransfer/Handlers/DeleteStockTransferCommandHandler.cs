using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using POS.Common.UnitOfWork;
using POS.Data;
using POS.Data.Dto;
using POS.Domain;
using POS.Helper;
using POS.MediatR.Commands;
using POS.Repository;

namespace POS.MediatR.Handlers
{
    public class DeleteStockTransferCommandHandler(IStockTransferRepository _stockTransferRepository,
        IUnitOfWork<POSDbContext> _uow,
        IInventoryRepository inventoryRepository,
        IDailyStockRepository dailyStockRepository)
        : IRequestHandler<DeleteStockTransferCommand, ServiceResponse<bool>>
    {
        public async Task<ServiceResponse<bool>> Handle(DeleteStockTransferCommand request, CancellationToken cancellationToken)
        {
            var entityExist = await _stockTransferRepository
                .AllIncluding(c => c.StockTransferItems)
                .FirstOrDefaultAsync(d => d.Id == request.Id);

            if (entityExist == null)
            {
                return ServiceResponse<bool>.Return404();
            }

            _stockTransferRepository.Delete(request.Id);

            if (entityExist.Status == Data.Enums.StockTransferStatus.Delivered)
            {
                var inventories = new List<InventoryDto>();
                foreach (var item in entityExist.StockTransferItems)
                {
                    var inventoryDto = new InventoryDto
                    {
                        ProductId = item.ProductId,
                        PricePerUnit = item.UnitPrice + (item.ShippingCharge / item.Quantity),
                        StockTransferId = entityExist.Id,
                        Stock = item.Quantity,
                        UnitId = item.UnitId.Value,
                        TaxValue = 0,
                        Discount = 0,
                        FromLocationId = entityExist.FromLocationId,
                        ToLocationId = entityExist.ToLocationId
                    };
                    inventoryDto = inventoryRepository.ConvertStockAndPriceToBaseUnit(inventoryDto);
                    inventories.Add(inventoryDto);
                }

                var inventoriesToDelete = inventories.GroupBy(i => i.ProductId)
                    .Select(group => new InventoryDto
                    {
                        ProductId = group.First().ProductId,
                        PricePerUnit = group.Sum(d => (d.PricePerUnit * d.Stock) + d.TaxValue - d.Discount) / group.Sum(d => d.Stock),
                        StockTransferId = entityExist.Id,
                        Stock = group.Sum(d => d.Stock),
                        FromLocationId = entityExist.FromLocationId,
                        ToLocationId = entityExist.ToLocationId,
                        LocationId = entityExist.ToLocationId,
                        InventorySource = InventorySourceEnum.StockTransfer
                    })
                    .Where(i => i.Stock != 0)
                    .ToList();

                foreach (var inventory in inventoriesToDelete)
                {
                    await inventoryRepository.DeleteStockTransfer(inventory);
                    inventory.Stock = -1 * inventory.Stock;
                    await dailyStockRepository.AddDailyStock(inventory);
                }
            }
            if (await _uow.SaveAsync() <= 0)
            {
                return ServiceResponse<bool>.Return500();
            }

            return ServiceResponse<bool>.ReturnResultWith200(true);
        }
    }
}
