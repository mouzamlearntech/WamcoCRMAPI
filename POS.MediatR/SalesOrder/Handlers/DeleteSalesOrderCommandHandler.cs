using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POS.Common.UnitOfWork;
using POS.Data;
using POS.Data.Dto;
using POS.Data.Entities;
using POS.Domain;
using POS.Helper;
using POS.MediatR.CommandAndQuery;
using POS.Repository;

namespace POS.MediatR.Handlers
{
    public class DeleteSalesOrderCommandHandler
          : IRequestHandler<DeleteSalesOrderCommand, ServiceResponse<bool>>
    {
        private readonly ISalesOrderRepository _salesOrderRepository;
        private readonly ILogger<DeleteSalesOrderCommandHandler> _logger;
        private readonly IUnitOfWork<POSDbContext> _uow;
        private readonly IInventoryRepository _inventoryRepository;

        public DeleteSalesOrderCommandHandler(ISalesOrderRepository salesOrderRepository,
            ILogger<DeleteSalesOrderCommandHandler> logger,
            IUnitOfWork<POSDbContext> uow,
            IInventoryRepository inventoryRepository)
        {
            _salesOrderRepository = salesOrderRepository;
            _logger = logger;
            _uow = uow;
            _inventoryRepository = inventoryRepository;
        }
        public async Task<ServiceResponse<bool>> Handle(DeleteSalesOrderCommand request, CancellationToken cancellationToken)
        {
            var salesOrder = await _salesOrderRepository.AllIncluding(c => c.SalesOrderItems).FirstOrDefaultAsync(c => c.Id == request.Id);

            if (salesOrder == null)
            {
                _logger.LogError("Sales order does not exists.");
                return ServiceResponse<bool>.Return404();
            }

            _salesOrderRepository.Delete(salesOrder);

            var inventories = salesOrder.SalesOrderItems
                .Select(cs => new InventoryDto
                {
                    ProductId = cs.ProductId,
                    LocationId = salesOrder.LocationId,
                    PricePerUnit = cs.UnitPrice,
                    SalesOrderId = salesOrder.Id,
                    Stock = cs.Status == PurchaseSaleItemStatusEnum.Not_Return ? cs.Quantity : (-1) * cs.Quantity,
                    UnitId = cs.UnitId,
                    TaxValue = cs.TaxValue,
                    Discount = cs.Discount
                }).ToList();

            inventories.ForEach(invetory =>
            {
                invetory = _inventoryRepository.ConvertStockAndPriceToBaseUnit(invetory);
            });

            var inventoriesToDelete = inventories
                .GroupBy(c => new { c.ProductId, c.LocationId })
                .Select(cs => new InventoryDto
                {
                    InventorySource = InventorySourceEnum.DeleteSalesOrder,
                    ProductId = cs.Key.ProductId,
                    LocationId = cs.Key.LocationId,
                    PricePerUnit = cs.Sum(d => d.PricePerUnit * d.Stock + d.TaxValue - d.Discount) / cs.Sum(d => d.Stock),
                    SalesOrderId = salesOrder.Id,
                    Stock = cs.Sum(d => d.Stock),
                }).ToList();

            foreach (var inventory in inventoriesToDelete)
            {
                await _inventoryRepository.AddInventory(inventory);
            }

            if (await _uow.SaveAsync() <= 0)
            {
                _logger.LogError("Error while deleting Sales order.");
                return ServiceResponse<bool>.Return500();
            }

            return ServiceResponse<bool>.ReturnSuccess();
        }
    }

}
