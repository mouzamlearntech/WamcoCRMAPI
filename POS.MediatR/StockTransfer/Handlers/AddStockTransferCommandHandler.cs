using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime.Telemetry;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POS.Common.UnitOfWork;
using POS.Data;
using POS.Data.Dto;
using POS.Data.Enums;
using POS.Domain;
using POS.Helper;
using POS.MediatR.Commands;
using POS.Repository;

namespace POS.MediatR.Handlers
{
    public class AddStockTransferCommandHandler(IUnitOfWork<POSDbContext> _uow,
        IMapper _mapper,
        IStockTransferRepository _stockTransferRepository,
        IInventoryRepository inventoryRepository,
        ILogger<AddStockTransferCommand> _logger,
        IDailyStockRepository dailyStockRepository
        ) : IRequestHandler<AddStockTransferCommand, ServiceResponse<StockTransferDto>>
    {
        public async Task<ServiceResponse<StockTransferDto>> Handle(AddStockTransferCommand request, CancellationToken cancellationToken)
        {
            var entity = _mapper.Map<Data.Entities.StockTransfer>(request);

            foreach (var item in entity.StockTransferItems)
            {
                item.Product = null;
                item.Unit = null;
            }

            _stockTransferRepository.Add(entity);

            if (request.Status == StockTransferStatus.Delivered)
            {
                var inventories = new List<InventoryDto>();
                foreach (var item in request.StockTransferItems)
                {
                    var inventoryDto = new InventoryDto
                    {
                        ProductId = item.ProductId,
                        LocationId= request.ToLocationId,
                        PricePerUnit = item.UnitPrice + (item.ShippingCharge / item.Quantity),
                        StockTransferId = entity.Id,
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
                        StockTransferId = entity.Id,
                        Stock = group.Sum(d => d.Stock),
                        FromLocationId = request.FromLocationId,
                        ToLocationId = request.ToLocationId,
                        LocationId = request.ToLocationId,
                        InventorySource = InventorySourceEnum.StockTransfer
                    })
                    .Where(i => i.Stock != 0)
                    .ToList();

                foreach (var inventory in inventoriesToAdd)
                {
                    await inventoryRepository.CreateStockTransfer(inventory);
                    await dailyStockRepository.AddDailyStock(inventory);
                }
            }

            if (await _uow.SaveAsync() <= 0)
            {
                _logger.LogError("Save Page have Error");
                return ServiceResponse<StockTransferDto>.Return500();
            }
            var entityToReturn = _mapper.Map<StockTransferDto>(entity);
            return ServiceResponse<StockTransferDto>.ReturnResultWith200(entityToReturn);
        }
    }
}
