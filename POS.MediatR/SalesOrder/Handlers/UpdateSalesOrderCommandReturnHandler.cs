using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POS.Common.UnitOfWork;
using POS.Data;
using POS.Data.Dto;
using POS.Data.Entities;
using POS.Domain;
using POS.Helper;
using POS.MediatR.SalesOrder.Commands;
using POS.Repository;
using System;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace POS.MediatR.Handlers
{
    public class UpdateSalesOrderReturnCommandHandler
        : IRequestHandler<UpdateSalesOrderReturnCommand, ServiceResponse<bool>>
    {
        private readonly ISalesOrderRepository _salesOrderRepository;
        private readonly IUnitOfWork<POSDbContext> _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateSalesOrderCommandHandler> _logger;
        private readonly IInventoryRepository _inventoryRepository;

        public UpdateSalesOrderReturnCommandHandler(
            ISalesOrderRepository salesOrderRepository,
            IUnitOfWork<POSDbContext> uow,
            IMapper mapper,
            ILogger<UpdateSalesOrderCommandHandler> logger,
            IInventoryRepository inventoryRepository)
        {
            _salesOrderRepository = salesOrderRepository;
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
            _inventoryRepository = inventoryRepository;
        }

        public async Task<ServiceResponse<bool>> Handle(UpdateSalesOrderReturnCommand request, CancellationToken cancellationToken)
        {

            var salesOrderUpdate = _mapper.Map<POS.Data.SalesOrder >(request);
            salesOrderUpdate.SalesOrderItems.ForEach(item =>
            {
                item.Product = null;
                item.SalesOrderItemTaxes.ForEach(tax => { tax.Tax = null; });
            });
            var salesOrderExit = await _salesOrderRepository.FindAsync(request.Id);

            if (salesOrderExit.Status == SalesOrderStatus.Return)
            {
                return ServiceResponse<bool>.Return409("Sales Order can't edit becuase it's already Return.");
            }

            if (salesOrderExit.DeliveryStatus != SalesDeliveryStatus.DELIVERED)
            {
				return ServiceResponse<bool>.Return409("Please make sales as delivered before return it.");
			}

            salesOrderExit.SaleReturnNote = salesOrderUpdate.Note;

            salesOrderExit.Status = SalesOrderStatus.Return;
            salesOrderExit.TotalAmount = salesOrderExit.TotalAmount - salesOrderUpdate.TotalAmount;
            salesOrderExit.TotalTax = salesOrderExit.TotalTax - salesOrderUpdate.TotalTax;
            salesOrderExit.TotalDiscount = salesOrderExit.TotalDiscount - salesOrderUpdate.TotalDiscount;
            salesOrderExit.SalesOrderItems = salesOrderUpdate.SalesOrderItems;
            salesOrderExit.SalesOrderItems.ForEach(c =>
            {
                c.SalesOrderId = salesOrderUpdate.Id;
                c.CreatedDate = DateTime.UtcNow;
                c.Status = PurchaseSaleItemStatusEnum.Return;
            });

            if (salesOrderExit.TotalAmount <= salesOrderExit.TotalPaidAmount)
            {
                salesOrderExit.PaymentStatus = PaymentStatus.Paid;
            }
            else if (salesOrderExit.TotalPaidAmount > 0)
            {
                salesOrderExit.PaymentStatus = PaymentStatus.Partial;
            }
            else
            {
                salesOrderExit.PaymentStatus = PaymentStatus.Pending;
            }

            _salesOrderRepository.Update(salesOrderExit);

            var inventories = request.SalesOrderItems
                .Select(cs => new InventoryDto
                {
                    ProductId = cs.ProductId,
                    LocationId = salesOrderExit.LocationId,
                    PricePerUnit = cs.UnitPrice,
                    SalesOrderId = salesOrderExit.Id,
                    Stock = cs.Quantity,
                    UnitId = cs.UnitId,
                    TaxValue = cs.TaxValue,
                    Discount = cs.Discount
                }).ToList();

            inventories.ForEach(invetory =>
            {
                invetory = _inventoryRepository.ConvertStockAndPriceToBaseUnit(invetory);
            });

            var inventoriesToAdd = inventories
                .GroupBy(c => new { c.ProductId, c.LocationId })
                .Select(cs => new InventoryDto
                {
                    InventorySource = InventorySourceEnum.SalesOrderReturn,
                    ProductId = cs.Key.ProductId,
                    LocationId = cs.Key.LocationId,
                    PricePerUnit = cs.Sum(d => d.PricePerUnit * d.Stock + d.TaxValue - d.Discount) / cs.Sum(d => d.Stock),
                    SalesOrderId = salesOrderExit.Id,
                    Stock = cs.Sum(d => d.Stock),
                }).ToList();


            foreach (var inventory in inventoriesToAdd)
            {
                await _inventoryRepository.AddInventory(inventory);
            }

            if (await _uow.SaveAsync() <= 0)
            {
                _logger.LogError("Error while Updating Sales Order.");
                return ServiceResponse<bool>.Return500();
            }

            return ServiceResponse<bool>.ReturnResultWith201(true);
        }
    }

}
