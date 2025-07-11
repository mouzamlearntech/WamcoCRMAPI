using POS.Common.UnitOfWork;
using POS.Domain;
using POS.Helper;
using POS.MediatR.CommandAndQuery;
using POS.Repository;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POS.Data.Dto;
using POS.Data.Entities;
using POS.Data;

namespace POS.MediatR.Handlers
{

    public class DeletePurchaseOrderCommandHandler
       : IRequestHandler<DeletePurchaseOrderCommand, ServiceResponse<bool>>
    {
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly ILogger<DeletePurchaseOrderCommandHandler> _logger;
        private readonly IUnitOfWork<POSDbContext> _uow;
        private readonly IInventoryRepository _inventoryRepository;

        public DeletePurchaseOrderCommandHandler(IPurchaseOrderRepository purchaseOrderRepository,
            ILogger<DeletePurchaseOrderCommandHandler> logger,
            IUnitOfWork<POSDbContext> uow,
            IInventoryRepository inventoryRepository)
        {
            _purchaseOrderRepository = purchaseOrderRepository;
            _logger = logger;
            _uow = uow;
            _inventoryRepository = inventoryRepository;
        }
        public async Task<ServiceResponse<bool>> Handle(DeletePurchaseOrderCommand request, CancellationToken cancellationToken)
        {
            var purchaseOrder = await _purchaseOrderRepository.AllIncluding(c => c.PurchaseOrderItems).FirstOrDefaultAsync(c => c.Id == request.Id);

            if (purchaseOrder == null)
            {
                _logger.LogError("Purchase order does not exists.");
                return ServiceResponse<bool>.Return404();
            }

            purchaseOrder.IsDeleted = true;
            _purchaseOrderRepository.Update(purchaseOrder);

            var inventories = purchaseOrder.PurchaseOrderItems
                  .Select(cs => new InventoryDto
                  {
                      ProductId = cs.ProductId,
                      LocationId = purchaseOrder.LocationId,
                      PricePerUnit = cs.UnitPrice,
                      PurchaseOrderId = purchaseOrder.Id,
                      Stock = cs.Status == PurchaseSaleItemStatusEnum.Not_Return ? cs.Quantity : (-1) * cs.Quantity,
                      UnitId = cs.UnitId,
                      TaxValue = cs.TaxValue,
                      Discount = cs.Discount,
                  }).ToList();

            inventories.ForEach(invetory =>
            {
                invetory = _inventoryRepository.ConvertStockAndPriceToBaseUnit(invetory);
            });

            var inventoriesToDelete = inventories
                .GroupBy(c => new { c.ProductId, c.LocationId })
                .Select(cs => new InventoryDto
                {
                    InventorySource = InventorySourceEnum.DeletePurchaseOrder,
                    ProductId = cs.Key.ProductId,
                    LocationId = cs.Key.LocationId,
                    PricePerUnit = cs.Sum(d => d.PricePerUnit * d.Stock + d.TaxValue - d.Discount) / cs.Sum(d => d.Stock),
                    PurchaseOrderId = purchaseOrder.Id,
                    Stock = cs.Sum(d => d.Stock),
                }).ToList();

            foreach (var inventory in inventoriesToDelete)
            {
                await _inventoryRepository.AddInventory(inventory);
            }

            if (await _uow.SaveAsync() <= 0)
            {
                _logger.LogError("Error while deleting Purchase order.");
                return ServiceResponse<bool>.Return500();
            }

            return ServiceResponse<bool>.ReturnSuccess();
        }
    }
}
