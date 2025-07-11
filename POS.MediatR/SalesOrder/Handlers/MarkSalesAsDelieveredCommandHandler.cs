using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using POS.Common.UnitOfWork;
using POS.Data.Dto;
using POS.Data;
using POS.Domain;
using POS.Helper;
using POS.MediatR;
using POS.Repository;
using Microsoft.EntityFrameworkCore;
using POS.MediatR.SalesOrder.Commands;

namespace POS.MediatR.SalesOrder.Handlers
{
	public class MarkSalesAsDelieveredCommandHandler(
	  ISalesOrderRepository salesOrderRepository,
	  IUnitOfWork<POSDbContext> uow,
	  ILogger<MarkSalesAsDelieveredCommandHandler> logger,
	  IInventoryRepository inventoryRepository)
	  : IRequestHandler<MarkSalesAsDelieveredCommand, ServiceResponse<bool>>
	{
		public async Task<ServiceResponse<bool>> Handle(MarkSalesAsDelieveredCommand request, CancellationToken cancellationToken)
		{
			var salesOrder = await salesOrderRepository.All
				.Include(d => d.SalesOrderItems)
				.ThenInclude(t => t.SalesOrderItemTaxes)
				.Where(c => c.Id == request.Id).FirstOrDefaultAsync();

			if (salesOrder == null)
			{
				logger.LogError("SalesOrder order does not exists.");
				return ServiceResponse<bool>.Return404();
			}

			if (salesOrder.DeliveryStatus == SalesDeliveryStatus.DELIVERED)
			{
				return ServiceResponse<bool>.ReturnSuccess();
			}

			salesOrder.DeliveryStatus = SalesDeliveryStatus.DELIVERED;

			salesOrderRepository.Update(salesOrder);

			var inventories = salesOrder.SalesOrderItems
				   .Select(cs => new InventoryDto
				   {
					   ProductId = cs.ProductId,
					   LocationId = salesOrder.LocationId,
					   PricePerUnit = cs.UnitPrice,
					   SalesOrderId = salesOrder.Id,
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
					InventorySource = InventorySourceEnum.SalesOrder,
					ProductId = cs.Key.ProductId,
					LocationId = cs.Key.LocationId,
					PricePerUnit = cs.Sum(d => d.PricePerUnit * d.Stock + d.TaxValue - d.Discount) / cs.Sum(d => d.Stock),
					SalesOrderId = salesOrder.Id,
					Stock = cs.Sum(d => d.Stock),
				}).ToList();

			foreach (var inventory in inventoriesToAdd)
			{
				await inventoryRepository.AddInventory(inventory);
			}

			if (await uow.SaveAsync() <= 0)
			{
				logger.LogError("Error while updating SalesOrder Order.");
				return ServiceResponse<bool>.Return500();
			}

			return ServiceResponse<bool>.ReturnSuccess();
		}
	}
}
