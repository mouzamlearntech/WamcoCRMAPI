using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POS.Common.UnitOfWork;
using POS.Data;
using POS.Data.Dto;
using POS.Domain;
using POS.Helper;
using POS.MediatR.Commands;
using POS.Repository;

namespace POS.MediatR.Handlers
{
    public class UpdateStockTransferCommandHandler(
        IUnitOfWork<POSDbContext> _uow,
        ILogger<UpdateStockTransferCommand> _logger,
        IMapper _mapper,
        IStockTransferRepository _stockTransferRepository,
        IStockTransferItemRepository stockTransferItemRepository,
        IInventoryRepository inventoryRepository,
        IDailyStockRepository dailyStockRepository)
        : IRequestHandler<UpdateStockTransferCommand, ServiceResponse<StockTransferDto>>
    {
        public async Task<ServiceResponse<StockTransferDto>> Handle(UpdateStockTransferCommand request, CancellationToken cancellationToken)
        {
            var entityExist = await _stockTransferRepository.All.FirstOrDefaultAsync(c => c.Id == request.Id);
            if (entityExist == null)
            {
                _logger.LogError("Stock transfer not found.");
                return ServiceResponse<StockTransferDto>.Return404("Stock transfer not found.");
            }

            if (entityExist.Status == Data.Enums.StockTransferStatus.Delivered)
            {
                return ServiceResponse<StockTransferDto>.ReturnFailed(404, "stock transfer can't be edited as it's already delivered.");
            }

            if (request.Status == Data.Enums.StockTransferStatus.Delivered)
            {
                var inventories = new List<InventoryDto>();
                foreach (var item in request.StockTransferItems)
                {
                    var inventoryDto = new InventoryDto
                    {
                        ProductId = item.ProductId,
                        PricePerUnit = item.UnitPrice + (item.ShippingCharge / item.Quantity),
                        StockTransferId = entityExist.Id,
                        Stock = item.Quantity,
                        UnitId = item.UnitId,
                        TaxValue = 0,
                        Discount = 0,
                        FromLocationId = request.FromLocationId,
                        ToLocationId = request.ToLocationId
                    };
                    inventoryDto = inventoryRepository.ConvertStockAndPriceToBaseUnit(inventoryDto);
                    inventories.Add(inventoryDto);
                }

                var inventoriesToAdd = inventories.GroupBy(i => i.ProductId)
                    .Select(group => new InventoryDto
                    {
                        ProductId = group.First().ProductId,
                        PricePerUnit = group.Sum(d => (d.PricePerUnit * d.Stock) + d.TaxValue - d.Discount) / group.Sum(d => d.Stock),
                        StockTransferId = entityExist.Id,
                        Stock = group.Sum(d => d.Stock),
                        FromLocationId = request.FromLocationId,
                        ToLocationId = request.ToLocationId,
                        InventorySource = InventorySourceEnum.StockTransfer,
                        LocationId= request.ToLocationId
                    })
                    .Where(i => i.Stock != 0)
                    .ToList();

                foreach (var inventory in inventoriesToAdd)
                {
                    await inventoryRepository.CreateStockTransfer(inventory);
                    await dailyStockRepository.AddDailyStock(inventory);
                }
            }

            var existingStockItems = await stockTransferItemRepository
                .FindBy(c => c.StockTransferId == request.Id)
                .ToListAsync();

            foreach (var item in existingStockItems)
            {
                stockTransferItemRepository.Delete(item);
            }

            entityExist = await _stockTransferRepository.FindBy(v => v.Id == request.Id).FirstOrDefaultAsync();
            _mapper.Map(request, entityExist);

            foreach (var item in entityExist.StockTransferItems)
            {
                var dbItem = _mapper.Map<Data.Entities.StockTransferItem>(item);
                dbItem.StockTransferId = entityExist.Id;
                dbItem.Product = null;
                dbItem.Unit = null;
                stockTransferItemRepository.Add(dbItem);
            }

            _stockTransferRepository.Update(entityExist);

            if (await _uow.SaveAsync() <= 0)
            {
                return ServiceResponse<StockTransferDto>.Return500();
            }
            var result = _mapper.Map<StockTransferDto>(entityExist);
            return ServiceResponse<StockTransferDto>.ReturnResultWith200(result);
        }
    }
}
