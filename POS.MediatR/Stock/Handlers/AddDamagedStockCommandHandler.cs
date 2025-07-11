using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using POS.Common.UnitOfWork;
using POS.Data;
using POS.Data.Dto;
using POS.Data.Entities;
using POS.Domain;
using POS.Helper;
using POS.MediatR.Stock.Commands;
using POS.Repository;

namespace POS.MediatR.Stock.Handlers
{
    public class AddDamagedStockCommandHandler (
        UserInfoToken _userInfoToken,
        IDamagedStockRepository _damagedStockRepository,
        ILogger<AddDamagedStockCommandHandler> _logger,
        IMapper _mapper,
        IUnitOfWork<POSDbContext> _uow,
        IInventoryRepository inventoryRepository
        ) : IRequestHandler<AddDamagedStockCommand, ServiceResponse<List<DamagedStockDto>>>
    {
        public async Task<ServiceResponse<List<DamagedStockDto>>> Handle(AddDamagedStockCommand request, CancellationToken cancellationToken)
        {
            var damagedStock = new List<DamagedStock>();
            var inventies= new List<InventoryDto>();
            foreach (var item in request.DamagedStockItems)
            {
                var entity = _mapper.Map<DamagedStock>(request);
                entity.Id = Guid.NewGuid();
                entity.DamagedDate = request.DamagedDate;
                entity.ReportedId = request.ReportedId;
                entity.ProductId = item.ProductId;
                entity.DamagedQuantity = item.DamagedQuantity;
                entity.LocationId = request.LocationId;
                entity.CreatedBy = Guid.Parse(_userInfoToken.Id);
                entity.CreatedDate = DateTime.UtcNow;
                damagedStock.Add(entity);

                var inventoryDto = new InventoryDto
                {
                    InventorySource = InventorySourceEnum.DamageStock,
                    ProductId = item.ProductId,
                    LocationId = request.LocationId,
                    PricePerUnit = 0,
                    DamagedStockId = entity.Id,
                    Stock = item.DamagedQuantity,
                    UnitId = item.UnitId
                };
                inventies.Add(inventoryDto);
            }

            _damagedStockRepository.AddRange(damagedStock);

            foreach (var inventory in inventies)
            {
                await inventoryRepository.AddInventory(inventory);
            }
            if (await _uow.SaveAsync() <= 0)
            {

                _logger.LogError("Error While saving Damaged Stock.");
                return ServiceResponse<List<DamagedStockDto>>.Return500();
            }
            var entityDto = _mapper.Map<List<DamagedStockDto>>(damagedStock);
            return ServiceResponse<List<DamagedStockDto>>.ReturnResultWith200(entityDto);
        }
    }
}
