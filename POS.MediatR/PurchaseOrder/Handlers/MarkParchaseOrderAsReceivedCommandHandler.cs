using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POS.Common.UnitOfWork;
using POS.Data;
using POS.Data.Dto;
using POS.Domain;
using POS.Helper;
using POS.MediatR;
using POS.Repository;

namespace POS.MediatR.PurchaseOrder.Handlers
{
    public class MarkParchaseOrderAsReceivedCommandHandler(
        IPurchaseOrderRepository purchaseOrderRepository,
        IUnitOfWork<POSDbContext> uow,
        ILogger<MarkParchaseOrderAsReceivedCommandHandler> logger,
        IInventoryRepository inventoryRepository)
        : IRequestHandler<MarkParchaseOrderAsReceivedCommand, ServiceResponse<bool>>
    {
        public async Task<ServiceResponse<bool>> Handle(MarkParchaseOrderAsReceivedCommand request, CancellationToken cancellationToken)
        {
            var purchaseOrder = await purchaseOrderRepository.All
                .Include(d => d.PurchaseOrderItems)
                .ThenInclude(t => t.PurchaseOrderItemTaxes)
                .Where(c => c.Id == request.Id).FirstOrDefaultAsync();

            if (purchaseOrder == null)
            {
                logger.LogError("Purchase order does not exists.");
                return ServiceResponse<bool>.Return404();
            }

            if (purchaseOrder.DeliveryStatus == PurchaseDeliveryStatus.RECEIVED)
            {
                return ServiceResponse<bool>.ReturnSuccess();
            }

            purchaseOrder.DeliveryStatus = PurchaseDeliveryStatus.RECEIVED;
            
            purchaseOrderRepository.Update(purchaseOrder);

            var inventories = purchaseOrder.PurchaseOrderItems
                   .Select(cs => new InventoryDto
                   {
                       ProductId = cs.ProductId,
                       LocationId = purchaseOrder.LocationId,
                       PricePerUnit = cs.UnitPrice,
                       PurchaseOrderId = purchaseOrder.Id,
                       Stock = cs.Quantity,
                       UnitId = cs.UnitId,
                       TaxValue = cs.TaxValue,
                       Discount = cs.Discount
                   }).ToList();

            inventories.ForEach(invetory =>
            {
                invetory = inventoryRepository.ConvertStockAndPriceToBaseUnit(invetory);
            });

            var inventoriesToAdd = inventories
                .GroupBy(c => new { c.ProductId, c.LocationId })
                .Select(cs => new InventoryDto
                {
                    InventorySource = InventorySourceEnum.PurchaseOrder,
                    ProductId = cs.Key.ProductId,
                    LocationId = cs.Key.LocationId,
                    PricePerUnit = cs.Sum(d => d.PricePerUnit * d.Stock + d.TaxValue - d.Discount) / cs.Sum(d => d.Stock),
                    PurchaseOrderId = purchaseOrder.Id,
                    Stock = cs.Sum(d => d.Stock),
                }).ToList();

            foreach (var inventory in inventoriesToAdd)
            {
                await inventoryRepository.AddInventory(inventory);
            }

            if (await uow.SaveAsync() <= 0)
            {
                logger.LogError("Error while updating Purchase Order.");
                return ServiceResponse<bool>.Return500();
            }

            return ServiceResponse<bool>.ReturnSuccess();
        }
    }
}
